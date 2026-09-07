using CaroClient.Network;
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

        // ── Gameplay state (Bước 3.1) ─────────────────────────────────────
        private NetworkClient? _networkClient;
        private string _myPlayerId = string.Empty;
        private int _mySymbol;          // 1=X hoặc 2=O
        private bool _isMyTurn;
        private bool _isGameOver;
        private int _moveCountP1;
        private int _moveCountP2;

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
        }

        // ── Constructor gameplay (Bước 3.2) ───────────────────────────────
        public GameBoardForm(NetworkClient client, string myPlayerId,
                             int mySymbol, string p1Name, string p2Name)
            : this()
        {
            _networkClient = client;
            _myPlayerId = myPlayerId;
            _mySymbol = mySymbol;
            _isMyTurn = (mySymbol == 1);   // X đi trước

            // Cập nhật tên player trên UI
            lblPlayer1Name.Text = p1Name;
            lblPlayer2Name.Text = p2Name;

            // Đăng ký nhận event từ server
            _networkClient.OnMoveMade += HandleMoveMade;
            _networkClient.OnGameOver += HandleGameOver;

            // Cập nhật indicator lượt đi ban đầu
            UpdateTurnIndicator();
        }

        // ── Khởi tạo bàn cờ bằng 2 vòng lặp ─────────────────────────────
        private void InitBoard()
        {
            // Tự động fit panel theo đúng kích thước bàn cờ (15 × 44 = 660px)
            int boardPixelSize = BoardSize * CellSize;
            pnlBoardContainer.Size = new Size(boardPixelSize, boardPixelSize);

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

            lblTime.Left = pnlPlayer2.Left; // Chữ "TIME:"
            lblTimeCount.Left = lblTime.Right + 5; // Thời gian "00:00"

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
        }

        // ══════════════════════════════════════════════════════════════════
        //  Cell_Click — kiểm tra lượt trước khi gửi (Bước 3.3)
        // ══════════════════════════════════════════════════════════════════
        private void Cell_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            var (row, col) = ((int, int))btn.Tag!;

            if (_isGameOver) return;
            if (!_isMyTurn) return;

            // Chỉ cho phép đánh vào ô trống
            if (_board[row][col] != 0) return;

            SendMove(row, col);
        }

        // ══════════════════════════════════════════════════════════════════
        //  SendMove — gửi MakeMoveRequest lên server (Bước 3.4)
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
        //  HandleMoveMade — xử lý nước đi từ server (Bước 3.5)
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
        //  HandleGameOver — xử lý đầu hàng/timeout (Bước 3.6)
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
        //  ShowGameResult — hiển thị kết quả game (Bước 3.7)
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
        //  Helper methods (Bước 3.8)
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
            _board = CreateJaggedBoard();
            for (int row = 0; row < BoardSize; row++)
                for (int col = 0; col < BoardSize; col++)
                {
                    _cells[row, col].Text      = "";
                    _cells[row, col].BackColor = Color.FromArgb(245, 222, 179);
                }
        }

        // ════════════════════════════════════════════════════════════════════
        // Event handler stubs (wired in Designer.cs)
        // ════════════════════════════════════════════════════════════════════

        private void label1_Click(object sender, EventArgs e) { }

        // btnSurrender (Bước 3.9)
        private async void button1_Click(object sender, EventArgs e)
        {
            if (_isGameOver || _networkClient == null) return;

            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đầu hàng?",
                "Xác nhận đầu hàng",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var msg = new NetworkMessage(MessageType.GameOverEvent, null);
                await _networkClient.SendMessageAsync(msg);
                // Chờ server gửi lại GameOverEvent xác nhận → HandleGameOver xử lý
            }
        }

        // btnNewGame (Bước 3.10)
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

        // ══════════════════════════════════════════════════════════════════
        //  Cleanup khi đóng Form (Bước 3.11)
        // ══════════════════════════════════════════════════════════════════
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hủy đăng ký event tránh memory leak
            if (_networkClient != null)
            {
                _networkClient.OnMoveMade -= HandleMoveMade;
                _networkClient.OnGameOver -= HandleGameOver;
            }
            base.OnFormClosing(e);
        }
    }
}
