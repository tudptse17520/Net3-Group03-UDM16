using CaroShared.Constants;
using CaroShared.Contracts;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.Json;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class GameBoardForm : Form
    {
        // ── Hằng số bàn cờ ────────────────────────────────────────────────
        private const int BoardSize = 15;
        private int _currentCellSize = 44;

        // ── Trạng thái bàn cờ ─────────────────────────────────────────────
        // 0 = trống | 1 = X (Player 1) | 2 = O (Player 2)
        private int[][] _board = CreateJaggedBoard();
        private string _roomId = string.Empty;
        private int _mySymbol = 1; // 1 = X, 2 = O
        private Guid _currentMatchId;

        // ── Spectator ─────────────────────────────────────────────────────
        private bool _isSpectator = false;

        // ── Quản lý đồng hồ đếm ngược lượt (DUY NHẤT 1 Timer logic) ───────
        private System.Windows.Forms.Timer? _countdownTimer;
        private int _remainingSeconds = 0;

        // ── Trạng thái kết thúc trận & đóng form an toàn ─────────────────
        private bool _isGameOver = false;
        private bool _isClosingProgrammatically = false;

        // ── Idempotency & Two-Phase Result UI ─────────────────────────────
        private bool _gameOverPresentationStarted = false;
        private bool _gameOverEventReceived = false;
        private Form? _resultShellForm = null;

        // ── Winning line presentation & Celebration ─────────────────────
        private readonly HashSet<(int row, int col)> _winningCells = new();
        private readonly List<(int row, int col)> _orderedWinningCells = new();
        private int _winningDirDr = 0;
        private int _winningDirDc = 0;
        private int _lastAcceptedMoveRow = -1;
        private int _lastAcceptedMoveCol = -1;
        private int _lastAcceptedMoveSymbol = 0;
        private int _celebrationElapsedMs = 0;
        private WinCelebrationOverlay? _celebrationOverlay;
        private bool _isLocalWinner = false;
        private readonly ToolTip _sharedToolTip = new ToolTip();

        private static int[][] CreateJaggedBoard()
        {
            var b = new int[BoardSize][];
            for (int i = 0; i < BoardSize; i++)
            {
                b[i] = new int[BoardSize];
            }
            return b;
        }

        // ── Tham chiếu các ô nút bàn cờ ────────────────────────────────────
        private Button[,] _cells = new Button[BoardSize, BoardSize];

        // ── Constructor mặc định (Designer cần) ───────────────────────────
        public GameBoardForm()
        {
            // Bật DoubleBuffering toàn diện để loại bỏ flicker
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;

            InitializeComponent();
            SetupCustomPaints();
            InitBoard();
            SetupDrawUI();
            CenterLayout();

            // Đăng ký sự kiện Resize để tính toán lại Responsive Geometry
            this.Resize += GameBoardForm_Resize;
            
            SetupAvatarLogic();

            CaroClient.Network.NetworkClient.Instance.OnMoveMade += HandleMoveMade;
            CaroClient.Network.NetworkClient.Instance.OnGameOver += HandleGameOver;
            CaroClient.Network.NetworkClient.Instance.OnMessageReceived += HandleMessageReceived;
            CaroClient.Network.NetworkClient.Instance.OnDrawOfferReceived += HandleDrawOfferReceived;
            CaroClient.Network.NetworkClient.Instance.OnDrawOfferResolved += HandleDrawOfferResolved;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var bgBrush = new SolidBrush(CaroTheme.Background);
            e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
        }

        // ── Constructor cho Player ────────────────────────────────────────
        public GameBoardForm(string roomId, int mySymbol, string opponentName, Guid matchId) : this()
        {
            _roomId = roomId;
            _mySymbol = mySymbol;
            _currentMatchId = matchId;

            string myName = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
            if (mySymbol == 1)
            {
                lblPlayer1Name.Text = myName;
                lblPlayer2Name.Text = opponentName;

                lblPlayer1Id.Text = $"ID: {myName}";
                lblPlayer2Id.Text = $"ID: {opponentName}";
                Piece1.Text = "Quân cờ: X (Đi trước)";
                Piece2.Text = "Quân cờ: O";
            }
            else
            {
                lblPlayer1Name.Text = opponentName;
                lblPlayer2Name.Text = myName;

                lblPlayer1Id.Text = $"ID: {opponentName}";
                lblPlayer2Id.Text = $"ID: {myName}";
                Piece1.Text = "Quân cờ: X (Đi trước)";
                Piece2.Text = "Quân cờ: O";
            }

            lblPlayer1MoveCount.Text = "0";
            lblPlayer2MoveCount.Text = "0";

            bool isMyTurn = (mySymbol == 1);
            UpdateTurnBadges(isMyTurn);

            _sharedToolTip.SetToolTip(lblPlayer1Name, lblPlayer1Name.Text);
            _sharedToolTip.SetToolTip(lblPlayer2Name, lblPlayer2Name.Text);

            StartTurnTimer(CaroShared.Constants.GameConstants.TurnTimeoutSeconds);
            LoadInitialAvatars();
        }

        // ── Constructor Spectator ─────────────────────────────────────────
        public GameBoardForm(SpectatorStateSnapshotDto snapshot) : this()
        {
            _isSpectator = true;
            if (snapshot.Session != null)
            {
                _currentMatchId = snapshot.Session.MatchIdentity;
            }

            // 1. Load tên người chơi
            lblPlayer1Name.Text = snapshot.Room?.PlayerX?.PlayerName ?? "Player X";
            lblPlayer2Name.Text = snapshot.Room?.PlayerO?.PlayerName ?? "Player O";
            lblPlayer1Id.Text = $"ID: {lblPlayer1Name.Text}";
            lblPlayer2Id.Text = $"ID: {lblPlayer2Name.Text}";

            // 2. Load trạng thái bàn cờ hiện tại
            if (snapshot.Session?.Board != null)
                UpdateBoard(snapshot.Session.Board);

            // 3. Cập nhật UI cho chế độ Spectator
            ApplySpectatorUI();
            _sharedToolTip.SetToolTip(lblPlayer1Name, lblPlayer1Name.Text);
            _sharedToolTip.SetToolTip(lblPlayer2Name, lblPlayer2Name.Text);

            // 4. Bắt đầu timer từ thông tin thời gian snapshot của Server
            if (snapshot.Session != null && snapshot.Session.RemainingTimeSeconds > 0)
            {
                StartTurnTimer(snapshot.Session.RemainingTimeSeconds);
            }
            LoadInitialAvatars();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Cài đặt Custom Paint cho các khối giao diện
        // ══════════════════════════════════════════════════════════════════
        private void SetupCustomPaints()
        {
            // Monogram Avatars
            picAvatarPlayer1.Paint += PicAvatarPlayer1_Paint;
            picAvatarPlayer2.Paint += PicAvatarPlayer2_Paint;

            // Mini Piece Previews
            picPlayer1Piece.Paint += (s, e) => DrawMiniPiece(e.Graphics, picPlayer1Piece.ClientRectangle, 1);
            picPlayer2Piece.Paint += (s, e) => DrawMiniPiece(e.Graphics, picPlayer2Piece.ClientRectangle, 2);

            // Recessed Stats Boxes
            pnlPlayer1Stats.Paint += (s, e) => DrawRecessedBox(e.Graphics, pnlPlayer1Stats.ClientRectangle);
            pnlPlayer2Stats.Paint += (s, e) => DrawRecessedBox(e.Graphics, pnlPlayer2Stats.ClientRectangle);

            // Timer Pills
            lblPlayer1TimerPill.Paint += (s, e) => DrawTimerPill(e.Graphics, lblPlayer1TimerPill.ClientRectangle, lblPlayer1TimerPill.Text, lblPlayer1TimerPill.ForeColor);
            lblPlayer2TimerPill.Paint += (s, e) => DrawTimerPill(e.Graphics, lblPlayer2TimerPill.ClientRectangle, lblPlayer2TimerPill.Text, lblPlayer2TimerPill.ForeColor);

            // Turn Badges
            pnlPlayer1Turn.Paint += (s, e) => DrawTurnBadge(e.Graphics, pnlPlayer1Turn.ClientRectangle, pnlPlayer1Turn.Text, pnlPlayer1Turn.Text.Contains("LƯỢT CỦA BẠN"));
            pnlPlayer2Turn.Paint += (s, e) => DrawTurnBadge(e.Graphics, pnlPlayer2Turn.ClientRectangle, pnlPlayer2Turn.Text, pnlPlayer2Turn.Text.Contains("LƯỢT CỦA BẠN"));
        }

        private void PicAvatarPlayer1_Paint(object? sender, PaintEventArgs e)
        {
            DrawMonogram(e.Graphics, picAvatarPlayer1.ClientRectangle, lblPlayer1Name.Text);
        }

        private void PicAvatarPlayer2_Paint(object? sender, PaintEventArgs e)
        {
            DrawMonogram(e.Graphics, picAvatarPlayer2.ClientRectangle, lblPlayer2Name.Text);
        }

        private void DrawMonogram(Graphics g, Rectangle rect, string playerName)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int size = Math.Min(rect.Width, rect.Height) - 6;
            if (size <= 4) return;
            var circleRect = new Rectangle(3, 3, size, size);

            // Vòng tròn nền ấm
            using (var bgBrush = new SolidBrush(CaroTheme.ButtonNormal))
            {
                g.FillEllipse(bgBrush, circleRect);
            }

            // Viền nổi gỗ
            using (var borderPen = new Pen(CaroTheme.WoodHighlight, 2.2f))
            {
                g.DrawEllipse(borderPen, circleRect);
            }

            // Chữ cái đầu tiên từ tên thật của người chơi
            string initial = string.IsNullOrWhiteSpace(playerName) ? "?" : playerName.Trim().Substring(0, 1).ToUpper();
            using (var font = new Font("Segoe UI", size * 0.42f, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    initial,
                    font,
                    circleRect,
                    CaroTheme.ButtonText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );
            }
        }

        private void DrawMiniPiece(Graphics g, Rectangle rect, int pieceType)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (pieceType == 1) // X
            {
                float pad = rect.Width * 0.2f;
                using var pen = new Pen(CaroTheme.XPiece, 3.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLine(pen, pad, pad, rect.Width - pad, rect.Height - pad);
                g.DrawLine(pen, rect.Width - pad, pad, pad, rect.Height - pad);
            }
            else if (pieceType == 2) // O
            {
                float outerD = rect.Width * 0.76f;
                float innerD = outerD * 0.48f;
                float cx = rect.Width / 2f;
                float cy = rect.Height / 2f;
                var outerR = new RectangleF(cx - outerD / 2, cy - outerD / 2, outerD, outerD);
                var innerR = new RectangleF(cx - innerD / 2, cy - innerD / 2, innerD, innerD);

                using var path = new GraphicsPath();
                path.AddEllipse(outerR);
                path.AddEllipse(innerR);
                using var brush = new SolidBrush(CaroTheme.OPieceBase);
                g.FillPath(brush, path);

                using var hiPen = new Pen(CaroTheme.OPieceHighlight, 1.2f);
                g.DrawArc(hiPen, outerR, 180, 100);

                using var innerShadowPen = new Pen(CaroTheme.OPieceInnerShadow, 1.0f);
                g.DrawArc(innerShadowPen, innerR, 180, 100);
            }
        }

        private void DrawRecessedBox(Graphics g, Rectangle rect)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var boxRect = new Rectangle(2, 2, rect.Width - 4, rect.Height - 4);
            using var path = CaroTheme.GetRoundedPath(boxRect, 12);
            using var bgBrush = new SolidBrush(CaroTheme.CardInnerBox);
            g.FillPath(bgBrush, path);

            using var innerBorderPen = new Pen(Color.FromArgb(50, CaroTheme.WoodShadow), 1f);
            g.DrawPath(innerBorderPen, path);
        }

        private void DrawTimerPill(Graphics g, Rectangle rect, string text, Color textColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var pillRect = new Rectangle(2, 2, rect.Width - 4, rect.Height - 4);
            int radius = pillRect.Height / 2;
            using var path = CaroTheme.GetRoundedPath(pillRect, radius);
            using var bgBrush = new SolidBrush(CaroTheme.TimerPillBg);
            g.FillPath(bgBrush, path);

            using var borderPen = new Pen(Color.FromArgb(80, CaroTheme.WoodShadow), 1f);
            g.DrawPath(borderPen, path);

            float fontSize = Math.Min(11f, Math.Max(8.5f, rect.Width / 22f));
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);

            TextRenderer.DrawText(
                g,
                text,
                font,
                pillRect,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }

        private void DrawTurnBadge(Graphics g, Rectangle rect, string text, bool isActive)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var pillRect = new Rectangle(2, 2, rect.Width - 4, rect.Height - 4);
            int radius = pillRect.Height / 2;
            using var path = CaroTheme.GetRoundedPath(pillRect, radius);

            Color bg = isActive ? CaroTheme.BadgeActiveBg : CaroTheme.BadgeInactiveBg;
            Color fg = isActive ? CaroTheme.BadgeActiveText : CaroTheme.BadgeInactiveText;

            using (var bgBrush = new SolidBrush(bg))
            {
                g.FillPath(bgBrush, path);
            }

            using (var borderPen = new Pen(Color.FromArgb(70, CaroTheme.WoodShadow), 1f))
            {
                g.DrawPath(borderPen, path);
            }

            // Tính cỡ chữ linh hoạt theo bề rộng pill để chữ không bao giờ bị tràn hay cắt
            float fontSize = Math.Min(10.5f, Math.Max(8.0f, rect.Width / 23f));
            using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);

            TextRenderer.DrawText(
                g,
                text,
                font,
                pillRect,
                fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine
            );
        }

        // ── Avatar Logic ──────────────────────────────────────────────────
        private Image? _avatarP1;
        private Image? _avatarP2;

        private void SetupAvatarLogic()
        {
            picAvatarPlayer1.Paint += Avatar_Paint;
            picAvatarPlayer2.Paint += Avatar_Paint;

            CaroClient.Settings.AvatarManager.Instance.OnAvatarUpdated += OnAvatarUpdated;
        }

        private void LoadInitialAvatars()
        {
            LoadAvatarForPlayer(lblPlayer1Name.Text, 1);
            LoadAvatarForPlayer(lblPlayer2Name.Text, 2);
        }

        private void LoadAvatarForPlayer(string playerName, int playerIndex)
        {
            if (string.IsNullOrEmpty(playerName) || playerName.StartsWith("Player")) return;

            var pInfo = CaroClient.Network.NetworkClient.Instance.PlayerList?.FirstOrDefault(p => string.Equals(p.PlayerName, playerName, StringComparison.OrdinalIgnoreCase));
            if (pInfo != null && pInfo.HasAvatar)
            {
                var img = CaroClient.Settings.AvatarManager.Instance.GetAvatar(pInfo.PlayerName, pInfo.AvatarVersion, pInfo.HasAvatar);
                if (img != null)
                {
                    UpdateAvatarUI(playerIndex, img);
                }
            }
            else if (pInfo == null)
            {
                CaroClient.Network.NetworkClient.Instance.SendAvatarRequestAsync(playerName);
            }
        }

        private void OnAvatarUpdated(string playerId, Image? avatar)
        {
            SafeInvoke(() =>
            {
                if (string.Equals(playerId, lblPlayer1Name.Text, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateAvatarUI(1, avatar);
                }
                else if (string.Equals(playerId, lblPlayer2Name.Text, StringComparison.OrdinalIgnoreCase))
                {
                    UpdateAvatarUI(2, avatar);
                }
            });
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.Invoke(action); } catch { }
            }
            else
            {
                action();
            }
        }

        private void UpdateAvatarUI(int playerIndex, Image? avatar)
        {
            var targetPic = playerIndex == 1 ? picAvatarPlayer1 : picAvatarPlayer2;
            var oldAvatar = playerIndex == 1 ? _avatarP1 : _avatarP2;

            if (playerIndex == 1) _avatarP1 = avatar != null ? (Image)avatar.Clone() : null;
            else _avatarP2 = avatar != null ? (Image)avatar.Clone() : null;

            targetPic.Invalidate();

            if (oldAvatar != null)
            {
                oldAvatar.Dispose();
            }
        }

        private void Avatar_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not PictureBox pic) return;
            bool isPlayer1 = pic == picAvatarPlayer1;
            var avatar = isPlayer1 ? _avatarP1 : _avatarP2;
            string name = isPlayer1 ? lblPlayer1Name.Text : lblPlayer2Name.Text;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, pic.Width - 1, pic.Height - 1);

            if (avatar != null)
            {
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(rect);
                    e.Graphics.SetClip(path);
                    e.Graphics.DrawImage(avatar, 0, 0, pic.Width, pic.Height);
                    e.Graphics.ResetClip();
                }
            }
            else
            {
                using var bgBrush = new SolidBrush(isPlayer1 ? CaroTheme.WoodFrame : CaroTheme.Grid);
                e.Graphics.FillEllipse(bgBrush, rect);

                string initial = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper();
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(initial, new Font("Segoe UI", 24, FontStyle.Bold), Brushes.White, rect, sf);
            }

            using var borderPen = new Pen(CaroTheme.CardInnerBox, 2f);
            e.Graphics.DrawEllipse(borderPen, rect);
        }

        private void UpdateTurnBadges(bool isMyTurn)
        {
            if (_isSpectator)
            {
                pnlPlayer1Turn.Text = "Đang xem...";
                pnlPlayer2Turn.Text = "Đang xem...";
                pnlPlayer1Turn.Invalidate();
                pnlPlayer2Turn.Invalidate();
                return;
            }

            if (_mySymbol == 1)
            {
                pnlPlayer1Turn.Text = isMyTurn ? "● LƯỢT CỦA BẠN" : "⏳ ĐANG CHỜ ĐỐI THỦ";
                pnlPlayer2Turn.Text = isMyTurn ? "⏳ ĐANG CHỜ ĐỐI THỦ" : "● LƯỢT CỦA BẠN";
            }
            else
            {
                pnlPlayer1Turn.Text = isMyTurn ? "⏳ ĐANG CHỜ ĐỐI THỦ" : "● LƯỢT CỦA BẠN";
                pnlPlayer2Turn.Text = isMyTurn ? "● LƯỢT CỦA BẠN" : "⏳ ĐANG CHỜ ĐỐI THỦ";
            }

            pnlPlayer1Turn.Invalidate();
            pnlPlayer2Turn.Invalidate();
            UpdateTimerUI();
        }

        // ══════════════════════════════════════════════════════════════════
        //  ApplySpectatorUI — thiết lập giao diện "chỉ xem"
        // ══════════════════════════════════════════════════════════════════
        private void ApplySpectatorUI()
        {
            this.Text = "CARO ONLINE — Chế độ Khán giả 👁️";
            lblAppTitle.Text = "CARO ONLINE — CHẾ ĐỘ KHÁN GIẢ 👁️";

            btnSurrender.Text = "THOÁT PHÒNG";
            btnSurrender.GlyphIcon = "🚪";

            btnExitMatch.Text = "THOÁT PHÒNG";
            btnExitMatch.GlyphIcon = "🚪";

            btnOfferDraw.Enabled = false;
            btnNewGame.Enabled = false;

            UpdateTurnBadges(false);

            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    _cells[row, col].Cursor = Cursors.Default;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  InitBoard — khởi tạo 225 ô cờ với Custom Paint cao cấp
        // ══════════════════════════════════════════════════════════════════
        private void InitBoard()
        {
            pnlBoardContainer.Controls.Clear();
            pnlBoardContainer.BackColor = CaroTheme.BoardSurface;

            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    var btn = new Button
                    {
                        Width     = _currentCellSize,
                        Height    = _currentCellSize,
                        Left      = col * _currentCellSize,
                        Top       = row * _currentCellSize,
                        Tag       = (row, col),
                        Text      = "",
                        FlatStyle = FlatStyle.Flat,
                        BackColor = CaroTheme.BoardSurface,
                        Cursor    = _isSpectator ? Cursors.Default : Cursors.Hand,
                        TabStop   = false,
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Click += Cell_Click;
                    btn.Paint += Cell_Paint;

                    pnlBoardContainer.Controls.Add(btn);
                    _cells[row, col] = btn;
                }
            }

            CenterLayout();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Cell_Paint — Vẽ X (khắc gỗ) và O (nhẫn ngà 3D) chuẩn reference
        // ══════════════════════════════════════════════════════════════════
        private void Cell_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            var (row, col) = ((int, int))btn.Tag!;
            int cellVal = _board[row][col];
            var rect = btn.ClientRectangle;
            bool isWinningCell = _winningCells.Contains((row, col));

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Tính toán hiệu ứng Pulse & Light Sweep cho ô thắng
            float scaleFactor = 1.0f;
            int sweepShineAlpha = 0;

            if (isWinningCell)
            {
                int cellIdx = _orderedWinningCells.IndexOf((row, col));
                if (cellIdx >= 0)
                {
                    // A. Staggered piece pulse (T = 150ms -> 700ms)
                    // Mỗi quân cách nhau 60ms, pulse scale 1.0 -> 1.15 -> 1.0 trong 220ms
                    int pieceStartMs = 150 + (cellIdx * 60);
                    int pieceDurationMs = 220;
                    if (_celebrationElapsedMs >= pieceStartMs && _celebrationElapsedMs <= pieceStartMs + pieceDurationMs)
                    {
                        float p = (_celebrationElapsedMs - pieceStartMs) / (float)pieceDurationMs;
                        scaleFactor = 1.0f + 0.15f * (float)Math.Sin(p * Math.PI);
                    }

                    // B. Light sweep wave (T = 300ms -> 900ms)
                    if (_celebrationElapsedMs >= 300 && _celebrationElapsedMs <= 900)
                    {
                        int totalPieces = Math.Max(1, _orderedWinningCells.Count);
                        float sweepT = (_celebrationElapsedMs - 300) / 600f; // 0.0 -> 1.0
                        float pieceNorm = (float)cellIdx / Math.Max(1, totalPieces - 1);
                        float diff = Math.Abs(sweepT - pieceNorm);
                        if (diff < 0.25f)
                        {
                            sweepShineAlpha = (int)(160 * (1f - diff / 0.25f));
                        }
                    }
                }
            }

            // Get settings
            var settings = CaroClient.Settings.PersonalizationManager.Instance.Settings;
            var boardTheme = CaroClient.Settings.BoardPaletteGenerator.GeneratePreset(settings.BoardAppearance.Theme);

            // 2. Mặt gỗ bàn cờ (hoặc gold glow nếu winning cell)
            using (var bgBrush = new SolidBrush(isWinningCell ? Color.FromArgb(65, CaroTheme.VictoryGoldLight) : boardTheme.SurfaceColor))
            {
                e.Graphics.FillRectangle(bgBrush, rect);
            }
            if (isWinningCell)
            {
                // Ánh sáng tỏa vàng ấm
                using (var glowBrush = new SolidBrush(Color.FromArgb(45, CaroTheme.VictoryWarmGlow)))
                {
                    e.Graphics.FillRectangle(glowBrush, rect);
                }
            }

            // 3. Đường kẻ lưới
            Color gridColor = isWinningCell ? CaroTheme.VictoryGold : boardTheme.GridColor;
            using (var gridPen = new Pen(gridColor, isWinningCell ? 1.5f : 1f))
            {
                e.Graphics.DrawRectangle(gridPen, 0, 0, rect.Width - 1, rect.Height - 1);
            }

            // 4. Render Quân cờ X hoặc O với scaleFactor
            if (cellVal != 0)
            {
                float pieceW = rect.Width * 0.7f * scaleFactor;
                float padX = (rect.Width - pieceW) / 2f;
                float padY = (rect.Height - pieceW) / 2f;
                var pieceRect = new Rectangle((int)padX, (int)padY, (int)pieceW, (int)pieceW);

                bool isMyPiece = (cellVal == _mySymbol);
                var shape = isMyPiece ? settings.PieceAppearance.MyShape : settings.PieceAppearance.OpponentShape;
                var color = isMyPiece ? settings.PieceAppearance.MyColor : settings.PieceAppearance.OpponentColor;

                CaroClient.Drawing.PieceRenderer.DrawPiece(e.Graphics, pieceRect, shape, color, settings.PieceAppearance.Effect, settings.PieceAppearance.Intensity);
            }

            // 5. Dải quét ánh sáng vàng ngà chạy qua ô (Light Sweep Wave)
            if (sweepShineAlpha > 0)
            {
                using (var sweepBrush = new SolidBrush(Color.FromArgb(sweepShineAlpha, CaroTheme.VictoryIvory)))
                {
                    e.Graphics.FillRectangle(sweepBrush, rect);
                }
            }

            // 6. Viền vàng kim nổi bật cho ô cờ chiến thắng
            if (isWinningCell)
            {
                using var goldPen = new Pen(CaroTheme.VictoryGold, 2.5f);
                e.Graphics.DrawRectangle(goldPen, 1, 1, rect.Width - 3, rect.Height - 3);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  CenterLayout — Responsive Geometry chuẩn theo công thức QA
        // ══════════════════════════════════════════════════════════════════
        private void CenterLayout()
        {
            if (this.ClientSize.Width <= 0 || this.ClientSize.Height <= 0) return;

            this.SuspendLayout();
            try
            {
                float dpiScale = this.DeviceDpi > 0 ? (this.DeviceDpi / 96f) : 1f;

            // Tính cardWidth tự động tương thích với DPI và diện tích khả dụng của Form
            int baseCardWidth = 260;
            int cardWidth = (int)Math.Round(baseCardWidth * dpiScale);
            // Giữ card gọn gàng khi form hẹp để không chèn ép bàn cờ
            if (this.ClientSize.Width < 1280 && cardWidth > 250) cardWidth = 250;
            if (this.ClientSize.Width < 1080 && cardWidth > 230) cardWidth = 230;

            int leftCardWidth = cardWidth;
            int rightCardWidth = cardWidth;
            int leftCardBoardGap = Math.Max(12, (int)Math.Round(20 * dpiScale));
            int rightCardBoardGap = leftCardBoardGap;
            int leftOuterMargin = Math.Max(12, (int)Math.Round(20 * dpiScale));
            int rightOuterMargin = leftOuterMargin;
            int frameThickness = pnlWoodFrame.FrameThickness;

            int headerReservedHeight = (int)Math.Round(56 * dpiScale);
            int footerReservedHeight = (int)Math.Round(68 * dpiScale);
            int topMargin = 8;
            int bottomMargin = 12;

            // 1. Tính toán vùng khả dụng trung tâm theo khuyến nghị QA
            int availableCenterWidth = this.ClientSize.Width
                - leftOuterMargin
                - rightOuterMargin
                - leftCardWidth
                - rightCardWidth
                - leftCardBoardGap
                - rightCardBoardGap;

            int availableCenterHeight = this.ClientSize.Height
                - headerReservedHeight
                - footerReservedHeight
                - topMargin
                - bottomMargin;

            // Bảo vệ an toàn chống kích thước âm hoặc cellSize <= 0
            if (availableCenterWidth < 180) availableCenterWidth = 180;
            if (availableCenterHeight < 180) availableCenterHeight = 180;

            // 2. Kích thước khung gỗ bên ngoài (luôn vuông vắn)
            int frameOuterSize = Math.Min(availableCenterWidth, availableCenterHeight);

            // 3. Kích thước mặt cờ bên trong
            int boardSurfaceSize = frameOuterSize - (frameThickness * 2);

            // 4. Tính toán kích thước ô cờ (luôn là số nguyên, không méo ô)
            int cellSize = boardSurfaceSize / BoardSize;
            if (cellSize < 12) cellSize = 12;

            boardSurfaceSize = cellSize * BoardSize;
            frameOuterSize = boardSurfaceSize + (frameThickness * 2);
            _currentCellSize = cellSize;

            // 5. Căn giữa khung gỗ
            pnlWoodFrame.Size = new Size(frameOuterSize, frameOuterSize);
            pnlWoodFrame.Left = (this.ClientSize.Width - frameOuterSize) / 2;
            pnlWoodFrame.Top = headerReservedHeight + Math.Max(4, (availableCenterHeight - frameOuterSize) / 2);

            // Căn mặt cờ bên trong khung gỗ
            pnlBoardContainer.Location = new Point(frameThickness, frameThickness);
            pnlBoardContainer.Size = new Size(boardSurfaceSize, boardSurfaceSize);

            // 6. Cập nhật vị trí và kích thước 225 ô cờ
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    var btn = _cells[row, col];
                    if (btn != null)
                    {
                        btn.Width  = _currentCellSize;
                        btn.Height = _currentCellSize;
                        btn.Left   = col * _currentCellSize;
                        btn.Top    = row * _currentCellSize;
                    }
                }
            }

            // 7. Căn chỉnh Card Player 1 & Player 2 và tất cả control con bên trong (chống tràn 100%)
            pnlPlayer1.Width = leftCardWidth;
            LayoutCardControls(
                pnlPlayer1, picAvatarPlayer1, lblPlayer1Name, lblPlayer1Id,
                lblPlayer1TimerPill, pnlPlayer1Stats, Piece1, picPlayer1Piece,
                lblPlayer1MoveCountLabel, lblPlayer1MoveCount, pnlPlayer1Turn);

            pnlPlayer2.Width = rightCardWidth;
            LayoutCardControls(
                pnlPlayer2, picAvatarPlayer2, lblPlayer2Name, lblPlayer2Id,
                lblPlayer2TimerPill, pnlPlayer2Stats, Piece2, picPlayer2Piece,
                lblPlayer2MoveCountLabel, lblPlayer2MoveCount, pnlPlayer2Turn);

            pnlPlayer1.Left = pnlWoodFrame.Left - leftCardBoardGap - leftCardWidth;
            pnlPlayer1.Top = pnlWoodFrame.Top + Math.Max(0, (pnlWoodFrame.Height - pnlPlayer1.Height) / 2);

            pnlPlayer2.Left = pnlWoodFrame.Right + rightCardBoardGap;
            pnlPlayer2.Top = pnlPlayer1.Top;

            // 8. Căn giữa hàng nút chức năng bên dưới bàn cờ với chiều rộng đã kiểm chứng chống chạm viền
            int btnSurrenderW = (int)Math.Round(165 * dpiScale);
            int btnDrawW = (int)Math.Round(145 * dpiScale);
            int btnNewGameW = (int)Math.Round(165 * dpiScale);
            int btnExitW = (int)Math.Round(155 * dpiScale);
            int btnGap = (int)Math.Round(12 * dpiScale);
            int totalFooterW = btnSurrenderW + btnDrawW + btnNewGameW + btnExitW + (btnGap * 3);

            // Đảm bảo footer luôn nằm gọn trong chiều rộng bàn cờ (không bao giờ vượt quá cạnh bàn cờ)
            if (totalFooterW > frameOuterSize && frameOuterSize > 300)
            {
                int availBtnTotal = frameOuterSize - (btnGap * 3);
                int baseSum = btnSurrenderW + btnDrawW + btnNewGameW + btnExitW;
                float shrink = (float)availBtnTotal / baseSum;
                btnSurrenderW = (int)Math.Round(btnSurrenderW * shrink);
                btnDrawW = (int)Math.Round(btnDrawW * shrink);
                btnNewGameW = (int)Math.Round(btnNewGameW * shrink);
                btnExitW = availBtnTotal - btnSurrenderW - btnDrawW - btnNewGameW;
                totalFooterW = btnSurrenderW + btnDrawW + btnNewGameW + btnExitW + (btnGap * 3);
            }

            int footerLeft = pnlWoodFrame.Left + Math.Max(0, (frameOuterSize - totalFooterW) / 2);
            int footerTop = pnlWoodFrame.Bottom + 12;
            int btnH = Math.Max(38, (int)Math.Round(42 * dpiScale));

            btnSurrender.Size = new Size(btnSurrenderW, btnH);
            btnSurrender.Location = new Point(footerLeft, footerTop);

            btnOfferDraw.Size = new Size(btnDrawW, btnH);
            btnOfferDraw.Location = new Point(btnSurrender.Right + btnGap, footerTop);

            btnNewGame.Size = new Size(btnNewGameW, btnH);
            btnNewGame.Location = new Point(btnOfferDraw.Right + btnGap, footerTop);

            btnExitMatch.Size = new Size(btnExitW, btnH);
            btnExitMatch.Location = new Point(btnNewGame.Right + btnGap, footerTop);

            // 9. Căn giữa Title
            lblAppTitle.Width = this.ClientSize.Width;
            lblAppTitle.Location = new Point(0, 10);

            // 10. Tự động căn giữa các overlay/panel nếu đang hiển thị
            if (_pnlDrawRequest != null && !_pnlDrawRequest.IsDisposed)
            {
                _pnlDrawRequest.Location = new Point(
                    (this.ClientSize.Width - _pnlDrawRequest.Width) / 2,
                    (this.ClientSize.Height - _pnlDrawRequest.Height) / 2
                );
            }
            }
            finally
            {
                this.ResumeLayout(true);
            }
            this.Invalidate(true);
        }

        private void LayoutCardControls(
            Soft3DPanel panel, PictureBox avatar, Label lblName, Label lblId,
            Label timerPill, Panel statsBox, Label lblPiece, PictureBox picPiece,
            Label lblMoveCountLabel, Label lblMoveCount, Label turnBadge)
        {
            int pad = 14;
            int innerWidth = Math.Max(100, panel.Width - (pad * 2));

            // Avatar & Player Info
            int avatarSize = 56;
            avatar.Size = new Size(avatarSize, avatarSize);
            avatar.Location = new Point(pad, 16);

            int textLeft = avatar.Right + 10;
            int textWidth = Math.Max(50, innerWidth - avatarSize - 10);
            lblName.Location = new Point(textLeft, 18);
            lblName.Size = new Size(textWidth, 26);

            lblId.Location = new Point(textLeft, lblName.Bottom + 2);
            lblId.Size = new Size(textWidth, 18);

            // Timer Pill
            timerPill.Location = new Point(pad, Math.Max(avatar.Bottom, lblId.Bottom) + 12);
            timerPill.Size = new Size(innerWidth, 34);

            // Stats Box
            statsBox.Location = new Point(pad, timerPill.Bottom + 10);
            statsBox.Size = new Size(innerWidth, 110);

            // Inside Stats Box
            int pieceSize = 28;
            picPiece.Size = new Size(pieceSize, pieceSize);
            picPiece.Location = new Point(innerWidth - 12 - pieceSize, 12);

            lblPiece.Location = new Point(10, 16);
            lblPiece.Size = new Size(Math.Max(50, innerWidth - pieceSize - 24), 22);

            lblMoveCountLabel.Location = new Point(10, 60);
            lblMoveCountLabel.Size = new Size(Math.Max(50, innerWidth - 45), 20);

            lblMoveCount.Size = new Size(30, 24);
            lblMoveCount.Location = new Point(innerWidth - 12 - lblMoveCount.Width, 56);

            // Turn Badge
            turnBadge.Location = new Point(pad, statsBox.Bottom + 12);
            turnBadge.Size = new Size(innerWidth, 40);

            // Panel Height bao bọc toàn bộ thành phần con
            panel.Height = turnBadge.Bottom + 18;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            CenterLayout();
            this.Invalidate(true);
        }

        // ── Sự kiện Resize ────────────────────────────────────────────────
        private void GameBoardForm_Resize(object? sender, EventArgs e)
        {
            CenterLayout();
            this.Invalidate(true);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Cell_Click — kiểm tra lượt và gửi nước đi
        // ══════════════════════════════════════════════════════════════════
        private void Cell_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            var (row, col) = ((int, int))btn.Tag!;

            // Board lock: Không cho đánh khi game đã kết thúc
            if (_isGameOver) return;

            // Khán giả không được đánh
            if (_isSpectator) return;

            // Chỉ cho phép đánh vào ô trống
            if (_board[row][col] != 0) return;

            SendMove(row, col);
        }

        private void SendMove(int row, int col)
        {
            var request = new CaroShared.Contracts.MakeMoveRequest { X = col, Y = row };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.MakeMoveRequest, request);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        // ── Cập nhật UI từ dữ liệu server gửi về ─────────────────────────
        public void UpdateBoard(int[][] board)
        {
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    _board[row][col] = board[row][col];
                    _cells[row, col]?.Invalidate();
                }
            }
        }

        // ── Reset bàn cờ về trạng thái ban đầu ───────────────────────────
        public void ResetBoard()
        {
            StopTurnTimer();
            StopCelebrationOverlay();
            _board = CreateJaggedBoard();
            lblPlayer1MoveCount.Text = "0";
            lblPlayer2MoveCount.Text = "0";
            pnlPlayer1.IsWinnerHighlighted = false;
            pnlPlayer2.IsWinnerHighlighted = false;
            pnlPlayer1.Invalidate();
            pnlPlayer2.Invalidate();
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    _cells[row, col]?.Invalidate();
                }
            }
            StartTurnTimer(CaroShared.Constants.GameConstants.TurnTimeoutSeconds);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Quản lý đồng hồ đếm ngược lượt (Client Turn Timer UI)
        // ══════════════════════════════════════════════════════════════════
        public void StartTurnTimer(int seconds = GameConstants.TurnTimeoutSeconds)
        {
            StopTurnTimer();

            _remainingSeconds = seconds > 0 ? seconds : GameConstants.TurnTimeoutSeconds;
            UpdateTimerUI();

            _countdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 giây
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        public void StopTurnTimer()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Tick -= CountdownTimer_Tick;
                _countdownTimer.Dispose();
                _countdownTimer = null;
            }
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateTimerUI();
            }
            else
            {
                StopTurnTimer();
            }
        }

        private void UpdateTimerUI()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateTimerUI));
                return;
            }

            int minutes = _remainingSeconds / 60;
            int secs = _remainingSeconds % 60;
            string timeStr = $"⏱ {minutes:D2}:{secs:D2}";
            lblTimeCount.Text = $"{minutes:D2}:{secs:D2}";

            Color textColor = (_remainingSeconds <= 5) ? CaroTheme.TimerAlertText : CaroTheme.TimerPillText;

            // Cập nhật nhãn đếm ngược trên Card của người đang đến lượt
            bool isP1Active = pnlPlayer1Turn.Text.Contains("LƯỢT CỦA BẠN");
            if (isP1Active)
            {
                lblPlayer1TimerPill.Text = timeStr;
                lblPlayer1TimerPill.ForeColor = textColor;
                lblPlayer2TimerPill.Text = "⏱ --:--";
                lblPlayer2TimerPill.ForeColor = CaroTheme.TextMuted;
            }
            else
            {
                lblPlayer2TimerPill.Text = timeStr;
                lblPlayer2TimerPill.ForeColor = textColor;
                lblPlayer1TimerPill.Text = "⏱ --:--";
                lblPlayer1TimerPill.ForeColor = CaroTheme.TextMuted;
            }

            lblPlayer1TimerPill.Invalidate();
            lblPlayer2TimerPill.Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_isClosingProgrammatically && !_isSpectator && !_isGameOver && !string.IsNullOrEmpty(_roomId))
            {
                var result = CaroDialogForm.Show(
                    this,
                    "Trận đấu đang diễn ra! Nếu rời phòng bạn sẽ bị xử thua.\nBạn có chắc chắn muốn thoát về sảnh?",
                    "Xác Nhận Thoát Trận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                var req = new CaroShared.Contracts.SurrenderRequest
                {
                    RoomId   = _roomId,
                    PlayerId = CaroClient.Network.NetworkClient.Instance.CurrentNickname
                };
                var msg = new CaroShared.Protocol.NetworkMessage(
                    CaroShared.Enums.MessageType.SurrenderRequest, req);
                _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);

                _isGameOver = true;
            }

            // Dispose tất cả celebration resources & result shell
            StopTurnTimer();
            StopCelebrationOverlay();
            if (_resultShellForm != null)
            {
                _resultShellForm.Dispose();
                _resultShellForm = null;
            }
            if (_pnlDrawRequest != null)
            {
                _pnlDrawRequest.Dispose();
                _pnlDrawRequest = null;
            }
            BackdropOverlay.ClearFor(this);

            CaroClient.Network.NetworkClient.Instance.OnMoveMade -= HandleMoveMade;
            CaroClient.Network.NetworkClient.Instance.OnGameOver -= HandleGameOver;
            CaroClient.Network.NetworkClient.Instance.OnMessageReceived -= HandleMessageReceived;
            CaroClient.Network.NetworkClient.Instance.OnDrawOfferReceived -= HandleDrawOfferReceived;
            CaroClient.Network.NetworkClient.Instance.OnDrawOfferResolved -= HandleDrawOfferResolved;
            CaroClient.Settings.AvatarManager.Instance.OnAvatarUpdated -= OnAvatarUpdated;
            _sharedToolTip.Dispose();
            base.OnFormClosing(e);
        }

        // ════════════════════════════════════════════════════════════════════
        // Event handlers (Preserved 100% from original source)
        // ════════════════════════════════════════════════════════════════════

        private void label1_Click(object sender, EventArgs e) { }
        private void label9_Click(object sender, EventArgs e) { }
        private void pictureBox1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label2_Click_1(object sender, EventArgs e) { }
        private void pictureBox1_Click_1(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label3_Click_1(object sender, EventArgs e) { }

        private void btnExitMatch_Click(object? sender, EventArgs e)
        {
            if (_isSpectator || string.IsNullOrEmpty(_roomId))
            {
                _isClosingProgrammatically = true;
                this.Close();
                return;
            }

            DialogResult result = DialogResult.Yes;
            
            if (!_isGameOver)
            {
                result = CaroDialogForm.Show(
                    this,
                    "Trận đấu đang diễn ra! Nếu rời phòng bạn sẽ bị xử thua.\nBạn có chắc chắn muốn thoát về sảnh?",
                    "Xác Nhận Thoát Trận",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
            }

            if (result == DialogResult.Yes)
            {
                // Chỉ gửi yêu cầu đầu hàng nếu game ĐANG DIỄN RA (tránh gửi đầu hàng trùng lặp sau khi game over)
                if (!_isGameOver)
                {
                    var req = new CaroShared.Contracts.SurrenderRequest
                    {
                        RoomId   = _roomId,
                        PlayerId = CaroClient.Network.NetworkClient.Instance.CurrentNickname
                    };
                    var msg = new CaroShared.Protocol.NetworkMessage(
                        CaroShared.Enums.MessageType.SurrenderRequest, req);
                    _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
                }

                _isGameOver = true;
                _isClosingProgrammatically = true;
                BackdropOverlay.ClearFor(this);
                this.Close();
            }
        }

        // btnSurrender / Thoát phòng (Spectator)
        private void button1_Click(object? sender, EventArgs e)
        {
            if (_isSpectator)
            {
                _isClosingProgrammatically = true;
                this.Close();
                return;
            }

            var result = CaroDialogForm.Show(
                this,
                "Bạn có chắc chắn muốn đầu hàng?",
                "Xác nhận đầu hàng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var req = new CaroShared.Contracts.SurrenderRequest
                {
                    RoomId   = _roomId,
                    PlayerId = CaroClient.Network.NetworkClient.Instance.CurrentNickname
                };
                var msg = new CaroShared.Protocol.NetworkMessage(
                    CaroShared.Enums.MessageType.SurrenderRequest, req);
                _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
            }
        }

        // btnNewGame (Ván mới)
        private void button3_Click(object? sender, EventArgs e)
        {
            var req = new CaroShared.Contracts.NewGameRequest
            {
                RoomId   = _roomId,
                PlayerId = CaroClient.Network.NetworkClient.Instance.CurrentNickname
            };
            var msg = new CaroShared.Protocol.NetworkMessage(
                CaroShared.Enums.MessageType.NewGameRequest, req);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        // ══════════════════════════════════════════════════════════════════
        //  NON-BLOCKING RESULT SHELL (TRUE INSTANT UI)
        // ══════════════════════════════════════════════════════════════════
        private void ShowNonBlockingResultShell(string resultTitle, string resultText)
        {
            if (_resultShellForm != null && !_resultShellForm.IsDisposed)
                return; // Already exists

            // Đảm bảo tạo backdrop
            BackdropOverlay.Acquire(this);

            int panelW = 380;
            int panelH = 260;

            _resultShellForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false,
                BackColor = CaroTheme.Background,
                Size = new Size(panelW, panelH)
            };

            using (var path = CaroTheme.GetRoundedPath(new Rectangle(0, 0, panelW, panelH), 16))
            {
                _resultShellForm.Region = new Region(path);
            }

            var shellPanel = new Soft3DPanel
            {
                Dock = DockStyle.Fill,
                CornerRadius = 16,
                Padding = new Padding(24)
            };

            var lblTitle = new Label
            {
                Text = resultTitle,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = CaroTheme.ButtonNormal,
                AutoSize = false,
                Size = new Size(panelW - 48, 40),
                Location = new Point(24, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblMessage = new Label
            {
                Name = "lblMessage",
                Text = resultText,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = CaroTheme.TextDark,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(panelW - 48, 40),
                Location = new Point(24, 70),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblLoading = new Label
            {
                Name = "lblLoading",
                Text = "Đang hoàn tất trận đấu...",
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                ForeColor = CaroTheme.TextMuted,
                AutoSize = false,
                Size = new Size(panelW - 48, 30),
                Location = new Point(24, 130),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnVanMoi = new PillButton
            {
                Name = "btnVanMoi",
                Text = "VÁN MỚI",
                Size = new Size(135, 38),
                Location = new Point(48, 180),
                Enabled = false // Disabled until Phase B
            };
            btnVanMoi.Click += button3_Click;

            var btnVeSanh = new PillButton
            {
                Name = "btnVeSanh",
                Text = "VỀ SẢNH",
                Size = new Size(135, 38),
                Location = new Point(198, 180),
                Enabled = false, // Disabled until Phase B
                IsDestructive = true
            };
            btnVeSanh.Click += btnExitMatch_Click; // Rewired: Thoát về sảnh an toàn sau khi kết thúc trận

            shellPanel.Controls.Add(lblTitle);
            shellPanel.Controls.Add(lblMessage);
            shellPanel.Controls.Add(lblLoading);
            shellPanel.Controls.Add(btnVanMoi);
            shellPanel.Controls.Add(btnVeSanh);
            
            _resultShellForm.Controls.Add(shellPanel);

            // Sync location with parent form
            EventHandler updateLocation = (s, e) => {
                if (_resultShellForm != null && !_resultShellForm.IsDisposed)
                {
                    if (this.WindowState == FormWindowState.Minimized) {
                        _resultShellForm.Visible = false;
                    } else {
                        if (!_resultShellForm.Visible) _resultShellForm.Visible = true;
                        int px = (this.ClientSize.Width - panelW) / 2;
                        int py = (this.ClientSize.Height - panelH) / 2;
                        _resultShellForm.Location = this.PointToScreen(new Point(px, py));
                    }
                }
            };

            this.Move += updateLocation;
            this.Resize += updateLocation;
            _resultShellForm.FormClosed += (s, e) => {
                this.Move -= updateLocation;
                this.Resize -= updateLocation;
            };

            // Set initial location and show
            updateLocation(this, EventArgs.Empty);
            _resultShellForm.Show(this);
        }

        private void FinalizeResultShell(string? errorDetail)
        {
            if (_resultShellForm == null || _resultShellForm.IsDisposed || _resultShellForm.Controls.Count == 0)
            {
                // Fallback nếu shell chưa được tạo (có thể do lỗi thứ tự)
                return;
            }

            var shellPanel = _resultShellForm.Controls[0];

            // Remove loading label
            var lblLoading = shellPanel.Controls["lblLoading"];
            if (lblLoading != null)
            {
                if (!string.IsNullOrEmpty(errorDetail))
                {
                    lblLoading.Text = "Lỗi xác nhận từ máy chủ.";
                    lblLoading.ForeColor = Color.IndianRed;
                }
                else
                {
                    lblLoading.Visible = false;
                }
            }

            // Enable buttons appropriately
            var btnVanMoi = shellPanel.Controls["btnVanMoi"];
            if (btnVanMoi != null)
            {
                // Ván mới có thể không hợp lệ nếu đối thủ đã thoát. 
                // Tuy nhiên ta cứ enable theo luồng GameOverEvent cơ bản.
                btnVanMoi.Enabled = true;
            }

            var btnVeSanh = shellPanel.Controls["btnVeSanh"];
            if (btnVeSanh != null)
            {
                btnVeSanh.Enabled = true;
            }
        }


        private void HandleMoveMade(CaroShared.Contracts.MoveMadeEventDto dto)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleMoveMade(dto)));
                return;
            }

            // Hủy/ẩn các thông báo đề nghị hòa khi có nước đi mới
            if (_pnlDrawRequest != null)
            {
                _pnlDrawRequest.Visible = false;
            }
            btnOfferDraw.Text = "HÒA";
            btnOfferDraw.Enabled = true;

            if (dto.IsValid)
            {
                int symbol = (dto.PlayerId == CaroClient.Network.NetworkClient.Instance.CurrentNickname)
                    ? _mySymbol
                    : (3 - _mySymbol);

                _board[dto.Y][dto.X] = symbol;
                _cells[dto.Y, dto.X]?.Invalidate();
                
                // FINAL PIECE MUST ACTUALLY PAINT FIRST BEFORE RESULT SHELL IS SHOWN
                if (dto.WinnerSymbol != 0)
                {
                    _cells[dto.Y, dto.X]?.Update();
                }

                // Lưu tọa độ nước đi cuối cùng để tính winning line
                _lastAcceptedMoveRow = dto.Y;
                _lastAcceptedMoveCol = dto.X;
                _lastAcceptedMoveSymbol = symbol;

                if (symbol == 1)
                {
                    if (int.TryParse(lblPlayer1MoveCount.Text, out int count))
                        lblPlayer1MoveCount.Text = (count + 1).ToString();
                }
                else
                {
                    if (int.TryParse(lblPlayer2MoveCount.Text, out int count))
                        lblPlayer2MoveCount.Text = (count + 1).ToString();
                }

                // Cập nhật indicator lượt đánh (Chỉ cập nhật nếu chưa GameOver để tránh đè trạng thái CHIẾN THẮNG)
                if (!_isSpectator && dto.WinnerSymbol == 0)
                {
                    bool isMyTurn = (dto.PlayerId != CaroClient.Network.NetworkClient.Instance.CurrentNickname);
                    UpdateTurnBadges(isMyTurn);
                }

                // Reset timer cho lượt tiếp theo
                if (dto.WinnerSymbol == 0)
                {
                    StartTurnTimer(CaroShared.Constants.GameConstants.TurnTimeoutSeconds);
                }
                else
                {
                    // PHASE A: TRUE INSTANT RESULT UI FOR NORMAL WIN
                    // BOARD LOCK: ngay khi MoveMadeEvent xác nhận có winner,
                    // khóa input và dừng timer.
                    _isGameOver = true;
                    StopTurnTimer();

                    if (!_gameOverPresentationStarted)
                    {
                        _gameOverPresentationStarted = true;

                        // --- Determine local winner/loser ---
                        _isLocalWinner = !_isSpectator && dto.WinnerSymbol == _mySymbol;

                        // --- Cập nhật Winner / Loser Player Cards ---
                        if (dto.WinnerSymbol == 1)
                        {
                            pnlPlayer1Turn.Text = "🏆 CHIẾN THẮNG";
                            pnlPlayer2Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer1.IsWinnerHighlighted = true;
                            pnlPlayer2.IsWinnerHighlighted = false;
                        }
                        else if (dto.WinnerSymbol == 2)
                        {
                            pnlPlayer2Turn.Text = "🏆 CHIẾN THẮNG";
                            pnlPlayer1Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer2.IsWinnerHighlighted = true;
                            pnlPlayer1.IsWinnerHighlighted = false;
                        }
                        else
                        {
                            pnlPlayer1Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer2Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer1.IsWinnerHighlighted = false;
                            pnlPlayer2.IsWinnerHighlighted = false;
                        }
                        pnlPlayer1.Invalidate();
                        pnlPlayer2.Invalidate();
                        pnlPlayer1Turn.Invalidate();
                        pnlPlayer2Turn.Invalidate();

                        // --- Winning line highlight ---
                        if (_lastAcceptedMoveRow >= 0 && dto.WinnerSymbol != 0)
                        {
                            FindWinningLine(_lastAcceptedMoveRow, _lastAcceptedMoveCol, _lastAcceptedMoveSymbol);
                            InvalidateWinningCells();
                        }

                        // --- Chuẩn bị text cho Result Dialog ---
                        string resultTitle;
                        string resultText;

                        if (dto.WinnerSymbol == 0)
                        {
                            resultTitle = "🤝 HÒA!";
                            resultText = "Ván đấu kết thúc hòa.";
                        }
                        else if (_isSpectator)
                        {
                            resultTitle = "Kết Thúc Ván Đấu";
                            resultText = dto.WinnerSymbol == 1 ? "X THẮNG!"
                                       : dto.WinnerSymbol == 2 ? "O THẮNG!"
                                       : "🤝 HÒA!";
                        }
                        else if (_isLocalWinner)
                        {
                            string myName = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
                            resultTitle = "🏆 CHIẾN THẮNG!";
                            resultText = $"Chúc mừng {myName}!\nBạn đã chiến thắng.";
                        }
                        else
                        {
                            resultTitle = "Kết Thúc Trận";
                            resultText = "Đối thủ đã chiến thắng.";
                        }

                        ShowNonBlockingResultShell(resultTitle, resultText);

                        if (_isLocalWinner)
                        {
                            Point centerPt;
                            if (_winningCells.Count > 0)
                            {
                                int cellX = pnlWoodFrame.Left + pnlBoardContainer.Left + (_lastAcceptedMoveCol * _currentCellSize) + (_currentCellSize / 2);
                                int cellY = pnlWoodFrame.Top + pnlBoardContainer.Top + (_lastAcceptedMoveRow * _currentCellSize) + (_currentCellSize / 2);
                                centerPt = new Point(cellX, cellY);
                            }
                            else
                            {
                                centerPt = new Point(
                                    pnlWoodFrame.Left + pnlBoardContainer.Left + (pnlBoardContainer.Width / 2),
                                    pnlWoodFrame.Top + pnlBoardContainer.Top + (pnlBoardContainer.Height / 2)
                                );
                            }

                            StopCelebrationOverlay();
                            string myName = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
                            _celebrationOverlay = new WinCelebrationOverlay(this, myName, centerPt, pnlWoodFrame.Bounds);
                            _celebrationOverlay.Start();
                        }
                    }
                }
            }
        }

        private void HandleGameOver(CaroShared.Protocol.NetworkMessage msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleGameOver(msg)));
                return;
            }

            // Phase B Idempotency Guard: Ngăn chặn xử lý lặp lại nếu đã nhận GameOver authoritative
            if (_gameOverEventReceived) return;
            _gameOverEventReceived = true;

            // Dừng timer (để tránh xử thua vô nghĩa khi trận đã kết thúc)
            StopTurnTimer();
            _isGameOver = true;

            if (msg.Payload is System.Text.Json.JsonElement element)
            {
                // Note: Server đang dùng MoveMadeEventDto làm GameOverPayload
                var dto = element.Deserialize<CaroShared.Contracts.MoveMadeEventDto>(
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (dto != null)
                {
                    // Fallback Phase A: Nếu presentation chưa chạy (ví dụ: timeout, surrender, draw...)
                    if (!_gameOverPresentationStarted)
                    {
                        _gameOverPresentationStarted = true;

                        // Đặt quân cuối nếu chưa có
                        if (dto.IsValid && !string.IsNullOrEmpty(dto.PlayerId)
                            && dto.X >= 0 && dto.X < BoardSize && dto.Y >= 0 && dto.Y < BoardSize
                            && _board[dto.Y][dto.X] == 0)
                        {
                            int symbol = (dto.PlayerId == CaroClient.Network.NetworkClient.Instance.CurrentNickname) ? _mySymbol : (3 - _mySymbol);
                            _board[dto.Y][dto.X] = symbol;
                            _lastAcceptedMoveRow = dto.Y;
                            _lastAcceptedMoveCol = dto.X;
                            _lastAcceptedMoveSymbol = symbol;
                            _cells[dto.Y, dto.X]?.Invalidate();
                        }

                        // Determine winner
                        _isLocalWinner = !_isSpectator && dto.WinnerSymbol == _mySymbol;

                        // Cập nhật Player Cards
                        if (dto.WinnerSymbol == 1)
                        {
                            pnlPlayer1Turn.Text = "🏆 CHIẾN THẮNG";
                            pnlPlayer2Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer1.IsWinnerHighlighted = true;
                            pnlPlayer2.IsWinnerHighlighted = false;
                        }
                        else if (dto.WinnerSymbol == 2)
                        {
                            pnlPlayer2Turn.Text = "🏆 CHIẾN THẮNG";
                            pnlPlayer1Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer2.IsWinnerHighlighted = true;
                            pnlPlayer1.IsWinnerHighlighted = false;
                        }
                        else
                        {
                            pnlPlayer1Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer2Turn.Text = "KẾT THÚC TRẬN";
                            pnlPlayer1.IsWinnerHighlighted = false;
                            pnlPlayer2.IsWinnerHighlighted = false;
                        }
                        pnlPlayer1.Invalidate();
                        pnlPlayer2.Invalidate();
                        pnlPlayer1Turn.Invalidate();
                        pnlPlayer2Turn.Invalidate();

                        string resultTitle;
                        string resultText;

                        if (_isSpectator)
                        {
                            resultTitle = "Kết Thúc Ván Đấu";
                            resultText = dto.WinnerSymbol == 1 ? "X THẮNG!"
                                       : dto.WinnerSymbol == 2 ? "O THẮNG!"
                                       : "🤝 HÒA!";
                        }
                        else if (_isLocalWinner)
                        {
                            string myName = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
                            resultTitle = "🏆 CHIẾN THẮNG!";
                            resultText = $"Chúc mừng {myName}!\nBạn đã chiến thắng.";
                        }
                        else if (dto.WinnerSymbol == 0)
                        {
                            resultTitle = "Kết Thúc Ván Đấu";
                            resultText = "🤝 HÒA!";
                        }
                        else
                        {
                            resultTitle = "Kết Thúc Trận";
                            resultText = "Đối thủ đã chiến thắng.";
                        }

                        string detail = !string.IsNullOrEmpty(dto.ErrorMessage) ? $"\n{dto.ErrorMessage}" : "";

                        ShowNonBlockingResultShell(resultTitle, $"{resultText}{detail}");

                        // Celebration (light)
                        if (_isLocalWinner)
                        {
                            Point centerPt = new Point(
                                pnlWoodFrame.Left + pnlBoardContainer.Left + (pnlBoardContainer.Width / 2),
                                pnlWoodFrame.Top + pnlBoardContainer.Top + (pnlBoardContainer.Height / 2)
                            );
                            StopCelebrationOverlay();
                            string myName = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
                            _celebrationOverlay = new WinCelebrationOverlay(this, myName, centerPt, pnlWoodFrame.Bounds);
                            _celebrationOverlay.Start();
                        }
                    }
                    
                    // PHASE B: FINALIZE
                    FinalizeResultShell(!dto.IsValid ? dto.ErrorMessage : null);
                }
            }
        }

        private void HandleMessageReceived(CaroShared.Protocol.NetworkMessage msg)
        {
            if (msg.Type == CaroShared.Enums.MessageType.NewGameEvent)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => HandleMessageReceived(msg)));
                    return;
                }

                if (msg.Payload is JsonElement el)
                {
                    var newGameEvent = el.Deserialize<NewGameEventDto>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (newGameEvent != null)
                    {
                        _currentMatchId = newGameEvent.MatchIdentity;
                    }
                }

                // Reset toàn bộ game state VÀ presentation state
                _isGameOver = false;
                _gameOverPresentationStarted = false;
                _gameOverEventReceived = false;
                if (_resultShellForm != null)
                {
                    if (!_resultShellForm.IsDisposed) _resultShellForm.Close();
                    _resultShellForm.Dispose();
                    _resultShellForm = null;
                }
                _winningCells.Clear();
                _orderedWinningCells.Clear();
                _winningDirDr = 0;
                _winningDirDc = 0;
                _lastAcceptedMoveRow = -1;
                _lastAcceptedMoveCol = -1;
                _lastAcceptedMoveSymbol = 0;
                _isLocalWinner = false;
                StopCelebrationOverlay();
                BackdropOverlay.ClearFor(this);
                if (_pnlDrawRequest != null) _pnlDrawRequest.Visible = false;
                btnOfferDraw.Text = "HÒA";
                btnOfferDraw.Enabled = true;
                ResetBoard();
                CaroDialogForm.Show(this, "Ván mới đã bắt đầu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Winning Line — Visual calculation (presentation ONLY, never
        //  changes game state). Matches server's CaroEngine rules exactly:
        //  - 4 consecutive = not win
        //  - exactly 5 = win UNLESS both ends are blocked
        //  - >5 consecutive = always win
        //  - checks all 4 directions
        // ══════════════════════════════════════════════════════════════════
        private void FindWinningLine(int row, int col, int symbol)
        {
            _winningCells.Clear();
            _orderedWinningCells.Clear();
            if (symbol == 0 || row < 0 || col < 0) return;

            int[][] directions = new int[][]
            {
                new int[] { 0, 1 },  // vertical (row changes)
                new int[] { 1, 0 },  // horizontal (col changes)
                new int[] { 1, 1 },  // diagonal \
                new int[] { 1, -1 }  // diagonal /
            };

            foreach (var dir in directions)
            {
                int dr = dir[0];
                int dc = dir[1];

                // Count forward
                var forward = new List<(int r, int c)>();
                int nr = row + dr, nc = col + dc;
                while (nr >= 0 && nr < BoardSize && nc >= 0 && nc < BoardSize && _board[nr][nc] == symbol)
                {
                    forward.Add((nr, nc));
                    nr += dr;
                    nc += dc;
                }

                // Count backward
                var backward = new List<(int r, int c)>();
                nr = row - dr; nc = col - dc;
                while (nr >= 0 && nr < BoardSize && nc >= 0 && nc < BoardSize && _board[nr][nc] == symbol)
                {
                    backward.Add((nr, nc));
                    nr -= dr;
                    nc -= dc;
                }

                int total = forward.Count + backward.Count + 1;

                if (total < 5) continue;

                bool isWin;
                if (total > 5)
                {
                    isWin = true;
                }
                else
                {
                    // Exactly 5: check both-ends-blocked rule
                    int headR = row - (backward.Count + 1) * dr;
                    int headC = col - (backward.Count + 1) * dc;
                    int tailR = row + (forward.Count + 1) * dr;
                    int tailC = col + (forward.Count + 1) * dc;

                    bool headBlocked = IsVisualBlocked(headR, headC, symbol);
                    bool tailBlocked = IsVisualBlocked(tailR, tailC, symbol);
                    isWin = !(headBlocked && tailBlocked);
                }

                if (isWin)
                {
                    _winningDirDr = dr;
                    _winningDirDc = dc;

                    // Lưu theo thứ tự từ đầu đến đuôi của chuỗi quân thắng
                    for (int i = backward.Count - 1; i >= 0; i--)
                    {
                        _orderedWinningCells.Add(backward[i]);
                        _winningCells.Add(backward[i]);
                    }
                    _orderedWinningCells.Add((row, col));
                    _winningCells.Add((row, col));
                    foreach (var cell in forward)
                    {
                        _orderedWinningCells.Add(cell);
                        _winningCells.Add(cell);
                    }
                    return; // First winning direction found is sufficient
                }
            }
        }

        private bool IsVisualBlocked(int row, int col, int symbol)
        {
            if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize)
                return true;
            if (_board[row][col] == 0)
                return false;
            return _board[row][col] != symbol;
        }

        private void InvalidateWinningCells()
        {
            foreach (var (row, col) in _winningCells)
            {
                _cells[row, col]?.Invalidate();
            }
        }

        public void UpdateCelebrationProgress(int elapsedMs)
        {
            _celebrationElapsedMs = elapsedMs;
            InvalidateWinningCells();
        }

        private void StopCelebrationOverlay()
        {
            if (_celebrationOverlay != null)
            {
                _celebrationOverlay.StopAndDispose();
                _celebrationOverlay = null;
            }
            _celebrationElapsedMs = 0;
        }
        // ── HÒA (DRAW) SYSTEM ─────────────────────────────────────────────
        private Guid _currentOfferId;
        private Soft3DPanel? _pnlDrawRequest;

        private void SetupDrawUI()
        {
            btnOfferDraw.Click += BtnOfferDraw_Click;

            // Bảng thông báo đề nghị hòa dạng thẻ Soft3D nổi bật, KHÔNG dùng backdrop che mờ để đảm bảo không chặn thao tác bàn cờ (Non-blocking)
            _pnlDrawRequest = new Soft3DPanel
            {
                Size = new Size(420, 115),
                CornerRadius = 16,
                Visible = false
            };

            var lblTitle = new Label
            {
                Name = "lblDrawTitle",
                Text = "🤝 ĐỀ NGHỊ HÒA",
                ForeColor = CaroTheme.TextDark,
                AutoSize = false,
                Size = new Size(388, 24),
                Location = new Point(16, 12),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var lblMsg = new Label
            {
                Name = "lblDrawMsg",
                Text = "Đối thủ đề nghị kết thúc trận hòa.",
                ForeColor = CaroTheme.TextMuted,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(388, 30),
                Location = new Point(16, 36),
                Font = new Font("Segoe UI", 9.5f),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            var btnAccept = new PillButton
            {
                Name = "btnAcceptDraw",
                Text = "CHẤP NHẬN",
                Size = new Size(125, 34),
                Location = new Point(75, 70),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.None
            };

            var btnReject = new PillButton
            {
                Name = "btnRejectDraw",
                Text = "TỪ CHỐI",
                Size = new Size(125, 34),
                Location = new Point(220, 70),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                IsDestructive = true,
                DialogResult = DialogResult.None
            };

            btnAccept.Click += (s, e) =>
            {
                btnAccept.Enabled = false;
                btnReject.Enabled = false;
                SendDrawResponse(true);
            };

            btnReject.Click += (s, e) =>
            {
                btnAccept.Enabled = false;
                btnReject.Enabled = false;
                SendDrawResponse(false);
            };

            _pnlDrawRequest.Controls.Add(lblTitle);
            _pnlDrawRequest.Controls.Add(lblMsg);
            _pnlDrawRequest.Controls.Add(btnAccept);
            _pnlDrawRequest.Controls.Add(btnReject);

            this.Controls.Add(_pnlDrawRequest);
            _pnlDrawRequest.BringToFront();
        }

        private void BtnOfferDraw_Click(object? sender, EventArgs e)
        {
            if (_isSpectator || _isGameOver) return;

            var req = new CaroShared.Contracts.DrawOfferRequestDto
            {
                RoomId = _roomId,
                MatchIdentity = _currentMatchId
            };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.DrawOfferRequest, req);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
            
            // Tạm disable nút HÒA và hiển thị trạng thái đang chờ
            btnOfferDraw.Text = "ĐANG CHỜ...";
            btnOfferDraw.Enabled = false;
        }

        private void SendDrawResponse(bool accept)
        {
            if (_pnlDrawRequest != null) _pnlDrawRequest.Visible = false;

            var req = new CaroShared.Contracts.DrawResponseRequestDto
            {
                RoomId = _roomId,
                MatchIdentity = _currentMatchId,
                OfferIdentity = _currentOfferId,
                Accept = accept
            };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.DrawResponseRequest, req);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        private void HandleDrawOfferReceived(CaroShared.Contracts.DrawOfferEventDto dto)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleDrawOfferReceived(dto)));
                return;
            }

            if (_isSpectator || _isGameOver || dto.MatchIdentity != _currentMatchId) return;

            _currentOfferId = dto.OfferIdentity;
            
            if (_pnlDrawRequest != null)
            {
                string offerer = !string.IsNullOrWhiteSpace(dto.OfferedByPlayerName)
                    ? dto.OfferedByPlayerName
                    : (!string.IsNullOrWhiteSpace(dto.OfferedByPlayerId) ? dto.OfferedByPlayerId : "Đối thủ");

                var lblMsg = _pnlDrawRequest.Controls["lblDrawMsg"] as Label;
                if (lblMsg != null)
                {
                    lblMsg.Text = $"{offerer} đề nghị kết thúc trận hòa.";
                }

                foreach (Control c in _pnlDrawRequest.Controls)
                {
                    if (c is Button b) b.Enabled = true;
                }
                _pnlDrawRequest.Location = new Point(
                    (this.ClientSize.Width - _pnlDrawRequest.Width) / 2,
                    (this.ClientSize.Height - _pnlDrawRequest.Height) / 2
                );
                _pnlDrawRequest.Visible = true;
                _pnlDrawRequest.BringToFront();
            }
        }

        private void HandleDrawOfferResolved(CaroShared.Contracts.DrawOfferResolvedDto dto)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleDrawOfferResolved(dto)));
                return;
            }

            if (dto.MatchIdentity != _currentMatchId) return;

            if (_pnlDrawRequest != null) _pnlDrawRequest.Visible = false;

            if (dto.Cancelled)
            {
                btnOfferDraw.Text = "HÒA";
                if (!_isGameOver) btnOfferDraw.Enabled = true;
            }
            else if (!dto.Accepted)
            {
                btnOfferDraw.Text = "HÒA";
                if (!_isGameOver) btnOfferDraw.Enabled = true;
                CaroDialogForm.Show(this, dto.Message, "Hòa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                btnOfferDraw.Text = "HÒA";
            }
        }
    }
}
