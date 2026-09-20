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
            BtnMatchHistory.Paint += Button_Paint;

            // Đăng ký sự kiện từ NetworkClient
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived += OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived += OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived += OnChallengeResponseReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined += HandleSpectatorJoined;
            CaroClient.Network.NetworkClient.Instance.OnRoomListReceived += OnRoomListReceivedHandler;
            this.FormClosing += LobbyForm_FormClosing;
            
            // Đăng ký sự kiện DoubleClick cho danh sách người chơi để gửi lời mời thách đấu
            LstPlayers.DoubleClick += LstPlayers_DoubleClick;

            // Khởi tạo ContextMenuStrip cho danh sách phòng (Spectator)
            var cmsRoomActions = new ContextMenuStrip();
            var tsmSpectate = new ToolStripMenuItem("👁️ Xem trận");
            tsmSpectate.Click += TsmSpectate_Click;
            cmsRoomActions.Items.Add(tsmSpectate);
            LstRooms.ContextMenuStrip = cmsRoomActions;

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

            int otherCount = 0;
            foreach (var name in playerNames)
            {
                if (string.Equals(name.Trim(), myNick, StringComparison.OrdinalIgnoreCase))
                {
                    LstPlayers.Items.Add($"🟢 {name} (Bạn)");
                }
                else
                {
                    LstPlayers.Items.Add($"👤 {name}");
                    otherCount++;
                }
            }

            LblPlayers.Text = $"Người chơi online ({playerNames.Count}) — Thách đấu: {otherCount}";
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
                // Khi chấp nhận, Server sẽ gửi ChallengeResponse kèm RoomId về cho cả 2 người chơi
                // Quá trình mở GameBoard sẽ được kích hoạt trong OnChallengeResponseReceivedHandler
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
                OpenGameBoard(response.RoomId, response.MySymbol, response.OpponentName);
            }
            else
            {
                MessageBox.Show($"Đối thủ [{response.ChallengerId}] đã TỪ CHỐI lời mời thách đấu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Chuyển sang màn hình Bàn cờ (GameBoardForm) với thông tin phòng
        private void OpenGameBoard(string roomId = "", int mySymbol = 1, string opponentName = "")
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OpenGameBoard(roomId, mySymbol, opponentName)));
                return;
            }

            GameBoardForm gameForm;
            if (!string.IsNullOrEmpty(roomId))
            {
                gameForm = new GameBoardForm(roomId, mySymbol, opponentName);
            }
            else
            {
                gameForm = new GameBoardForm();
            }
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

        // Nháy đúp vào danh sách người chơi để thách đấu nhanh
        private void LstPlayers_DoubleClick(object? sender, EventArgs e)
        {
            if (LstPlayers.SelectedItem == null) return;
            // Thực hiện logic tương tự BtnChallenge_Click
            BtnChallenge_Click(sender!, e);
        }

        // Removed duplicate TsmSpectate_Click (kept the one from develop that actually sends request)

        private void LobbyForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived -= OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived -= OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived -= OnChallengeResponseReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined -= HandleSpectatorJoined;
            CaroClient.Network.NetworkClient.Instance.OnRoomListReceived -= OnRoomListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.Disconnect();
        }

        // Removed HandleChallengeRequest and HandleChallengeResponse (kept client-challenge versions)

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


        // Xử lý khi nhận danh sách phòng đang chơi từ Server
        private void OnRoomListReceivedHandler(System.Collections.Generic.List<CaroShared.Contracts.RoomDto> rooms)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnRoomListReceivedHandler(rooms)));
                return;
            }

            LstRooms.Items.Clear();
            if (rooms.Count == 0)
            {
                LstRooms.Items.Add("(Chưa có phòng nào đang chơi)");
            }
            else
            {
                foreach (var room in rooms)
                {
                    LstRooms.Items.Add($"{room.RoomId} | {room.PlayerX} vs {room.PlayerO} | 👁️ {room.SpectatorCount}");
                }
            }
            LblRoomList.Text = $"Phòng đang chơi ({rooms.Count}):";
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
                string selected = LstRooms.SelectedItem.ToString() ?? string.Empty;
                // Parse RoomId từ format "ROOM-XXXXXX | PlayerX vs PlayerO | ..."
                if (selected.Contains(" | "))
                    roomCode = selected.Split(" | ")[0].Trim();
                else
                    roomCode = selected;
            }

            if (string.IsNullOrEmpty(roomCode) || roomCode.StartsWith("("))
            {
                MessageBox.Show("Vui lòng chọn phòng đang chơi hoặc nhập mã phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Gửi JoinSpectatorRequest để vào xem phòng đang chơi
            var request = new CaroShared.Contracts.JoinSpectatorRequest { RoomId = roomCode };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.JoinSpectatorRequest, request);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        private void BtnCreateRoom_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Để bắt đầu trận đấu, hãy chọn người chơi trong danh sách và nhấn THÁCH ĐẤU.\n" +
                "Phòng sẽ được tạo tự động khi đối thủ chấp nhận.",
                "Hướng dẫn", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            // Gửi request lấy danh sách người chơi online
            var plrMsg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.PlayerListRequest, null);
            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(plrMsg);

            // Gửi request lấy danh sách phòng đang chơi
            var roomMsg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.RoomListRequest, null);
            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(roomMsg);
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