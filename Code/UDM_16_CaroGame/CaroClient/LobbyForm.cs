using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class LobbyForm : CaroForm
    {
        // Khắc phục lỗi WFO1000: Báo cho Designer bỏ qua Property này
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlayerName { get; set; } = string.Empty;

        private readonly Label _endpointStatus = new() { Name = "EndpointStatus", Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleCenter, ForeColor = CaroTheme.TextMuted, AutoEllipsis = true };
        private readonly ToolTip _sharedToolTip = new ToolTip();

        // Constructor mặc định cho Visual Studio Designer
        public LobbyForm()
        {
            InitializeComponent();
            LstPlayers.DisplayMember = "PlayerName";
            this.DoubleBuffered = true;
            Controls.Add(_endpointStatus);
            InitializeLobbySocial();

            // Wire input focus highlight
            TxtRoomCode.GotFocus += (s, e) => { pnlRoomCode.IsFocused = true; pnlRoomCode.Invalidate(); };
            TxtRoomCode.LostFocus += (s, e) => { pnlRoomCode.IsFocused = false; pnlRoomCode.Invalidate(); };
        }

        // Constructor chính nhận tên người chơi
        public LobbyForm(string playerName) : this()
        {
            PlayerName = playerName;
            _endpointStatus.Text = "Máy chủ: " + CaroClient.Network.NetworkClient.Instance.ConnectedEndpoint;
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
            CaroClient.Network.NetworkClient.Instance.OnDisconnected += LobbyDisconnected;
            CaroClient.Network.NetworkClient.Instance.OnError += LobbyError;
            Shown += async (_, _) =>
            {
                OnPlayerListReceivedHandler(CaroClient.Network.NetworkClient.Instance.PlayerList.ToList());
                await RefreshLobbyAsync();
            };

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
            if (IsDisposed || Disposing) return;
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
                string? selected = (LstPlayers.SelectedItem as CaroShared.Contracts.PlayerInfoDto)?.PlayerName;
                string myNick = PlayerName.Trim();
                LstPlayers.BeginUpdate();
                try
                {
                    LstPlayers.Items.Clear();
                    foreach (var player in players.Where(p => !p.PlayerName.Equals(myNick, StringComparison.OrdinalIgnoreCase)))
                    {
                        int index = LstPlayers.Items.Add(player);
                        if (player.PlayerName == selected) LstPlayers.SelectedIndex = index;
                    }
                    LblPlayers.Text = $"Đối thủ sẵn sàng: {LstPlayers.Items.Count}";
                }
                finally { LstPlayers.EndUpdate(); }

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
                    OpenGameBoard(response.RoomId, response.MySymbol, response.OpponentName, response.MatchIdentity, response.Timing);
                }
                else
                {
                    CaroDialogForm.Show(this, DeclinedInvitationText(response), "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            });
        }

        // Chuyển sang màn hình Bàn cờ (GameBoardForm) với thông tin phòng
        private void OpenGameBoard(string roomId = "", int mySymbol = 1, string opponentName = "", Guid matchId = default, CaroShared.Contracts.GameTimingDto? timing = null)
        {
            SafeInvoke(async () =>
            {
                using (GameBoardForm gameForm = !string.IsNullOrEmpty(roomId)
                    ? new GameBoardForm(roomId, mySymbol, opponentName, matchId, timing)
                    : new GameBoardForm())
                {
                    this.Hide();
                    gameForm.ShowDialog();
                    this.Show();
                    // The existing server protocol treats surrender after a finished
                    // match as leaving the room. Refresh presence only after that ACK flow.
                    if (!string.IsNullOrEmpty(roomId))
                    {
                        try
                        {
                            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(new(
                                CaroShared.Enums.MessageType.SurrenderRequest,
                                new CaroShared.Contracts.SurrenderRequest { RoomId = roomId }));
                            await RefreshLobbyAsync();
                        }
                        catch (Exception error) { LobbyError(error); }
                    }
                }
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

        private void LobbyDisconnected() => SafeInvoke(() =>
        {
            LstPlayers.Items.Clear();
            LstRooms.Items.Clear();
            LblPlayers.Text = "Chưa có kết nối";
            _endpointStatus.Text = "Mất kết nối máy chủ. Đóng cửa sổ và mở Caro để đăng nhập lại.";
            BtnChallenge.Enabled = false;
        });
        private void LobbyError(Exception error) => SafeInvoke(() =>
        {
            if (Visible) ToastNotification.Show(this, "Chưa thực hiện được yêu cầu. Kiểm tra kết nối rồi thử lại.", ToastType.Warning);
        });

        private void LobbyForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            CaroClient.Network.NetworkClient.Instance.OnDisconnected -= LobbyDisconnected;
            CaroClient.Network.NetworkClient.Instance.OnError -= LobbyError;
            CaroClient.Network.NetworkClient.Instance.OnPlayerListReceived -= OnPlayerListReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeReceived -= OnChallengeReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnChallengeResponseReceived -= OnChallengeResponseReceivedHandler;
            CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined -= HandleSpectatorJoined;
            CaroClient.Network.NetworkClient.Instance.OnRoomListReceived -= OnRoomListReceivedHandler;
            CaroClient.Settings.AvatarManager.Instance.OnAvatarUpdated -= OnAvatarUpdated;
            CaroClient.Network.NetworkClient.Instance.Disconnect();
            _sharedToolTip.Dispose();
        }

        private async void TsmSpectate_Click(object? sender, EventArgs e)
        {
            if (LstRooms.SelectedItem is CaroShared.Contracts.RoomDto room) await JoinRoomAsync(room.RoomId);
        }

        private void HandleSpectatorJoined(CaroShared.Contracts.JoinSpectatorResponse response)
        {
            SafeInvoke(async () =>
            {
                _joiningRoom = false;
                BtnJoinRoom.Enabled = true;
                if (response.IsSuccess && response.Snapshot != null)
                {
                    using var spectatorForm = new GameBoardForm(response.Snapshot);
                    Hide();
                    spectatorForm.ShowDialog();
                    Show();
                    try
                    {
                        await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(new(
                            CaroShared.Enums.MessageType.LeaveSpectatorRequest,
                            new CaroShared.Contracts.RoomAccessRequest { RoomId = response.Snapshot.Room!.RoomId }));
                        await RefreshLobbyAsync();
                    }
                    catch (Exception ex) { LobbyError(ex); }
                }
                else CaroDialogForm.Show(this, response.ErrorMessage ?? "Không tìm thấy phòng.", "Thông báo");
            });
        }

        private void OnRoomListReceivedHandler(System.Collections.Generic.List<CaroShared.Contracts.RoomDto> rooms)
        {
            SafeInvoke(() =>
            {
                string? selected = (LstRooms.SelectedItem as CaroShared.Contracts.RoomDto)?.RoomId;
                LstRooms.BeginUpdate();
                try
                {
                    LstRooms.Items.Clear();
                    foreach (var room in rooms)
                    {
                        int index = LstRooms.Items.Add(room);
                        if (room.RoomId == selected) LstRooms.SelectedIndex = index;
                    }
                    LblRoomList.Text = $"Phòng đang chơi ({rooms.Count}):";
                }
                finally { LstRooms.EndUpdate(); }
            });
        }

        private async void BtnJoinRoom_Click(object sender, EventArgs e)
        {
            string code = TxtRoomCode.Text.Trim();
            if (code.Length == 0 && LstRooms.SelectedItem is CaroShared.Contracts.RoomDto room) code = room.RoomId;
            await JoinRoomAsync(code);
        }
        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            ToastNotification.Show(this, "Đang làm mới danh sách...", ToastType.Info);
            await RefreshLobbyAsync();
        }

        private async Task RefreshLobbyAsync()
        {
            try
            {
            var plrMsg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.PlayerListRequest, null);
            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(plrMsg);

            var roomMsg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.RoomListRequest, null);
            await CaroClient.Network.NetworkClient.Instance.SendMessageAsync(roomMsg);
            }
            catch (Exception)
            {
                if (!IsDisposed) ToastNotification.Show(this, "Chưa tải được danh sách. Vui lòng kiểm tra kết nối rồi thử lại.", ToastType.Warning);
            }
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
            using var historyForm = new MatchHistoryForm(PlayerName);
            historyForm.ShowDialog(this);
        }
    }
}
