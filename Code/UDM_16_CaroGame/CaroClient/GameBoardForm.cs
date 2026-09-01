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

        // ─────────────────────────────────────────────────────────────────
        public GameBoardForm()
        {
            InitializeComponent();
            InitBoard();
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

        // ── Xử lý khi người chơi click ô cờ ──────────────────────────────
        private void Cell_Click(object? sender, EventArgs e)
        {
            if (sender is not Button btn) return;
            var (row, col) = ((int, int))btn.Tag!;

            // Chỉ cho phép đánh vào ô trống
            if (_board[row][col] != 0) return;

            // TODO [NetworkDev]: Gửi tọa độ (row, col) lên server qua NetworkClient
            //   Gợi ý: NetworkClient.SendMove(row, col);
            //          Sau đó chờ server phản hồi MoveMadeEvent rồi gọi UpdateBoard()
            SendMove(row, col);
        }

        /// <summary>
        /// Gửi nước đi lên server.
        /// </summary>
        /// <param name="row">Hàng (0–14)</param>
        /// <param name="col">Cột (0–14)</param>
        /// <remarks>
        /// TODO [NetworkDev]: Triển khai kết nối socket/HTTP tại đây.
        /// Method này được gọi mỗi khi người chơi click ô cờ hợp lệ.
        /// </remarks>
        private void SendMove(int row, int col)
        {
            throw new NotImplementedException(
                $"[NetworkDev] Chưa triển khai gửi nước đi: row={row}, col={col}. " +
                "Hãy kết nối NetworkClient và gửi MakeMoveRequest tới server.");
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

        // btnSurrender
        // TODO [NetworkDev]: Gửi SurrenderRequest lên server, sau đó chờ GameOverEvent.
        private void button1_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException(
                "[NetworkDev] Chưa triển khai đầu hàng. " +
                "Hãy gửi SurrenderRequest tới server và xử lý GameOverEvent.");
        }

        // btnNewGame
        // TODO [NetworkDev]: Gửi NewGameRequest lên server.
        //                    Server xác nhận → gọi ResetBoard() ở phía client.
        private void button3_Click(object sender, EventArgs e)
        {
            throw new NotImplementedException(
                "[NetworkDev] Chưa triển khai yêu cầu ván mới. " +
                "Hãy gửi NewGameRequest và gọi ResetBoard() sau khi server xác nhận.");
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
