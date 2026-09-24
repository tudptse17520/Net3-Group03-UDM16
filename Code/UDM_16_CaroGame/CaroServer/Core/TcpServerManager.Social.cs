using System.Text.Json;
using CaroServer.Models;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroServer.Core;

public partial class TcpServerManager
{
    private readonly SemaphoreSlim _challengeActions = new(1, 1);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string Recipient, string Sender), DateTime> _pendingChallenges = new();
    private async Task HandleClientDisconnectedAsync(PlayerSession session)
    {
        var room = _roomManager.GetRoom(session.CurrentRoomId ?? "");
        if (room != null) await room.Actions.WaitAsync();
        try { await HandleClientDisconnectedCoreAsync(session); }
        finally { room?.Actions.Release(); }
    }
    private static T? Payload<T>(NetworkMessage m) => m.Payload is JsonElement json ? json.Deserialize<T>(JsonOptions) : default;
    private Task Reject(PlayerSession s, NetworkMessage m, string text) => s.SendMessageAsync(new(
        MessageType.ErrorResponse, new ErrorResponse { Code = ErrorCode.InvalidRequest, Message = text }, m.RequestId));

    private async Task HandleIncomingMessage(PlayerSession sender, NetworkMessage message)
    {
        if (!sender.IsAuthenticated && message.Type is not (MessageType.LoginRequest or MessageType.ReconnectRequest or MessageType.Pong))
        { await Reject(sender, message, "Vui lòng đăng nhập trước."); return; }
        string? requestedRoom = message.Payload is JsonElement p && p.ValueKind == JsonValueKind.Object && p.TryGetProperty("RoomId", out var id)
            ? id.GetString() : null;
        var room = _roomManager.GetRoom(RoomCodes.Normalize(requestedRoom) ?? requestedRoom ?? sender.CurrentRoomId ?? "");
        bool playerAction = message.Type is MessageType.MakeMoveRequest or MessageType.SurrenderRequest
            or MessageType.DrawOfferRequest or MessageType.DrawResponseRequest or MessageType.NewGameRequest
            or MessageType.NewGameResponseRequest or MessageType.SetSpectatorLockRequest or MessageType.RoomChatRequest;
        bool roomAction = playerAction || message.Type is MessageType.JoinSpectatorRequest or MessageType.LeaveSpectatorRequest or MessageType.RoomPresenceRequest;
        bool challengeAction = message.Type is MessageType.ChallengeRequest or MessageType.ChallengeResponse;
        var gate = challengeAction ? _challengeActions : roomAction ? room?.Actions : null;
        if (gate != null) await gate.WaitAsync();
        try
        {
            if (playerAction && (room == null || !room.IsPlayer(sender.PlayerId) || sender.CurrentRoomId != room.RoomId))
            { await Reject(sender, message, "Bạn không có quyền thực hiện thao tác trong phòng này."); return; }
            switch (message.Type)
            {
                case MessageType.JoinSpectatorRequest: await JoinSpectatorAsync(sender, message, room); break;
                case MessageType.LeaveSpectatorRequest:
                    if (room == null || sender.CurrentRoomId != room.RoomId || !room.RemoveSpectator(sender.PlayerId))
                    { await Reject(sender, message, "Bạn không ở trong phòng khán giả này."); break; }
                    sender.CurrentRoomId = null;
                    _lobbyManager.AddPlayer(sender.PlayerId, sender.PlayerId);
                    await PublishPresenceAsync(room, left: sender.PlayerId);
                    RemoveEmptyFinishedRoom(room);
                    await BroadcastPlayerListAsync();
                    break;
                case MessageType.RoomPresenceRequest:
                    if (room != null && sender.CurrentRoomId == room.RoomId)
                        await sender.SendMessageAsync(new(MessageType.RoomPresenceEvent, new RoomPresenceEvent { Room = DescribeRoom(room) }));
                    else await Reject(sender, message, "Bạn không ở trong phòng này.");
                    break;
                case MessageType.SetSpectatorLockRequest:
                    room!.IsSpectatorLocked = Payload<RoomAccessRequest>(message)!.IsLocked;
                    await PublishPresenceAsync(room);
                    break;
                case MessageType.LobbyChatRequest: await ChatAsync(sender, message, null); break;
                case MessageType.RoomChatRequest: await ChatAsync(sender, message, room); break;
                case MessageType.NewGameResponseRequest: await ResolveNewGameAsync(sender, message, room!); break;
                default: await DispatchMessageAsync(sender, message); break;
            }
        }
        finally { gate?.Release(); }
    }

    private RoomDto DescribeRoom(Room room) => new()
    {
        RoomId = room.RoomId, PlayerX = CreatePlayerInfo(room.PlayerXId), PlayerO = CreatePlayerInfo(room.PlayerOId),
        IsSpectatorLocked = room.IsSpectatorLocked, Revision = room.PresenceRevision,
        SpectatorCount = room.SpectatorCount,
        Spectators = room.GetSpectators().Order().Select(CreatePlayerInfo).ToList()
    };
    private void RemoveEmptyFinishedRoom(Room? room)
    {
        if (room != null && room.Session.Engine.Status != "Playing" && room.SpectatorCount == 0 &&
            _sessionManager.GetSession(room.PlayerXId)?.CurrentRoomId != room.RoomId &&
            _sessionManager.GetSession(room.PlayerOId)?.CurrentRoomId != room.RoomId)
            _roomManager.RemoveRoom(room.RoomId);
    }
    private async Task PublishPresenceAsync(Room room, string? joined = null, string? left = null)
    {
        room.PresenceRevision++;
        await _broadcaster.BroadcastToRoomAsync(room.RoomId, new(MessageType.RoomPresenceEvent,
            new RoomPresenceEvent { Room = DescribeRoom(room), JoinedPlayerName = joined, LeftPlayerName = left }));
        await PublishRoomsAsync();
    }
    private RoomListResponse RoomList() => new()
    {
        Rooms = _roomManager.GetActiveRooms().Where(r => r.Session.Engine.Status == "Playing").Select(DescribeRoom).ToList()
    };
    private async Task PublishRoomsAsync()
    {
        var message = new NetworkMessage(MessageType.RoomListResponse, RoomList());
        foreach (var s in _sessionManager.GetAllSessions().Where(s => s.IsAuthenticated && s.CurrentRoomId == null))
            await SafeSendAsync(s, message);
    }
    private static async Task SafeSendAsync(PlayerSession session, NetworkMessage message)
    {
        try { await session.SendMessageAsync(message); }
        catch (Exception) { session.Dispose(); }
    }
    private async Task JoinSpectatorAsync(PlayerSession sender, NetworkMessage message, Room? room)
    {
        string? error = room == null ? "Không tìm thấy phòng."
            : room.Session.Engine.Status != "Playing" ? "Trận đấu đã kết thúc."
            : room.IsSpectatorLocked ? "Phòng này đang khóa, khán giả không thể vào xem."
            : sender.CurrentRoomId != null ? "Hãy thoát phòng hiện tại trước." : null;
        if (error != null || !room!.AddSpectator(sender.PlayerId))
        {
            await sender.SendMessageAsync(new(MessageType.JoinSpectatorResponse,
                new JoinSpectatorResponse { ErrorMessage = error ?? "Không thể vào xem phòng này." }, message.RequestId));
            return;
        }
        sender.CurrentRoomId = room.RoomId;
        _lobbyManager.RemovePlayer(sender.PlayerId);
        await sender.SendMessageAsync(new(MessageType.JoinSpectatorResponse, new JoinSpectatorResponse
        {
            IsSuccess = true, Snapshot = new() { Room = DescribeRoom(room), Session = room.Session.ToDto() }
        }, message.RequestId));
        await PublishPresenceAsync(room, joined: sender.PlayerId);
        await BroadcastPlayerListAsync();
    }
    private async Task ChatAsync(PlayerSession sender, NetworkMessage message, Room? room)
    {
        if (room == null && sender.CurrentRoomId != null)
        { await Reject(sender, message, "Chat sảnh chỉ dành cho người đang ở sảnh."); return; }
        string text = Payload<ChatRequest>(message)?.Text?.Trim() ?? "";
        if (text.Length == 0) return;
        if (text.Length > 500 || text.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'))
        { await Reject(sender, message, "Tin nhắn tối đa 500 ký tự và không chứa ký tự điều khiển."); return; }
        var content = new ChatEvent { MessageId = Guid.NewGuid(), RoomId = room?.RoomId ?? "", SenderName = sender.PlayerId, Text = text };
        var notification = new NetworkMessage(room == null ? MessageType.LobbyChatEvent : MessageType.RoomChatEvent, content);
        var recipients = room == null ? _sessionManager.GetAllSessions().Where(s => s.IsAuthenticated && s.CurrentRoomId == null)
            : new[] { _sessionManager.GetSession(room.PlayerXId), _sessionManager.GetSession(room.PlayerOId) }
                .Where(s => s?.CurrentRoomId == room.RoomId).Select(s => s!);
        foreach (var recipient in recipients) await SafeSendAsync(recipient, notification);
    }

    private async Task HandleNewGameAsync(PlayerSession sender, NetworkMessage message)
    {
        var room = _roomManager.GetRoom(sender.CurrentRoomId!);
        if (room == null) return;
        if (room.HasDisconnectedPlayers || _sessionManager.GetSession(room.PlayerXId)?.CurrentRoomId != room.RoomId ||
            _sessionManager.GetSession(room.PlayerOId)?.CurrentRoomId != room.RoomId)
        { await Reject(sender, message, "Đối thủ đã rời phòng hoặc mất kết nối."); return; }
        if (room.PendingNewGame is { } existing)
        {
            await sender.SendMessageAsync(new(MessageType.NewGameOfferEvent, existing));
            return; // First proposal wins, including simultaneous requests from both players.
        }
        var offer = new NewGameOfferDto
        {
            RoomId = room.RoomId, MatchIdentity = room.Session.MatchId, OfferIdentity = Guid.NewGuid(),
            RequesterName = sender.PlayerId, ExpiresAtUtc = DateTime.UtcNow.AddSeconds(20)
        };
        room.PendingNewGame = offer;
        await SendPlayersAsync(room, new(MessageType.NewGameOfferEvent, offer));
        _ = ExpireNewGameAsync(room, offer);
    }
    private async Task ExpireNewGameAsync(Room room, NewGameOfferDto offer)
    {
        await Task.Delay(TimeSpan.FromSeconds(20));
        await room.Actions.WaitAsync();
        try
        {
            if (room.PendingNewGame?.OfferIdentity == offer.OfferIdentity)
                await CancelNewGameAsync(room, "Yêu cầu ván mới đã hết thời gian chờ.");
        }
        finally { room.Actions.Release(); }
    }
    private async Task CancelNewGameAsync(Room room, string text)
    {
        if (room.PendingNewGame is not { } offer) return;
        room.PendingNewGame = null;
        await SendPlayersAsync(room, new(MessageType.NewGameOfferResolvedEvent, offer with { Message = text }));
    }
    private async Task SendPlayersAsync(Room room, NetworkMessage message)
    {
        foreach (string id in new[] { room.PlayerXId, room.PlayerOId })
            if (_sessionManager.GetSession(id) is { } session && session.CurrentRoomId == room.RoomId)
                await SafeSendAsync(session, message);
    }
    private async Task ResolveNewGameAsync(PlayerSession sender, NetworkMessage message, Room room)
    {
        var response = Payload<NewGameResponseRequest>(message);
        if (response == null || room.PendingNewGame is not { } offer || response.OfferIdentity != offer.OfferIdentity ||
            sender.PlayerId == offer.RequesterName || offer.MatchIdentity != room.Session.MatchId)
        { await Reject(sender, message, "Yêu cầu ván mới không còn hợp lệ."); return; }
        if (DateTime.UtcNow >= offer.ExpiresAtUtc || room.HasDisconnectedPlayers ||
            _sessionManager.GetSession(room.PlayerXId)?.CurrentRoomId != room.RoomId ||
            _sessionManager.GetSession(room.PlayerOId)?.CurrentRoomId != room.RoomId)
        { await CancelNewGameAsync(room, "Yêu cầu ván mới đã hủy."); return; }
        if (!response.Accept)
        { await CancelNewGameAsync(room, $"{sender.PlayerId} đã từ chối bắt đầu ván mới."); return; }
        room.PendingNewGame = null;
        await SendPlayersAsync(room, new(MessageType.NewGameOfferResolvedEvent, offer with { Accepted = true }));
        await ResetAcceptedGameAsync(sender, new NetworkMessage(MessageType.NewGameRequest,
            JsonSerializer.SerializeToElement(new NewGameRequest { RoomId = room.RoomId })));
        await PublishRoomsAsync();
    }
}
