using System;
using System.ComponentModel;
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

        private readonly ToolTip _sharedToolTip = new ToolTip();

        // Constructor mặc định cho Visual Studio Designer
        public LobbyForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;

            // Wire input focus highlight
            TxtRoomCode.GotFocus += (s, e) => { pnlRoomCode.IsFocused = true; pnlRoomCode.Invalidate(); };
            TxtRoomCode.LostFocus += (s, e) => { pnlRoomCode.IsFocused = false; pnlRoomCode.Invalidate(); };
        }

        // Constructor chính nhận tên người chơi
        public LobbyForm(string playerName) : this()
        {
            PlayerName = playerName;
            LblWelcome.Text = $"Xin chào, {PlayerName}!";
            _sharedToolTip.SetToolTip(LblWelcome, $"Xin chào, {PlayerName}!");

            // Đăng ký sự kiện từ NetworkClient
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived += OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived += OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived += OnChallengeResponseReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined += HandleSpectatorJoined;
            CaroClient.Network.NetworkClient.Instance.OnRoomListReceived += OnRoomListReceivedHandler;
            CaroClient.Settings.AvatarManager.Instance.OnAvatarUpdated += OnAvatarUpdated;
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

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var bgBrush = new SolidBrush(CaroTheme.Background);
            e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
        }

        private void SafeInvoke(Action action)
        {
            if (!this.IsHandleCreated)
            {
                this.HandleCreated += (s, e) => SafeInvoke(action);
                return;
            }

            if (this.InvokeRequired)
                this.BeginInvoke(action);
            else
                action();
        }

        private void OnPlayerListReceivedHandler(System.Collections.Generic.List<CaroShared.Contracts.PlayerInfoDto> players)
        {
            SafeInvoke(() =>
            {
                LstPlayers.Items.Clear();
                string myNick = !string.IsNullOrWhiteSpace(PlayerName) 
                    ? PlayerName.Trim() 
                    : CaroClient.Network.NetworkClient.Instance.CurrentNickname.Trim();

                int otherCount = 0;
                foreach (var p in players)
                {
                    LstPlayers.Items.Add(p);
                    if (!string.Equals(p.PlayerName.Trim(), myNick, StringComparison.OrdinalIgnoreCase))
                    {
                        otherCount++;
                    }
                }

                LblPlayers.Text = $"Online ({players.Count}) | Sẵn sàng: {otherCount}";
            });
        }

        // Xử lý khi nhận được lời mời thách đấu từ người chơi khác
        private async void OnChallengeReceivedHandler(CaroShared.Contracts.ChallengeRequest request)
        {
            SafeInvoke(async () =>
            {
                DialogResult result = CaroDialogForm.Show(
                    this,
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
            });
        }

        // Xử lý khi nhận phản hồi thách đấu từ đối thủ
        private void OnChallengeResponseReceivedHandler(CaroShared.Contracts.ChallengeResponse response)
        {
            SafeInvoke(() =>
            {
                if (response.IsAccepted)
                {
                    CaroDialogForm.Show(this, $"Đối thủ [{response.ChallengerId}] đã CHẤP NHẬN lời mời!\nĐang vào bàn cờ...", "Thách đấu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    OpenGameBoard(response.RoomId, response.MySymbol, response.OpponentName, response.MatchIdentity);
                }
                else
                {
                    CaroDialogForm.Show(this, $"Đối thủ [{response.ChallengerId}] đã TỪ CHỐI lời mời thách đấu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            });
        }

        // Chuyển sang màn hình Bàn cờ (GameBoardForm) với thông tin phòng
        private void OpenGameBoard(string roomId = "", int mySymbol = 1, string opponentName = "", Guid matchId = default)
        {
            SafeInvoke(() =>
            {
                GameBoardForm gameForm;
                if (!string.IsNullOrEmpty(roomId))
                {
                    gameForm = new GameBoardForm(roomId, mySymbol, opponentName, matchId);
                }
                else
                {
                    gameForm = new GameBoardForm();
                }
                this.Hide();
                gameForm.ShowDialog();
                this.Show();
            });
        }

        private void OnAvatarUpdated(string playerId, Image? avatar)
        {
            SafeInvoke(() =>
            {
                // Force redraw ListBox
                LstPlayers.Invalidate();
            });
        }

        private void LstPlayers_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= LstPlayers.Items.Count) return;

            var p = LstPlayers.Items[e.Index] as CaroShared.Contracts.PlayerInfoDto;
            if (p == null) return;

            string myNick = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
            bool isMe = string.Equals(p.PlayerName.Trim(), myNick.Trim(), StringComparison.OrdinalIgnoreCase);

            e.DrawBackground();

            // Background for selected vs normal
            Brush textBrush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                ? Brushes.White : new SolidBrush(CaroTheme.TextDark);

            // Fetch avatar from AvatarManager
            Image? avatar = CaroClient.Settings.AvatarManager.Instance.GetAvatar(p.PlayerName, p.AvatarVersion, p.HasAvatar);
            
            // Draw Avatar (28x28)
            Rectangle avatarRect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 4, 28, 28);
            if (avatar != null)
            {
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddEllipse(avatarRect);
                    e.Graphics.SetClip(path);
                    e.Graphics.DrawImage(avatar, avatarRect);
                    e.Graphics.ResetClip();
                }
                avatar.Dispose(); // GetAvatar returns a clone, we must dispose it
            }
            else
            {
                // Default placeholder
                e.Graphics.FillEllipse(Brushes.LightGray, avatarRect);
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(p.PlayerName.Substring(0, 1).ToUpper(), new Font("Segoe UI", 12, FontStyle.Bold), Brushes.White, avatarRect, sf);
            }

            // Draw status dot
            Rectangle dotRect = new Rectangle(avatarRect.Right - 8, avatarRect.Bottom - 8, 10, 10);
            e.Graphics.FillEllipse(isMe ? Brushes.LimeGreen : Brushes.Gray, dotRect);
            e.Graphics.DrawEllipse(Pens.White, dotRect);

            // Draw Name
            Rectangle textRect = new Rectangle(avatarRect.Right + 10, e.Bounds.Y, e.Bounds.Width - avatarRect.Width - 14, e.Bounds.Height);
            StringFormat textSf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            
            string display = isMe ? $"{p.PlayerName} (Bạn)" : p.PlayerName;
            e.Graphics.DrawString(display, e.Font ?? new Font("Segoe UI", 9.5f), textBrush, textRect, textSf);

            if ((e.State & DrawItemState.Selected) != DrawItemState.Selected)
            {
                textBrush.Dispose();
            }

            e.DrawFocusRectangle();
        }

        private async void BtnChallenge_Click(object sender, EventArgs e)
        {
            if (LstPlayers.SelectedItem == null)
            {
                CaroDialogForm.Show(this, "Vui lòng chọn một người chơi trong danh sách để thách đấu!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetNick = "";
            if (LstPlayers.SelectedItem is CaroShared.Contracts.PlayerInfoDto p)
            {
                targetNick = p.PlayerName.Trim();
            }
            else
            {
                string selectedItem = LstPlayers.SelectedItem.ToString() ?? string.Empty;
                targetNick = selectedItem.Replace("🟢 ", "").Replace(" (Bạn)", "").Replace("👤 ", "").Trim();
            }

            string myNick = !string.IsNullOrWhiteSpace(PlayerName) 
                ? PlayerName.Trim() 
                : CaroClient.Network.NetworkClient.Instance.CurrentNickname.Trim();

            if (string.Equals(targetNick, myNick, StringComparison.OrdinalIgnoreCase))
            {
                CaroDialogForm.Show(this, "Bạn không thể tự thách đấu chính mình!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            await CaroClient.Network.NetworkClient.Instance.SendChallengeRequestAsync(targetNick);
            ToastNotification.Show(this, $"Đã gửi lời mời thách đấu tới [{targetNick}]. Vui lòng chờ phản hồi...", ToastType.Info);
        }

        // Nháy đúp vào danh sách người chơi để thách đấu nhanh
        private void LstPlayers_DoubleClick(object? sender, EventArgs e)
        {
            if (LstPlayers.SelectedItem == null) return;
            BtnChallenge_Click(sender!, e);
        }

        private void LobbyForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived -= OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived -= OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived -= OnChallengeResponseReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined -= HandleSpectatorJoined;
            CaroClient.Network.NetworkClient.Instance.OnRoomListReceived -= OnRoomListReceivedHandler;
            CaroClient.Settings.AvatarManager.Instance.OnAvatarUpdated -= OnAvatarUpdated;
            CaroClient.Network.NetworkClient.Instance.Disconnect();
            _sharedToolTip.Dispose();
        }

        private void TsmSpectate_Click(object? sender, EventArgs e)
        {
            if (LstRooms.SelectedItem == null)
            {
                CaroDialogForm.Show(this, "Vui lòng chọn một phòng để vào xem!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string roomId = LstRooms.SelectedItem.ToString() ?? string.Empty;
            if (roomId.Contains(" | "))
                roomId = roomId.Split(" | ")[0].Trim();
            else if (roomId.Contains("Phòng"))
                roomId = roomId.Split(' ')[1];

            var request = new CaroShared.Contracts.JoinSpectatorRequest { RoomId = roomId };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.JoinSpectatorRequest, request);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        private void HandleSpectatorJoined(CaroShared.Contracts.JoinSpectatorResponse response)
        {
            SafeInvoke(() =>
            {
                if (response.IsSuccess && response.Snapshot != null)
                {
                    var spectatorForm = new GameBoardForm(response.Snapshot);
                    this.Hide();
                    spectatorForm.ShowDialog();
                    this.Show();
                }
                else
                {
                    CaroDialogForm.Show(this, $"Không thể vào xem: {response.ErrorMessage}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });
        }

        // Xử lý khi nhận danh sách phòng đang chơi từ Server
        private void OnRoomListReceivedHandler(System.Collections.Generic.List<CaroShared.Contracts.RoomDto> rooms)
        {
            SafeInvoke(() =>
            {
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
            });
        }

        private void BtnJoinRoom_Click(object sender, EventArgs e)
        {
            string roomCode = TxtRoomCode.Text.Trim();

            if (LstRooms.SelectedItem != null && string.IsNullOrEmpty(roomCode))
            {
                string selected = LstRooms.SelectedItem.ToString() ?? string.Empty;
                if (selected.Contains(" | "))
                    roomCode = selected.Split(" | ")[0].Trim();
                else
                    roomCode = selected;
            }

            if (string.IsNullOrEmpty(roomCode) || roomCode.StartsWith("("))
            {
                CaroDialogForm.Show(this, "Vui lòng chọn phòng đang chơi hoặc nhập mã phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var request = new CaroShared.Contracts.JoinSpectatorRequest { RoomId = roomCode };
            var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.JoinSpectatorRequest, request);
            _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            ToastNotification.Show(this, "Đang làm mới danh sách...", ToastType.Info);

            var plrMsg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.PlayerListRequest, null);
            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(plrMsg);

            var roomMsg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.RoomListRequest, null);
            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(roomMsg);
        }

        private void BtnPersonalization_Click(object sender, EventArgs e)
        {
            using var form = new PersonalizationForm();
            form.ShowDialog(this);
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = CaroDialogForm.Show(this, "Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
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