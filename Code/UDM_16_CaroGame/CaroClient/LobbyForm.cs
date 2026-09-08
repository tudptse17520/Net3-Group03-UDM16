using System;
using System.ComponentModel; // Cần thiết cho DesignerSerializationVisibility
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class LobbyForm : Form
    {
        // Khắc phục lỗi WFO1000: Báo cho Designer bỏ qua Property này
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlayerName { get; set; } = string.Empty;

        // Constructor mặc định cho Visual Studio Designer
        public LobbyForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        // Constructor chính nhận tên người chơi
        public LobbyForm(string playerName) : this()
        {
            PlayerName = playerName;
            LblWelcome.Text = $"Xin chào, {PlayerName}!";

            // Đăng ký sự kiện vẽ bo góc
            BtnJoinRoom.Paint += Button_Paint;
            BtnCreateRoom.Paint += Button_Paint;
            BtnRefresh.Paint += Button_Paint;
            BtnLogout.Paint += Button_Paint;
            BtnChallenge.Paint += Button_Paint;

            // Đăng ký sự kiện từ NetworkClient
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived += OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived += OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived += OnChallengeResponseReceivedHandler;
            this.FormClosing += LobbyForm_FormClosing;
        }

        private void OnPlayerListReceivedHandler(System.Collections.Generic.List<string> playerNames)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnPlayerListReceivedHandler(playerNames)));
                return;
            }

            LstPlayers.Items.Clear();
            string myNick = !string.IsNullOrWhiteSpace(PlayerName) 
                ? PlayerName.Trim() 
                : CaroClient.Network.NetworkClient.Instance.CurrentNickname.Trim();

            foreach (var name in playerNames)
            {
                if (string.Equals(name.Trim(), myNick, StringComparison.OrdinalIgnoreCase))
                {
                    LstPlayers.Items.Add($"🟢 {name} (Bạn)");
                }
                else
                {
                    LstPlayers.Items.Add($"👤 {name}");
                }
            }

            LblPlayers.Text = $"Người chơi online ({playerNames.Count}):";
        }

        // Xử lý khi nhận được lời mời thách đấu từ người chơi khác
        private async void OnChallengeReceivedHandler(CaroShared.Contracts.ChallengeRequest request)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnChallengeReceivedHandler(request)));
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Người chơi [{request.TargetPlayerId}] muốn THÁCH ĐẤU với bạn!\n\nBạn có chấp nhận không?",
                "Lời Mời Thách Đấu",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            bool isAccepted = (result == DialogResult.Yes);
            await CaroClient.Network.NetworkClient.Instance.SendChallengeResponseAsync(request.TargetPlayerId, isAccepted);

            if (isAccepted)
            {
                OpenGameBoard();
            }
        }

        // Xử lý khi nhận phản hồi thách đấu từ đối thủ
        private void OnChallengeResponseReceivedHandler(CaroShared.Contracts.ChallengeResponse response)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnChallengeResponseReceivedHandler(response)));
                return;
            }

            if (response.IsAccepted)
            {
                MessageBox.Show($"Đối thủ [{response.ChallengerId}] đã CHẤP NHẬN lời mời!\nĐang vào bàn cờ...", "Thách đấu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                OpenGameBoard();
            }
            else
            {
                MessageBox.Show($"Đối thủ [{response.ChallengerId}] đã TỪ CHỐI lời mời thách đấu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Chuyển sang màn hình Bàn cờ (GameBoardForm)
        private void OpenGameBoard()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(OpenGameBoard));
                return;
            }

            GameBoardForm gameForm = new GameBoardForm();
            this.Hide();
            gameForm.ShowDialog();
            this.Show();
        }

        private async void BtnChallenge_Click(object sender, EventArgs e)
        {
            if (LstPlayers.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một người chơi trong danh sách để thách đấu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedItem = LstPlayers.SelectedItem.ToString() ?? string.Empty;
            string targetNick = selectedItem.Replace("🟢 ", "").Replace(" (Bạn)", "").Replace("👤 ", "").Trim();
            string myNick = !string.IsNullOrWhiteSpace(PlayerName) 
                ? PlayerName.Trim() 
                : CaroClient.Network.NetworkClient.Instance.CurrentNickname.Trim();

            if (string.Equals(targetNick, myNick, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Bạn không thể tự thách đấu chính mình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await CaroClient.Network.NetworkClient.Instance.SendChallengeRequestAsync(targetNick);
            MessageBox.Show($"Đã gửi lời mời thách đấu tới [{targetNick}]. Vui lòng chờ phản hồi...", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LobbyForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived -= OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived -= OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived -= OnChallengeResponseReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.Disconnect();
        }

        // Khắc phục cảnh báo CS8622: Thêm dấu ? cho object? sender
        private void Button_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            int borderRadius = 8;
            Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);

            using (GraphicsPath path = GetRoundedPath(rect, borderRadius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Tô màu nền
                using (SolidBrush brush = new SolidBrush(btn.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Vẽ viền vàng đồng cổ điển
                using (Pen pen = new Pen(ColorTranslator.FromHtml("#E8C37B"), 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                // Vẽ chữ giữa nút
                TextRenderer.DrawText(
                    e.Graphics,
                    btn.Text,
                    btn.Font,
                    rect,
                    btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );

                btn.Region = new Region(path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void BtnJoinRoom_Click(object sender, EventArgs e)
        {
            string roomCode = TxtRoomCode.Text.Trim();

            if (LstRooms.SelectedItem != null && string.IsNullOrEmpty(roomCode))
            {
                roomCode = LstRooms.SelectedItem.ToString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(roomCode))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập mã phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Đang tham gia phòng: {roomCode}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCreateRoom_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Đang tạo phòng mới...", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            // TODO: Sẽ request danh sách khi Server hỗ trợ PlayerListRequest

            // Cập nhật danh sách phòng mẫu
            LstRooms.Items.Clear();
            LstRooms.Items.Add("Phòng 101 (1/2)");
            LstRooms.Items.Add("Phòng 102 (Đang chơi)");
            LstRooms.Items.Add("Phòng 103 (1/2)");
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                CaroClient.Network.NetworkClient.Instance.Disconnect();
                this.Close();
            }
        }
    }
}