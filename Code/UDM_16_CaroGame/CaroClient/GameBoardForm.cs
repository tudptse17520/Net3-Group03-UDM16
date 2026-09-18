using CaroClient.Network;
using CaroShared.Constants;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroClient
{
    public partial class GameBoardForm : Form
    {
        // ── Hằng số bàn cờ (độc lập, không import CaroShared) ─────────────
        private const int BoardSize = 15;
        private const int CellSize  = 44; // px, ô vuông 44×44

        // ── Trạng thái bàn cờ ─────────────────────────────────────────────
        // 0 = trống | 1 = X (Player 1) | 2 = O (Player 2)
        private int[][] _board = CreateJaggedBoard();

        // ── Gameplay state ─────────────────────────────────────
        private string _roomId = string.Empty;
        private NetworkClient? _networkClient;
        private string _myPlayerId = string.Empty;
        private int _mySymbol;          // 1=X hoặc 2=O
        private bool _isMyTurn;
        private bool _isGameOver;
        private int _moveCountP1;
        private int _moveCountP2;

        // ── Spectator ─────────────────────────────────────────────────────
        private bool _isSpectator = false;

        // ── Quản lý đồng hồ đếm ngược ──────────────────────────────────────
        private System.Windows.Forms.Timer? _countdownTimer;
        private int _remainingSeconds = 0;

        private static int[][] CreateJaggedBoard()
        {
            var b = new int[BoardSize][];
            for (int i = 0; i < BoardSize; i++)
            {
                b[i] = new int[BoardSize];
            }
            return b;
        }

        // ── Tham chiếu các ô nút ──────────────────────────────────────────
        private Button[,] _cells = new Button[BoardSize, BoardSize];

        // ── Constructor mặc định (Designer cần) ───────────────────────────
        public GameBoardForm()
        {
            InitializeComponent();
            InitBoard();

            // Đăng ký sự kiện Resize để căn giữa cụm chơi
            this.Resize += GameBoardForm_Resize;
        }

        // ── Constructor gameplay ───────────────────────────────
        public GameBoardForm(string myPlayerId,
                             int mySymbol, string p1Name, string p2Name)
            : this()
        {
            _networkClient = CaroClient.Network.NetworkClient.Instance;
            _myPlayerId = myPlayerId;
            _mySymbol = mySymbol;
            _isMyTurn = (mySymbol == 1);   // X đi trước

            // Cập nhật tên player trên UI
            lblPlayer1Name.Text = p1Name;
            lblPlayer2Name.Text = p2Name;

            // Đăng ký nhận event từ server
            if (_networkClient != null)
            {
                _networkClient.OnMoveMade += HandleMoveMade;
                _networkClient.OnGameOver += HandleGameOver;
            }

            // Cập nhật indicator lượt đi ban đầu
            UpdateTurnIndicator();
            
            // Tắt nút new game ban đầu
            btnNewGame.Enabled = false;
        }

        // ── Constructor Spectator ─────────────────────────────────────────
        /// <summary>
        /// Mở form ở chế độ Khán giả: chỉ xem, không đánh được.
        /// </summary>
        public GameBoardForm(SpectatorStateSnapshotDto snapshot) : this()
        {
            _isSpectator = true;

            // 1. Load tên người chơi
            lblPlayer1Name.Text = snapshot.Room?.PlayerX ?? "Player X";
            lblPlayer2Name.Text = snapshot.Room?.PlayerO ?? "Player O";

            // 2. Load trạng thái bàn cờ hiện tại
            if (snapshot.Session?.Board != null)
                UpdateBoard(snapshot.Session.Board);

            // 3. Cập nhật UI cho chế độ Spectator
            ApplySpectatorUI();

            // 4. Bắt đầu timer từ thông tin thời gian snapshot của Server
            if (snapshot.Session != null && snapshot.Session.RemainingTimeSeconds > 0)
            {
                StartTurnTimer(snapshot.Session.RemainingTimeSeconds);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  ApplySpectatorUI — thiết lập giao diện "chỉ xem"
        // ══════════════════════════════════════════════════════════════════
        private void ApplySpectatorUI()
        {
            // Đổi tiêu đề
            this.Text = "CARO ONLINE — Chế độ Khán giả 👁️";
            lblAppTitle.Text = "👁️ KHÁN GIẢ";

            // Đổi nút Đầu hàng → Thoát phòng
            btnSurrender.Text = "THOÁT PHÒNG";

            // Lock các nút không liên quan
            btnOfferDraw.Enabled = false;
            btnNewGame.Enabled = false;

            // Cập nhật indicator lượt
            pnlPlayer1Turn.Text = "Đang xem...";
            pnlPlayer1Turn.ForeColor = Color.CornflowerBlue;
            pnlPlayer2Turn.Text = "Đang xem...";
            pnlPlayer2Turn.ForeColor = Color.CornflowerBlue;

            // Đổi cursor các ô cờ → không phải bàn tay
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    _cells[row, col].Cursor = Cursors.Default;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  InitBoard — khởi tạo bàn cờ bằng 2 vòng lặp
        // ══════════════════════════════════════════════════════════════════
        private void InitBoard()
        {
            // Tự động fit panel theo đúng kích thước bàn cờ (15 × 44 = 660px)
            int boardPixelSize = BoardSize * CellSize;
            pnlBoardContainer.Size = new Size(boardPixelSize, boardPixelSize);


            pnlBoardContainer.Controls.Clear();
            pnlBoardContainer.BackColor = Color.FromArgb(40, 40, 40);

            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    var btn = new Button
                    {
                        Width     = CellSize,
                        Height    = CellSize,
                        Left      = col * CellSize,
                        Top       = row * CellSize,
                        Tag       = (row, col),
                        Text      = "",
                        Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(245, 222, 179),  // màu gỗ nhạt
                        ForeColor = Color.Black,
                        Cursor    = Cursors.Hand,
                        TabStop   = false,
                    };
                    btn.FlatAppearance.BorderColor = Color.FromArgb(120, 80, 30);
                    btn.FlatAppearance.BorderSize  = 1;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 180, 140);
                    btn.Click += Cell_Click;

                    pnlBoardContainer.Controls.Add(btn);
                    _cells[row, col] = btn;
                }
            }

            // Căn giữa lần đầu
            CenterLayout();
        }

        // ══════════════════════════════════════════════════════════════════
        //  CenterLayout — căn giữa cụm chơi trong Form
        // ══════════════════════════════════════════════════════════════════
        private void CenterLayout()
        {
            // Căn giữa bàn cờ trong Form
            pnlBoardContainer.Left = (this.ClientSize.Width - pnlBoardContainer.Width) / 2;

            // Căn chỉnh panel Player 1 bên trái bàn cờ
            pnlPlayer1.Top = pnlBoardContainer.Top;
            pnlPlayer1.Left = pnlBoardContainer.Left - pnlPlayer1.Width - 20;

            // Căn chỉnh panel Player 2 và các nút bên phải bàn cờ
            pnlPlayer2.Top = pnlBoardContainer.Top;
            pnlPlayer2.Left = pnlBoardContainer.Right + 20;

            btnSurrender.Left = pnlPlayer2.Left;
            btnOfferDraw.Left = pnlPlayer2.Left;
            btnNewGame.Left = pnlPlayer2.Left;

            lblTime.Left = pnlPlayer2.Left;
            lblTimeCount.Left = lblTime.Right + 5;
        }

        // ── Sự kiện Resize ────────────────────────────────────────────────
        private void GameBoardForm_Resize(object? sender, EventArgs e)
        {
            CenterLayout();
        }

        // ══════════════════════════════════════════════════════════════════
        //  Cell_Click — kiểm tra lượt trước khi gửi
        // ══════════════════════════════════════════════════════════════════
        private void Cell_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            var (row, col) = ((int, int))btn.Tag!;

            if (_isSpectator) return;
            if (_isGameOver) return;
            if (!_isMyTurn) return;

            // Chỉ cho phép đánh vào ô trống
            if (_board[row][col] != 0) return;

            SendMove(row, col);
        }

        // ══════════════════════════════════════════════════════════════════
        //  SendMove — gửi MakeMoveRequest lên server
        // ══════════════════════════════════════════════════════════════════
        private async void SendMove(int row, int col)
        {
            if (_networkClient == null) return;

            _isMyTurn = false;    // Chặn click tiếp ngay lập tức

            var request = new MakeMoveRequest { X = col, Y = row };
            var msg = new NetworkMessage(MessageType.MakeMoveRequest, request);

            try
            {
                await _networkClient.SendMessageAsync(msg);
            }
            catch (Exception ex)
            {
                _isMyTurn = true;  // Lỗi → cho đánh lại
                MessageBox.Show($"Lỗi gửi nước đi: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  HandleMoveMade — xử lý nước đi từ server
        // ══════════════════════════════════════════════════════════════════
        private void HandleMoveMade(MoveMadeEventDto dto)
        {
            if (IsDisposed) return;

            this.Invoke(() =>
            {
                // 1. Nước đi không hợp lệ → cho đánh lại
                if (!dto.IsValid)
                {
                    MessageBox.Show(dto.ErrorMessage, "Không hợp lệ",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _isMyTurn = true;
                    return;
                }

                // 2. Xác định symbol: ai vừa đánh?
                bool isMyMove = (dto.PlayerId == _myPlayerId);
                int symbol = isMyMove ? _mySymbol : (_mySymbol == 1 ? 2 : 1);

                // 3. Cập nhật 1 ô trên bàn cờ
                int row = dto.Y;
                int col = dto.X;
                _board[row][col] = symbol;
                _cells[row, col].Text = symbol == 1 ? "X" : "O";
                _cells[row, col].ForeColor = symbol == 1 ? Color.DarkBlue : Color.DarkRed;

                // 4. Cập nhật đếm nước đi
                if (symbol == 1) _moveCountP1++;
                else             _moveCountP2++;
                lblPlayer1MoveCount.Text = _moveCountP1.ToString();
                lblPlayer2MoveCount.Text = _moveCountP2.ToString();

                // 5. Chuyển lượt
                _isMyTurn = !isMyMove;
                UpdateTurnIndicator();

                // 6. Kiểm tra kết thúc game
                if (dto.WinnerSymbol != 0)
                {
                    ShowGameResult(dto.WinnerSymbol);
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════
        //  HandleGameOver — xử lý đầu hàng/timeout 
        // ══════════════════════════════════════════════════════════════════
        private void HandleGameOver(NetworkMessage msg)
        {
            if (IsDisposed) return;

            this.Invoke(() =>
            {
                // Cố gắng đọc payload nếu có
                try
                {
                    var serializer = new MessageSerializer();
                    var dto = serializer.DeserializePayload<MoveMadeEventDto>(msg);
                    ShowGameResult(dto.WinnerSymbol);
                }
                catch
                {
                    // Payload không parse được → hiển thị thông báo chung
                    ShowGameResult(0);  // 0 = hòa / không xác định
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════
        //  ShowGameResult — hiển thị kết quả game
        // ══════════════════════════════════════════════════════════════════
        private void ShowGameResult(int winnerSymbol)
        {
            _isGameOver = true;

            // Disable toàn bộ ô cờ
            SetBoardEnabled(false);

            // Xác định thông báo
            string message;
            string title;
            MessageBoxIcon icon;

            if (winnerSymbol == 0)
            {
                message = "Trận đấu kết thúc hòa!";
                title = "Hòa";
                icon = MessageBoxIcon.Information;
            }
            else if (winnerSymbol == _mySymbol)
            {
                message = "🎉 Chúc mừng! Bạn đã thắng!";
                title = "Chiến thắng";
                icon = MessageBoxIcon.Information;
            }
            else
            {
                message = "😢 Bạn đã thua! Chúc may mắn lần sau.";
                title = "Thua cuộc";
                icon = MessageBoxIcon.Information;
            }

            MessageBox.Show(message, title, MessageBoxButtons.OK, icon);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Helper methods
        // ══════════════════════════════════════════════════════════════════
        private void UpdateTurnIndicator()
        {
            if (_isMyTurn)
            {
                // Highlight panel của mình
                if (_mySymbol == 1)
                {
                    pnlPlayer1Turn.Text = "▶ Lượt của bạn";
                    pnlPlayer1Turn.ForeColor = Color.Green;
                    pnlPlayer2Turn.Text = "Chờ...";
                    pnlPlayer2Turn.ForeColor = Color.Gray;
                }
                else
                {
                    pnlPlayer2Turn.Text = "▶ Lượt của bạn";
                    pnlPlayer2Turn.ForeColor = Color.Green;
                    pnlPlayer1Turn.Text = "Chờ...";
                    pnlPlayer1Turn.ForeColor = Color.Gray;
                }
            }
            else
            {
                // Highlight panel đối thủ
                if (_mySymbol == 1)
                {
                    pnlPlayer1Turn.Text = "Chờ...";
                    pnlPlayer1Turn.ForeColor = Color.Gray;
                    pnlPlayer2Turn.Text = "▶ Đang đánh...";
                    pnlPlayer2Turn.ForeColor = Color.Orange;
                }
                else
                {
                    pnlPlayer2Turn.Text = "Chờ...";
                    pnlPlayer2Turn.ForeColor = Color.Gray;
                    pnlPlayer1Turn.Text = "▶ Đang đánh...";
                    pnlPlayer1Turn.ForeColor = Color.Orange;
                }
            }
        }

        private void SetBoardEnabled(bool enabled)
        {
            for (int r = 0; r < BoardSize; r++)
                for (int c = 0; c < BoardSize; c++)
                    _cells[r, c].Enabled = enabled;
        }

        // ── Cập nhật UI từ dữ liệu server gửi về ─────────────────────────
        // board: mảng 15×15 (0=trống, 1=X, 2=O)
        public void UpdateBoard(int[][] board)
        {
            for (int row = 0; row < BoardSize; row++)
            {
                for (int col = 0; col < BoardSize; col++)
                {
                    _board[row][col] = board[row][col];
                    _cells[row, col].Text = board[row][col] switch
                    {
                        1 => "X",
                        2 => "O",
                        _ => ""
                    };
                    _cells[row, col].ForeColor = board[row][col] switch
                    {
                        1 => Color.DarkBlue,
                        2 => Color.DarkRed,
                        _ => Color.Black
                    };
                }
            }
        }

        // ── Reset bàn cờ về trạng thái ban đầu ───────────────────────────
        public void ResetBoard()
        {
            StopTurnTimer();
            _board = CreateJaggedBoard();
            for (int row = 0; row < BoardSize; row++)
                for (int col = 0; col < BoardSize; col++)
                {
                    _cells[row, col].Text      = "";
                    _cells[row, col].BackColor = Color.FromArgb(245, 222, 179);
                }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Quản lý đồng hồ đếm ngược thời gian (Client Timer UI)
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Bắt đầu đếm ngược lượt mới với số giây quy định.
        /// </summary>
        /// <param name="seconds">Số giây đếm ngược (Mặc định: TurnTimeoutSeconds = 30s)</param>
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

        /// <summary>
        /// Dừng đếm ngược thời gian.
        /// </summary>
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
                // Khi đồng hồ về 00:00: Dừng timer client, giữ 00:00 và chờ Server xử lý Timeout
                StopTurnTimer();
            }
        }

        /// <summary>
        /// Cập nhật hiển thị label thời gian đếm ngược.
        /// </summary>
        private void UpdateTimerUI()
        {
            if (lblTimeCount.InvokeRequired)
            {
                lblTimeCount.Invoke(new Action(UpdateTimerUI));
                return;
            }

            int minutes = _remainingSeconds / 60;
            int secs = _remainingSeconds % 60;
            lblTimeCount.Text = $"{minutes:D2}:{secs:D2}";

            // Đổi màu đỏ cảnh báo khi còn <= 5 giây
            if (_remainingSeconds <= 5)
            {
                lblTimeCount.ForeColor = Color.Red;
            }
            else
            {
                lblTimeCount.ForeColor = Color.DarkOrange;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopTurnTimer();
            // Hủy đăng ký event tránh memory leak
            if (_networkClient != null)
            {
                _networkClient.OnMoveMade -= HandleMoveMade;
                _networkClient.OnGameOver -= HandleGameOver;
            }
            base.OnFormClosing(e);
        }

        // ════════════════════════════════════════════════════════════════════
        // Event handler stubs (wired in Designer.cs)
        // ════════════════════════════════════════════════════════════════════

        private void label1_Click(object sender, EventArgs e) { }

        // btnSurrender / Thoát phòng (Spectator)
        private async void button1_Click(object sender, EventArgs e)
        {
            // Nếu là Spectator, nút này là "Thoát phòng"
            if (_isSpectator)
            {
                this.Close();
                return;
            }

            if (_isGameOver || _networkClient == null) return;

            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đầu hàng?",
                "Xác nhận đầu hàng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var req = new CaroShared.Contracts.SurrenderRequest
                {
                    RoomId   = _roomId,
                    PlayerId = _networkClient.CurrentNickname
                };
                var msg = new CaroShared.Protocol.NetworkMessage(
                    CaroShared.Enums.MessageType.SurrenderRequest, req);
                _ = _networkClient.SendMessageAsync(msg);
                // Không close ngay — Server sẽ broadcast GameOverEvent để kết thúc ván
            }

        }

        // btnNewGame
        private void button3_Click(object sender, EventArgs e)
        {
            if (!_isGameOver) return;   // Chỉ cho phép khi game đã kết thúc

            ResetBoard();
            _isGameOver = false;
            _moveCountP1 = 0;
            _moveCountP2 = 0;
            lblPlayer1MoveCount.Text = "0";
            lblPlayer2MoveCount.Text = "0";
            _isMyTurn = (_mySymbol == 1);
            UpdateTurnIndicator();
            SetBoardEnabled(true);
        }

        private void pictureBox1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label2_Click_1(object sender, EventArgs e) { }
        private void pictureBox1_Click_1(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label3_Click_1(object sender, EventArgs e) { }
        private void label9_Click(object sender, EventArgs e) { }


    }
}
