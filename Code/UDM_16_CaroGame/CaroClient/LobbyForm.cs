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
            BtnMatchHistory.Paint += Button_Paint;

            // Đăng ký sự kiện nhận danh sách người chơi từ Server
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived += OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponse += HandleChallengeResponse;
            CaroClient.Network.NetworkClient.Instance.OnChallengeRequest += HandleChallengeRequest;
            CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined += HandleSpectatorJoined;
            this.FormClosing += LobbyForm_FormClosing;
            
            // Đăng ký sự kiện DoubleClick cho danh sách người chơi để gửi lời mời thách đấu
            LstPlayers.DoubleClick += LstPlayers_DoubleClick;

            // Khởi tạo ContextMenuStrip cho danh sách phòng (Spectator)
            var cmsRoomActions = new ContextMenuStrip();
            var tsmSpectate = new ToolStripMenuItem("👁️ Xem trận");
            tsmSpectate.Click += TsmSpectate_Click;
            cmsRoomActions.Items.Add(tsmSpectate);
            LstRooms.ContextMenuStrip = cmsRoomActions;

            // TODO: Sẽ request danh sách khi Server hỗ trợ PlayerListRequest
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

        private void LobbyForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived -= OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponse -= HandleChallengeResponse;
            CaroClient.Network.NetworkClient.Instance.OnChallengeRequest -= HandleChallengeRequest;
            CaroClient.Network.NetworkClient.Instance.Disconnect();
        }

        private void HandleChallengeRequest(CaroShared.Contracts.ChallengeRequest request)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleChallengeRequest(request)));
                return;
            }

            var dialogResult = MessageBox.Show(
                $"Người chơi {request.TargetPlayerId} muốn thách đấu với bạn. Bạn có đồng ý không?", 
                "Lời mời thách đấu", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Question);

            var response = new CaroShared.Contracts.ChallengeResponse
            {
                ChallengerId = request.TargetPlayerId,
                IsAccepted = (dialogResult == DialogResult.Yes)
            };
            
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.ChallengeResponse, response);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        private void HandleChallengeResponse(CaroShared.Contracts.ChallengeResponse response)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleChallengeResponse(response)));
                return;
            }

            if (response.IsAccepted)
            {
                // Mở GameBoardForm cho cả hai bên khi có RoomId
                if (!string.IsNullOrEmpty(response.RoomId))
                {
                    var gameForm = new GameBoardForm(response.RoomId, response.MySymbol, response.OpponentName);
                    this.Hide();
                    gameForm.ShowDialog();
                    this.Show();
                }
            }
            else
            {
                MessageBox.Show($"Người chơi {response.ChallengerId} đã từ chối lời mời.", "Thách đấu", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void TsmSpectate_Click(object? sender, EventArgs e)
        {
            if (LstRooms.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn một phòng để vào xem!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // TODO: Lấy RoomId thực tế, tạm thời dummy string nếu chưa có
            string roomId = LstRooms.SelectedItem.ToString() ?? string.Empty;
            // Parse roomId (vd: "Phòng 102 (Đang chơi)" -> giả định lấy ID thực tế từ object)
            // Hiện tại dùng tạm mã phòng
            if (roomId.Contains("Phòng"))
                roomId = roomId.Split(' ')[1]; // Tạm thời

            var request = new CaroShared.Contracts.JoinSpectatorRequest { RoomId = roomId };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.JoinSpectatorRequest, request);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        private void HandleSpectatorJoined(CaroShared.Contracts.JoinSpectatorResponse response)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HandleSpectatorJoined(response)));
                return;
            }

            if (response.IsSuccess && response.Snapshot != null)
            {
                var spectatorForm = new GameBoardForm(response.Snapshot);
                this.Hide();
                spectatorForm.ShowDialog();
                this.Show();
            }
            else
            {
                MessageBox.Show($"Không thể vào xem: {response.ErrorMessage}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LstPlayers_DoubleClick(object? sender, EventArgs e)
        {
            if (LstPlayers.SelectedItem == null) return;
            string selectedText = LstPlayers.SelectedItem.ToString() ?? "";
            
            // Xóa các ký tự biểu tượng "👤 " hoặc "(Bạn)"
            string targetPlayer = selectedText.Replace("👤", "").Replace("(Bạn)", "").Trim();

            string myNick = CaroClient.Network.NetworkClient.Instance.CurrentNickname.Trim();
            if (string.Equals(targetPlayer, myNick, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Bạn không thể tự thách đấu với chính mình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var request = new CaroShared.Contracts.ChallengeRequest { TargetPlayerId = targetPlayer };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.ChallengeRequest, request);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
            MessageBox.Show($"Đã gửi lời mời thách đấu đến {targetPlayer}. Đang chờ phản hồi...", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void BtnMatchHistory_Click(object sender, EventArgs e)
        {
            var historyForm = new MatchHistoryForm(PlayerName);
            historyForm.ShowDialog();
        }
    }
}