using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CaroClient;
using CaroServer.Core;
using CaroServer.Data;
using CaroServer.Managers;
using CaroServer.Repositories;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;
using Microsoft.EntityFrameworkCore;

internal static class SocialChecks
{
    internal static async Task Run(int? existingPort = null, string host = "127.0.0.1")
    {
        await CheckResumedTurnAndStaleTimer();
        Program.Assert(new RoomDto { RoomId = "ROOM-847883" }.ToString() == "ROOM - 847883", "Room display must not stringify player DTOs.");
        foreach (string code in new[] { "847883", "ROOM-847883", " ROOM - 847883 " })
            Program.Assert(RoomCodes.Normalize(code) == "ROOM-847883", "Numeric room normalization.");
        Program.Assert(RoomCodes.Normalize("../../847883") == null && RoomCodes.Normalize("abcdef") == null, "Reject invalid room codes.");
        var rooms = new RoomManager();
        var server = existingPort == null ? new TcpServerManager(new SessionManager(), rooms, new LobbyManager(),
            new MatchHistoryRepository(new DbContextOptionsBuilder<CaroDbContext>().UseSqlServer(
                "Server=127.0.0.1,1;Database=CaroSocialTest;User Id=test;Password=TestOnly!;Connect Timeout=1;Encrypt=False").Options), 0) : null;
        Task listening = server?.StartListeningAsync() ?? Task.CompletedTask;
        int port = existingPort ?? ((IPEndPoint)Program.Field<TcpListener>(server!, "_listener").LocalEndpoint).Port;
        try
        {
            using var a = await Peer.Login(host, port, "Trung");
            using var b = await Peer.Login(host, port, "ZOROO");
            using var c = await Peer.Login(host, port, "Tu Doan");
            using var d = await Peer.Login(host, port, "Nam");
            foreach (var (sender, receiver) in new[] { (a, b), (b, a) })
            {
                await sender.Send(MessageType.ChallengeRequest, new ChallengeRequest { TargetPlayerId = receiver.Name });
                await receiver.Read<ChallengeRequest>(MessageType.ChallengeRequest);
                await receiver.Send(MessageType.ChallengeResponse, new ChallengeResponse { ChallengerId = sender.Name });
                var declined = await sender.Read<ChallengeResponse>(MessageType.ChallengeResponse);
                Program.Assert(declined.OpponentName == receiver.Name, "Decline must name recipient, not requester.");
                Program.Assert(LobbyForm.DeclinedInvitationText(declined).Contains($"[{receiver.Name}]"), "Client decline text.");
            }
            await a.Send(MessageType.LobbyChatRequest, new { Text = "Xin chào Việt Nam 👋", SenderName = "Spoofed" });
            var lobbyChat = await b.Read<ChatEvent>(MessageType.LobbyChatEvent);
            Program.Assert(lobbyChat.SenderName == "Trung" && lobbyChat.Text == "Xin chào Việt Nam 👋", "Server identity and exact Unicode chat.");
            await b.ExpectNone(MessageType.LobbyChatEvent);
            await a.Send(MessageType.LobbyChatRequest, new ChatRequest { Text = new string('x', 501) });
            await a.Read<ErrorResponse>(MessageType.ErrorResponse);
            await a.Send(MessageType.LobbyChatRequest, new ChatRequest { Text = "   " });
            await b.ExpectNone(MessageType.LobbyChatEvent);
            Console.WriteLine("PASS: decline identity both directions, exact room display, lobby chat Unicode/identity/length/no duplicates.");

            await a.Send(MessageType.ChallengeRequest, new ChallengeRequest { TargetPlayerId = b.Name });
            await b.Read<ChallengeRequest>(MessageType.ChallengeRequest);
            await b.Send(MessageType.ChallengeResponse, new ChallengeResponse { ChallengerId = a.Name, IsAccepted = true });
            var start = await a.Read<ChallengeResponse>(MessageType.ChallengeResponse);
            await b.Read<ChallengeResponse>(MessageType.ChallengeResponse);
            string roomId = start.RoomId;
            Program.Assert(RoomCodes.Normalize(roomId) == roomId, "Server generates six numeric digits.");
            await a.Send(MessageType.MakeMoveRequest, new MakeMoveRequest { X = 0, Y = 0 });
            await a.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent); await b.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
            await b.Send(MessageType.MakeMoveRequest, new MakeMoveRequest { X = 2, Y = 2 });
            await a.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent); await b.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
            using var avatar = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(avatar)) g.Clear(System.Drawing.Color.SaddleBrown);
            using var bytes = new MemoryStream(); avatar.Save(bytes, System.Drawing.Imaging.ImageFormat.Png);
            string avatarBase64 = Convert.ToBase64String(bytes.ToArray());
            await c.Send(MessageType.AvatarUpdateRequest, new AvatarUpdateRequest { Base64Image = avatarBase64 });
            Program.Assert((await c.Read<AvatarUpdateResponse>(MessageType.AvatarUpdateResponse)).Success, "Spectator avatar upload.");
            await c.Send(MessageType.JoinSpectatorRequest, new JoinSpectatorRequest { RoomId = roomId[5..] });
            var joined = await c.Read<JoinSpectatorResponse>(MessageType.JoinSpectatorResponse);
            Program.Assert(joined.IsSuccess && joined.Snapshot!.Session!.LastMoveX == 2 && joined.Snapshot.Session.LastMoveY == 2, "Numeric join and authoritative last move snapshot.");
            var presence = await a.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => p.JoinedPlayerName == c.Name);
            await b.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => p.JoinedPlayerName == c.Name);
            Program.Assert(presence.Room.Spectators.Single().HasAvatar, "Presence contains avatar metadata.");
            await a.Send(MessageType.AvatarRequest, new AvatarRequest { PlayerId = c.Name });
            Program.Assert((await a.Read<AvatarDataEvent>(MessageType.AvatarDataEvent)).Base64Image == avatarBase64, "Avatar fetch exact bytes.");
            await c.Send(MessageType.PlayerListRequest, new { });
            Program.Assert(!(await c.Read<PlayerListResponse>(MessageType.PlayerListResponse)).Players.Any(p => p.PlayerName == c.Name), "Spectator is not an available opponent.");
            Console.WriteLine("PASS: numeric spectate, two-player join notifications, avatar metadata/data, last move snapshot and availability.");

            foreach (var type in new[] { MessageType.MakeMoveRequest, MessageType.SurrenderRequest, MessageType.DrawOfferRequest,
                         MessageType.DrawResponseRequest, MessageType.NewGameRequest, MessageType.NewGameResponseRequest,
                         MessageType.SetSpectatorLockRequest, MessageType.RoomChatRequest })
            {
                await c.Send(type, new { RoomId = roomId, X = 3, Y = 3, Text = "forbidden", IsLocked = true });
                await c.Read<ErrorResponse>(MessageType.ErrorResponse);
            }
            await d.Send(MessageType.RoomChatRequest, new ChatRequest { RoomId = roomId, Text = "outside" });
            await d.Read<ErrorResponse>(MessageType.ErrorResponse);
            await a.Send(MessageType.RoomChatRequest, new { RoomId = roomId, Text = "Nước cờ đẹp 😊", SenderName = "Fake" });
            Program.Assert((await b.Read<ChatEvent>(MessageType.RoomChatEvent)).SenderName == a.Name, "Private chat identity.");
            await c.ExpectNone(MessageType.RoomChatEvent); await d.ExpectNone(MessageType.RoomChatEvent);
            await a.Send(MessageType.LobbyChatRequest, new ChatRequest { Text = "not in lobby" });
            await a.Read<ErrorResponse>(MessageType.ErrorResponse);
            Console.WriteLine("PASS: all spectator action packets and outsider room chat rejected; private player chat excludes spectators.");

            await a.Send(MessageType.SetSpectatorLockRequest, new RoomAccessRequest { RoomId = roomId, IsLocked = true });
            var locked = await b.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => p.Room.IsSpectatorLocked);
            Program.Assert(locked.Room.Spectators.Any(p => p.PlayerName == c.Name), "Lock retains current spectators.");
            foreach (string code in new[] { roomId[5..], "ROOM - " + roomId[5..] })
            {
                await d.Send(MessageType.JoinSpectatorRequest, new JoinSpectatorRequest { RoomId = code });
                Program.Assert((await d.Read<JoinSpectatorResponse>(MessageType.JoinSpectatorResponse)).ErrorMessage!.Contains("khóa"), "Locked room rejects both entry paths.");
            }
            await b.Send(MessageType.SetSpectatorLockRequest, new RoomAccessRequest { RoomId = roomId, IsLocked = false });
            await a.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => !p.Room.IsSpectatorLocked && p.Room.Revision > locked.Room.Revision);
            await d.Send(MessageType.JoinSpectatorRequest, new JoinSpectatorRequest { RoomId = "ROOM - " + roomId[5..] });
            Program.Assert((await d.Read<JoinSpectatorResponse>(MessageType.JoinSpectatorResponse)).IsSuccess, "Other player can unlock.");
            await a.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => p.Room.SpectatorCount == 2);
            Console.WriteLine("PASS: lock/unlock, locked numeric/list paths rejected, existing spectator retained.");

            await a.Send(MessageType.NewGameRequest, new NewGameRequest { RoomId = roomId });
            var offer = await b.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
            await a.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
            await a.ExpectNone(MessageType.NewGameEvent);
            await b.Send(MessageType.NewGameResponseRequest, new NewGameResponseRequest { RoomId = roomId, OfferIdentity = offer.OfferIdentity });
            var declinedRematch = await a.Read<NewGameOfferDto>(MessageType.NewGameOfferResolvedEvent);
            Program.Assert(declinedRematch.Message.Contains("ZOROO"), "Rematch decline uses responder name.");
            await a.ExpectNone(MessageType.NewGameEvent);
            // Both request concurrently: the first offer wins; the other request never resets the board.
            await Task.WhenAll(a.Send(MessageType.NewGameRequest, new NewGameRequest { RoomId = roomId }),
                b.Send(MessageType.NewGameRequest, new NewGameRequest { RoomId = roomId }));
            var first = await a.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
            var other = await b.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
            Program.Assert(first.OfferIdentity == other.OfferIdentity, "Concurrent requests have a single offer.");
            var responder = first.RequesterName == a.Name ? b : a;
            await responder.Send(MessageType.NewGameResponseRequest, new NewGameResponseRequest { RoomId = roomId, OfferIdentity = first.OfferIdentity, Accept = true });
            var next = await a.Read<NewGameEventDto>(MessageType.NewGameEvent);
            await b.Read<NewGameEventDto>(MessageType.NewGameEvent); await c.Read<NewGameEventDto>(MessageType.NewGameEvent); await d.Read<NewGameEventDto>(MessageType.NewGameEvent);
            Program.Assert(next.MatchIdentity != start.MatchIdentity && next.Timing!.TurnNumber == 1, "Only acceptance resets authoritative match/timer.");
            await responder.Send(MessageType.NewGameResponseRequest, new NewGameResponseRequest { RoomId = roomId, OfferIdentity = first.OfferIdentity, Accept = true });
            await responder.Read<ErrorResponse>(MessageType.ErrorResponse);
            Console.WriteLine("PASS: new game waits, decline preserves board, simultaneous proposals merge, acceptance resets once for players/spectators.");

            await a.Send(MessageType.NewGameRequest, new NewGameRequest { RoomId = roomId });
            offer = await a.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
            var expired = await a.Read<NewGameOfferDto>(MessageType.NewGameOfferResolvedEvent, p => p.OfferIdentity == offer.OfferIdentity, 25);
            Program.Assert(!expired.Accepted && expired.Message.Contains("hết thời gian"), "Actual 20-second proposal timeout.");
            await a.Send(MessageType.NewGameRequest, new NewGameRequest { RoomId = roomId });
            offer = await a.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
            b.Dispose();
            await a.Read<NewGameOfferDto>(MessageType.NewGameOfferResolvedEvent, p => p.OfferIdentity == offer.OfferIdentity);
            await a.Read<GameStateDto>(MessageType.GameStateUpdate);
            using var reconnected = await Peer.Connect(host, port, b.Name);
            await reconnected.Send(MessageType.ReconnectRequest, new ReconnectRequest { SessionToken = b.Token });
            var restored = await reconnected.Read<ReconnectResponse>(MessageType.ReconnectResponse);
            Program.Assert(restored.Success && restored.GameState!.Room!.Spectators.Count == 2 && restored.GameState.Session!.LastMoveX == null,
                "Reconnect restores spectators and reset last-move state.");
            Console.WriteLine("PASS: actual offer timeout, disconnect cancels proposal, reconnect restores spectator snapshot.");

            await c.Send(MessageType.LeaveSpectatorRequest, new RoomAccessRequest { RoomId = roomId });
            await a.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => p.LeftPlayerName == c.Name && p.Room.SpectatorCount == 1);
            d.Dispose();
            await a.Read<RoomPresenceEvent>(MessageType.RoomPresenceEvent, p => p.LeftPlayerName == d.Name && p.Room.SpectatorCount == 0);
            await c.Send(MessageType.PlayerListRequest, new { });
            await c.Read<PlayerListResponse>(MessageType.PlayerListResponse, p => p.Players.Any(p => p.PlayerName == c.Name));
            await a.Send(MessageType.SurrenderRequest, new SurrenderRequest { RoomId = roomId });
            await a.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            await c.Send(MessageType.JoinSpectatorRequest, new JoinSpectatorRequest { RoomId = roomId });
            Program.Assert((await c.Read<JoinSpectatorResponse>(MessageType.JoinSpectatorResponse)).ErrorMessage == "Trận đấu đã kết thúc.", "Finished-room message.");
            await c.Send(MessageType.JoinSpectatorRequest, new JoinSpectatorRequest { RoomId = "bad" });
            Program.Assert((await c.Read<JoinSpectatorResponse>(MessageType.JoinSpectatorResponse)).ErrorMessage == "Không tìm thấy phòng.", "Missing-room message.");
            Console.WriteLine("PASS: spectator exit/disconnect removes ghosts, returns availability, finished/missing room errors.");
        }
        finally { server?.Stop(); await listening; }
    }
    private static async Task CheckResumedTurnAndStaleTimer()
    {
        var rooms = new RoomManager();
        string id = rooms.CreateRoom("TimerX", "TimerO");
        try
        {
            rooms.HandleMove(id, "TimerX", 0, 0);
            rooms.HandleMove(id, "TimerO", 0, 1);
            rooms.HandleMove(id, "TimerX", 1, 0);
            var session = rooms.GetSession(id)!;
            var completed = new TaskCompletionSource<CaroServer.Game.MoveResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            rooms.OnRoomTimeout += (_, result) => { completed.TrySetResult(result); return Task.CompletedTask; };
            session.Timer.StartTurn(4, 1, _ => { }); session.PauseTimer();
            rooms.ResumeTurnTimer(session);
            var result = await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Program.Assert(result.WinnerSymbol == 1, "Resumed turn 4 expires player O, not symbol 4.");
            rooms.ResetRoom(id);
            var stale = (Task)typeof(RoomManager).GetMethod("CompleteTurnTimeoutAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .Invoke(rooms, [session, 2, 4])!;
            await stale;
            Program.Assert(rooms.GetSession(id)!.Engine.Status == "Playing", "Queued old timer cannot end a rematch.");
            Console.WriteLine("PASS: resumed turn maps turn number to correct player; stale timer cannot end new match.");
        }
        finally { rooms.RemoveRoom(id); }
    }
    private sealed class Peer : IDisposable
    {
        private readonly TcpClient _client;
        private readonly StreamReader _reader;
        private readonly MessageSerializer _serializer = new();
        private readonly System.Threading.Channels.Channel<NetworkMessage> _inbox = System.Threading.Channels.Channel.CreateUnbounded<NetworkMessage>();
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly CancellationTokenSource _lifetime = new();
        internal string Name { get; }
        internal string Token { get; private set; } = "";
        private Peer(TcpClient client, string name) { _client = client; _reader = new(client.GetStream(), Encoding.UTF8); Name = name; _ = Pump(); }
        private async Task Pump()
        {
            try
            {
                while (await _reader.ReadLineAsync(_lifetime.Token) is string line)
                {
                    var message = _serializer.Deserialize(line);
                    if (message.Type == MessageType.Ping) await Send(MessageType.Pong, new { });
                    else await _inbox.Writer.WriteAsync(message);
                }
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException or ObjectDisposedException) { }
            finally { _inbox.Writer.TryComplete(); }
        }
        internal static async Task<Peer> Connect(string host, int port, string name)
        { var client = new TcpClient(); await client.ConnectAsync(host, port); return new(client, name); }
        internal static async Task<Peer> Login(string host, int port, string name)
        { var peer = await Connect(host, port, name); await peer.Send(MessageType.LoginRequest, name); peer.Token = (await peer.Read<PlayerListResponse>(MessageType.LoginResponse)).SessionToken!; return peer; }
        internal async Task Send(MessageType type, object payload)
        {
            await _writeLock.WaitAsync();
            try { await _client.GetStream().WriteAsync(Encoding.UTF8.GetBytes(_serializer.Serialize(new(type, payload)))); }
            finally { _writeLock.Release(); }
        }
        internal async Task<T> Read<T>(MessageType type, Func<T, bool>? condition = null, int seconds = 10)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            while (true)
            {
                var message = await _inbox.Reader.ReadAsync(timeout.Token);
                if (message.Type == MessageType.Ping) { await Send(MessageType.Pong, new { }); continue; }
                if (message.Type != type) continue;
                var value = _serializer.DeserializePayload<T>(message);
                if (condition == null || condition(value)) return value;
            }
        }
        internal async Task ExpectNone(MessageType type)
        {
            using var timeout = new CancellationTokenSource(180);
            try
            {
                while (true)
                {
                    var message = await _inbox.Reader.ReadAsync(timeout.Token);
                    Program.Assert(message.Type != type, $"Unexpected packet: {type}");
                    if (message.Type == MessageType.Ping) await Send(MessageType.Pong, new { });
                }
            }
            catch (OperationCanceledException) { }
        }
        public void Dispose() { _lifetime.Cancel(); _client.Dispose(); }
    }
}
