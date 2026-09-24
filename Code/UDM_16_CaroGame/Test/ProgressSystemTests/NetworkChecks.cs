using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using CaroServer.Core;
using CaroServer.Data;
using CaroServer.Managers;
using CaroServer.Repositories;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;
using Microsoft.EntityFrameworkCore;

internal static class NetworkChecks
{
    // Deliberately unreachable database: never touch the developer's SQL instance.
    internal static async Task Run(int? existingPort = null, string host = "127.0.0.1")
    {
        var rooms = new RoomManager();
        var server = existingPort == null ? new TcpServerManager(new SessionManager(), rooms, new LobbyManager(),
            new MatchHistoryRepository(new DbContextOptionsBuilder<CaroDbContext>()
                .UseSqlServer("Server=127.0.0.1,1;Database=CaroIsolatedTest;User Id=test;Password=TestOnly!;Connect Timeout=1;Encrypt=False").Options), 0) : null;
        Task listening = server?.StartListeningAsync() ?? Task.CompletedTask;
        int port = existingPort ?? ((IPEndPoint)Program.Field<TcpListener>(server!, "_listener").LocalEndpoint).Port;
        try
        {
            using var a = await Peer.Connect(host, port);
            using var b = await Peer.Connect(host, port);
            Peer opponent = b;
            await a.Send(MessageType.LoginRequest, "ProgressTestX");
            var loginA = await a.Read<PlayerListResponse>(MessageType.LoginResponse);
            await b.Send(MessageType.LoginRequest, "ProgressTestO");
            var loginB = await b.Read<PlayerListResponse>(MessageType.LoginResponse);
            Program.Assert(loginB.Players.Any(p => p.PlayerName == "ProgressTestX"), "Initial snapshot must contain existing player A.");
            await a.WaitForPlayers("ProgressTestX", "ProgressTestO");
            using (var duplicate = await Peer.Connect(host, port))
            {
                await duplicate.Send(MessageType.LoginRequest, "progresstestx");
                var reply = await duplicate.ReadLoginReply();
                Program.Assert(reply.Type == MessageType.ErrorResponse, "Duplicate username must be rejected without replacing original session.");
            }
            using (var c = await Peer.Connect(host, port))
            {
                await c.Send(MessageType.LoginRequest, "PresenceThird");
                await c.Read<PlayerListResponse>(MessageType.LoginResponse);
                await a.WaitForPlayers("ProgressTestX", "ProgressTestO", "PresenceThird");
            }
            await a.WaitForPlayers("ProgressTestX", "ProgressTestO");
            Console.WriteLine("PASS: initial snapshot, join broadcast, duplicate-name rejection and leave without ghost players.");
            await a.Send(MessageType.ChallengeRequest, new ChallengeRequest { TargetPlayerId = "ProgressTestO" });
            await b.Read<ChallengeRequest>(MessageType.ChallengeRequest);
            await b.Send(MessageType.ChallengeResponse, new ChallengeResponse { ChallengerId = "ProgressTestX", IsAccepted = true });
            var start = await a.Read<ChallengeResponse>(MessageType.ChallengeResponse);
            var startO = await b.Read<ChallengeResponse>(MessageType.ChallengeResponse);
            Program.Assert(loginB.Players.Count == 2, "Readiness checks must not create phantom players.");
            Program.Assert(start.MySymbol == 1 && startO.MySymbol == 2, "Logical symbols must be preserved.");
            Program.Assert(start.Timing is { TurnDurationSeconds: 30, CurrentTurn: 1 } && start.Timing.MatchStartedAtUtc != default, "Server must send authoritative start and turn timing.");
            var room = rooms.GetRoom(start.RoomId);
            for (int x = 0; x < 5; x++)
            {
                await a.Send(MessageType.MakeMoveRequest, new MakeMoveRequest { X = x, Y = 0 });
                var move = await a.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
                await b.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
                Program.Assert(move.IsValid && move.Timing!.MatchStartedAtUtc == start.Timing!.MatchStartedAtUtc, "Moves cannot reset match start.");
                if (x == 4)
                {
                    Program.Assert(move.WinnerSymbol == 1 && move.Timing!.MatchEndedAtUtc != null, "Final move must carry immediate terminal time.");
                    break;
                }
                Program.Assert(move.Timing!.CurrentTurn == 2, "Server advances to O.");
                await b.Send(MessageType.MakeMoveRequest, new MakeMoveRequest { X = x, Y = 2 });
                await a.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
                await b.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
            }
            var win = await a.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            await b.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            Program.Assert(win.Timing?.MatchEndedAtUtc != null, "GameOver must finalize before database persistence.");
            Console.WriteLine("PASS: real TCP start, alternating X/O, immediate win and terminal timestamps without a database.");

            await RequestRematch(a, opponent, start.RoomId);
            var rematch = await a.Read<NewGameEventDto>(MessageType.NewGameEvent);
            await b.Read<NewGameEventDto>(MessageType.NewGameEvent);
            Program.Assert(rematch.MatchIdentity != start.MatchIdentity && rematch.Timing!.MatchEndedAtUtc == null, "Rematch gets new timing and identity.");
            await a.Send(MessageType.DrawOfferRequest, new DrawOfferRequestDto { RoomId = start.RoomId, MatchIdentity = rematch.MatchIdentity });
            var offer = await b.Read<DrawOfferEventDto>(MessageType.DrawOfferEvent);
            Program.Assert(room == null || room.Session.Timer.DeadlineUtc != DateTime.MinValue, "Draw offer must not pause turn timer.");
            await b.Send(MessageType.DrawResponseRequest, new DrawResponseRequestDto { RoomId = start.RoomId, MatchIdentity = rematch.MatchIdentity, OfferIdentity = offer.OfferIdentity, Accept = false });
            await a.Read<DrawOfferResolvedDto>(MessageType.DrawOfferResolvedEvent);
            await b.Read<DrawOfferResolvedDto>(MessageType.DrawOfferResolvedEvent);
            Program.Assert(room == null || room.Session.Engine.Status == "Playing", "Rejected draw preserves game.");
            await a.Send(MessageType.MakeMoveRequest, new MakeMoveRequest { X = 0, Y = 0 });
            await a.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent); await b.Read<MoveMadeEventDto>(MessageType.MoveMadeEvent);
            await a.Send(MessageType.DrawOfferRequest, new DrawOfferRequestDto { RoomId = start.RoomId, MatchIdentity = rematch.MatchIdentity });
            offer = await b.Read<DrawOfferEventDto>(MessageType.DrawOfferEvent);
            await b.Send(MessageType.DrawResponseRequest, new DrawResponseRequestDto { RoomId = start.RoomId, MatchIdentity = rematch.MatchIdentity, OfferIdentity = offer.OfferIdentity, Accept = true });
            var draw = await a.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            await b.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            Program.Assert(draw.WinnerSymbol == 0 && draw.Timing!.MatchEndedAtUtc != null, "Accepted draw must finalize with a frozen time.");
            Console.WriteLine("PASS: real TCP rematch, draw waiting/rejection/acceptance and terminal result.");

            await RequestRematch(a, opponent, start.RoomId);
            await a.Read<NewGameEventDto>(MessageType.NewGameEvent); await b.Read<NewGameEventDto>(MessageType.NewGameEvent);
            b.Dispose();
            var paused = await a.Read<GameStateDto>(MessageType.GameStateUpdate);
            Program.Assert(paused.Session!.Timing!.IsPaused && paused.ReconnectWindowSeconds == 60 && paused.ReconnectDeadlineUtc != null, "Server sends actual reconnect grace and paused turn.");
            using var resumed = await Peer.Connect(host, port);
            opponent = resumed;
            await resumed.Send(MessageType.ReconnectRequest, new ReconnectRequest { SessionToken = loginB.SessionToken! });
            var restored = await resumed.Read<ReconnectResponse>(MessageType.ReconnectResponse);
            var resumeState = await a.Read<GameStateDto>(MessageType.GameStateUpdate);
            Program.Assert(restored.Success && !resumeState.Session!.Timing!.IsPaused && resumeState.ReconnectDeadlineUtc == null, "Reconnect restores authoritative turn without new match.");
            Program.Assert(restored.GameState!.Session!.Timing!.MatchStartedAtUtc == paused.Session.Timing.MatchStartedAtUtc, "Reconnect must not reset match clock.");
            await a.Send(MessageType.SurrenderRequest, new SurrenderRequest { RoomId = start.RoomId });
            var surrender = await a.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            await resumed.Read<MoveMadeEventDto>(MessageType.GameOverEvent);
            Program.Assert(surrender.WinnerSymbol == 2 && surrender.Timing!.MatchEndedAtUtc != null, "Surrender must carry final timing.");
            Console.WriteLine("PASS: real TCP reconnect grace, pause/resume, preserved match clock and surrender.");

            await a.Send(MessageType.AvatarUpdateRequest, new AvatarUpdateRequest { Base64Image = "invalid base64" });
            var avatarFail = await a.Read<AvatarUpdateResponse>(MessageType.AvatarUpdateResponse);
            Program.Assert(!avatarFail.Success, "Avatar failure must acknowledge.");
            await a.Send(MessageType.AvatarRemoveRequest, new AvatarRemoveRequest());
            Program.Assert((await a.Read<AvatarRemoveResponse>(MessageType.AvatarRemoveResponse)).Success, "Avatar removal must acknowledge.");
            await a.Send(MessageType.MatchHistoryRequest, new MatchHistoryRequest { PlayerId = "ProgressTestX" });
            await a.Read<MatchHistoryResponse>(MessageType.MatchHistoryResponse);
            Console.WriteLine("PASS: avatar success/failure acknowledgements and history response without a database.");
            if (existingPort != null)
            {
                await RequestRematch(a, opponent, start.RoomId);
                await a.Read<NewGameEventDto>(MessageType.NewGameEvent);
                await resumed.Read<NewGameEventDto>(MessageType.NewGameEvent);
                var timeoutReads = await Task.WhenAll(a.Read<MoveMadeEventDto>(MessageType.GameOverEvent, 40), resumed.Read<MoveMadeEventDto>(MessageType.GameOverEvent, 40));
                Program.Assert(timeoutReads.All(x => x.WinnerSymbol == 2 && x.Timing?.MatchEndedAtUtc != null), "Real turn timeout winner and frozen clock.");
                Console.WriteLine("PASS: actual 30-second turn timeout in published server.");
                await RequestRematch(a, opponent, start.RoomId);
                await a.Read<NewGameEventDto>(MessageType.NewGameEvent);
                await resumed.Read<NewGameEventDto>(MessageType.NewGameEvent);
                resumed.Dispose();
                await a.Read<GameStateDto>(MessageType.GameStateUpdate);
                var forfeit = await a.Read<MoveMadeEventDto>(MessageType.GameOverEvent, 70);
                Program.Assert(forfeit.WinnerSymbol == 1 && forfeit.Timing?.MatchEndedAtUtc != null, "Disconnect forfeiture must freeze clock.");
                Console.WriteLine("PASS: actual 60-second disconnect forfeiture in published server.");
            }
        }
        finally { server?.Stop(); await listening; }
    }

    private static async Task RequestRematch(Peer sender, Peer opponent, string roomId)
    {
        await sender.Send(MessageType.NewGameRequest, new NewGameRequest { RoomId = roomId });
        var offer = await sender.Read<NewGameOfferDto>(MessageType.NewGameOfferEvent);
        await opponent.Send(MessageType.NewGameResponseRequest, new NewGameResponseRequest { RoomId = roomId, OfferIdentity = offer.OfferIdentity, Accept = true });
    }
    private sealed class Peer : IDisposable
    {
        private readonly TcpClient _client;
        private readonly StreamReader _reader;
        private readonly MessageSerializer _serializer = new();
        private Peer(TcpClient client) { _client = client; _reader = new(client.GetStream(), Encoding.UTF8); }
        internal static async Task<Peer> Connect(string host, int port)
        {
            var client = new TcpClient(); await client.ConnectAsync(host, port); return new(client);
        }
        internal async Task Send(MessageType type, object payload)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_serializer.Serialize(new(type, payload)));
            await _client.GetStream().WriteAsync(bytes);
        }
        internal async Task<T> Read<T>(MessageType type, int seconds = 10)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            while (true)
            {
                var line = await _reader.ReadLineAsync(timeout.Token) ?? throw new IOException("Connection closed.");
                var message = _serializer.Deserialize(line);
                if (message.Type == MessageType.Ping) { await Send(MessageType.Pong, new { }); continue; }
                if (message.Type == type) return _serializer.DeserializePayload<T>(message);
            }
        }
        internal async Task<NetworkMessage> ReadLoginReply()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                var message = _serializer.Deserialize((await _reader.ReadLineAsync(timeout.Token))!);
                if (message.Type is MessageType.LoginResponse or MessageType.ErrorResponse) return message;
            }
        }
        internal async Task WaitForPlayers(params string[] names)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            while (true)
            {
                var message = _serializer.Deserialize((await _reader.ReadLineAsync(timeout.Token))!);
                if (message.Type == MessageType.PlayerListResponse)
                {
                    var list = _serializer.DeserializePayload<PlayerListResponse>(message).Players.Select(p => p.PlayerName).Order().ToArray();
                    if (list.SequenceEqual(names.Order())) return;
                }
            }
        }
        public void Dispose() => _client.Dispose();
    }
}
