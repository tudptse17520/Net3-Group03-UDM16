using System.Text.Json;
using CaroClient.Network;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroClient;

public partial class LobbyForm
{
    private readonly ChatPanel _lobbyChat = new("CHAT SẢNH") { Name = "LobbyChat" };
    private bool _joiningRoom;
    protected override void OnShown(EventArgs e) { FitLobbyToScreen(); base.OnShown(e); }
    protected override void OnDpiChanged(DpiChangedEventArgs e) { base.OnDpiChanged(e); FitLobbyToScreen(); }
    private void FitLobbyToScreen()
    {
        if (_lobbyChat.Parent == null) return;
        float scale = DeviceDpi / 96f;
        int gap = (int)(12 * scale);
        _endpointStatus.Height = (int)(24 * scale);
        _lobbyChat.SetBounds(cardPlayers.Left, cardPlayers.Bottom + gap, cardActions.Right - cardPlayers.Left, (int)(150 * scale));
        ClientSize = new Size(ClientSize.Width, _lobbyChat.Bottom + gap + _endpointStatus.Height);
        LstRooms.ItemHeight = (int)(28 * scale);
        LstPlayers.ItemHeight = (int)(36 * scale);
        int excess = Height - Screen.FromControl(this).WorkingArea.Height;
        if (excess > 0 && _lobbyChat.Height - excess >= 100 * DeviceDpi / 96)
        { _lobbyChat.Height -= excess; Height -= excess; }
    }
    private void InitializeLobbySocial()
    {
        Controls.Add(_lobbyChat);
        // Retain all three existing cards; append one compact row below them.
        int gap = (int)(12 * DeviceDpi / 96f);
        int chatHeight = (int)(150 * DeviceDpi / 96f);
        _lobbyChat.SetBounds(cardPlayers.Left, cardPlayers.Bottom + gap, cardActions.Right - cardPlayers.Left, chatHeight);
        ClientSize = new Size(ClientSize.Width, _lobbyChat.Bottom + gap + _endpointStatus.Height);
        _lobbyChat.SendRequested += text => NetworkClient.Instance.SendMessageAsync(new(MessageType.LobbyChatRequest, new ChatRequest { Text = text }));
        NetworkClient.Instance.OnMessageReceived += LobbySocialMessage;
        LstRooms.DoubleClick += TsmSpectate_Click;
        LstRooms.MouseClick += async (_, e) =>
        {
            int index = LstRooms.IndexFromPoint(e.Location);
            if (index < 0 || LstRooms.Items[index] is not RoomDto room ||
                !RoomLockBounds(LstRooms.GetItemRectangle(index)).Contains(e.Location) ||
                room.OwnerName != NetworkClient.Instance.CurrentNickname) return;
            try { await NetworkClient.Instance.SendMessageAsync(new(MessageType.SetSpectatorLockRequest,
                new RoomAccessRequest { RoomId = room.RoomId, IsLocked = !room.IsSpectatorLocked })); }
            catch (Exception ex) { LobbyError(ex); }
        };
        LstRooms.MouseMove += (_, e) =>
        {
            int index = LstRooms.IndexFromPoint(e.Location);
            string hint = index >= 0 && LstRooms.Items[index] is RoomDto room && RoomLockBounds(LstRooms.GetItemRectangle(index)).Contains(e.Location)
                ? (room.IsSpectatorLocked ? "Phòng đang khóa" : "Phòng đang mở") + (room.OwnerName == NetworkClient.Instance.CurrentNickname ? " — bấm để đổi" : " — chỉ chủ phòng được đổi") : "Nhấp đúp vào phòng để xem trận";
            if (_sharedToolTip.GetToolTip(LstRooms) != hint) _sharedToolTip.SetToolTip(LstRooms, hint);
        };
        LstRooms.DrawMode = DrawMode.OwnerDrawFixed;
        LstRooms.ItemHeight = (int)(28 * DeviceDpi / 96f);
        LstRooms.DrawItem += (_, e) =>
        {
            if (e.Index < 0 || LstRooms.Items[e.Index] is not RoomDto room) return;
            e.DrawBackground();
            var bounds = e.Bounds; bounds.Inflate(-5, 0);
            var icon = RoomLockBounds(e.Bounds);
            RoomLockButton.DrawLock(e.Graphics, icon, room.IsSpectatorLocked, e.ForeColor);
            bounds.Width = Math.Max(1, icon.Left - bounds.Left - 6);
            TextRenderer.DrawText(e.Graphics, room.ToString(), LstRooms.Font, bounds, e.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        };
        LblRoomCode.Text = "Nhập mã phòng (6 số):";
        TxtRoomCode.PlaceholderText = "Ví dụ: 847883";
        FormClosed += (_, _) => NetworkClient.Instance.OnMessageReceived -= LobbySocialMessage;
    }
    private Rectangle RoomLockBounds(Rectangle row) => new(row.Right - (int)(30 * DeviceDpi / 96f), row.Top, (int)(28 * DeviceDpi / 96f), row.Height);
    private void LobbySocialMessage(NetworkMessage message)
    {
        if (message.Type == MessageType.LobbyChatEvent && message.Payload is JsonElement json)
        {
            var chat = json.Deserialize<ChatEvent>();
            if (chat != null) SafeInvoke(() => _lobbyChat.AddMessage(chat));
        }
    }
    private async Task JoinRoomAsync(string input)
    {
        if (_joiningRoom) return;
        string? roomId = RoomCodes.Normalize(input);
        if (roomId == null)
        { CaroDialogForm.Show(this, "Vui lòng nhập mã phòng gồm 6 chữ số.", "Thông báo"); return; }
        _joiningRoom = true;
        BtnJoinRoom.Enabled = false;
        try { await NetworkClient.Instance.SendMessageAsync(new(MessageType.JoinSpectatorRequest, new JoinSpectatorRequest { RoomId = roomId })); }
        catch (Exception ex) { _joiningRoom = false; BtnJoinRoom.Enabled = true; LobbyError(ex); }
    }
    public static string DeclinedInvitationText(ChallengeResponse response) => $"Đối thủ [{response.OpponentName}] đã TỪ CHỐI lời mời thách đấu.";
}
