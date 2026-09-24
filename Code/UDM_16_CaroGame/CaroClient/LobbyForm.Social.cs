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
        LstRooms.DrawMode = DrawMode.OwnerDrawFixed;
        LstRooms.ItemHeight = (int)(28 * DeviceDpi / 96f);
        LstRooms.DrawItem += (_, e) =>
        {
            if (e.Index < 0 || LstRooms.Items[e.Index] is not RoomDto room) return;
            e.DrawBackground();
            var bounds = e.Bounds; bounds.Inflate(-5, 0);
            if (room.IsSpectatorLocked)
            {
                TextRenderer.DrawText(e.Graphics, "🔒", LstRooms.Font, new Rectangle(bounds.Right - 26, bounds.Top, 26, bounds.Height), e.ForeColor, TextFormatFlags.VerticalCenter);
                bounds.Width -= 28;
            }
            TextRenderer.DrawText(e.Graphics, room.ToString(), LstRooms.Font, bounds, e.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        };
        LblRoomCode.Text = "Nhập mã phòng (6 số):";
        TxtRoomCode.PlaceholderText = "Ví dụ: 847883";
        FormClosed += (_, _) => NetworkClient.Instance.OnMessageReceived -= LobbySocialMessage;
    }
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
