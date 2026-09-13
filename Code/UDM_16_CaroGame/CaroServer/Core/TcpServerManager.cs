using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CaroServer.Game;
using CaroServer.Managers;
using CaroServer.Models;
using CaroShared.Constants;
using CaroShared.Enums;
using CaroShared.Protocol;
using CaroShared.Contracts;
using CaroServer.Repositories;

namespace CaroServer.Core
{
    // Quản lý Server và các kết nối TCP
    public class TcpServerManager
    {
        private readonly TcpListener _listener;
        private readonly SessionManager _sessionManager;
        private readonly RoomManager _roomManager;
        private readonly EventBroadcaster _broadcaster;
        private readonly LobbyManager _lobbyManager;
        private readonly MatchHistoryRepository _matchRepo;
        private bool _isRunning;

        // Dùng chung cho Serialize và Deserialize
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public TcpServerManager(SessionManager sessionManager, RoomManager roomManager, LobbyManager lobbyManager, MatchHistoryRepository matchRepo)
            : this(sessionManager, roomManager, lobbyManager, matchRepo, new EventBroadcaster(sessionManager, roomManager))
        {
        }

        public TcpServerManager(SessionManager sessionManager, RoomManager roomManager, LobbyManager lobbyManager, MatchHistoryRepository matchRepo, EventBroadcaster broadcaster)
        {
            _sessionManager = sessionManager;
            _roomManager = roomManager;
            _lobbyManager = lobbyManager;
            _matchRepo = matchRepo;
            _broadcaster = broadcaster;
            // Lắng nghe kết nối trên port mặc định
            _listener = new TcpListener(IPAddress.Any, NetworkConstants.DefaultPort);

            // Đăng ký xử lý khi phòng hết thời gian
            _roomManager.OnRoomTimeout += async (roomId, moveResult) =>
            {
                // Gửi kết quả hết giờ cho tất cả người trong phòng
                var responseDto = new MoveMadeEventDto
                {
                    RoomId = roomId,
                    WinnerSymbol = moveResult.WinnerSymbol,
                    IsValid = true,
                    ErrorMessage = moveResult.ErrorMessage ?? "Hết thời gian lượt đánh"
                };

                var timeoutMsg = new NetworkMessage(MessageType.MoveMadeEvent, responseDto);
                await _broadcaster.BroadcastToRoomAsync(roomId, timeoutMsg);

                // Xóa phòng và giải phóng tài nguyên sau khi kết thúc
                var room = _roomManager.GetRoom(roomId);
                if (room != null)
                {
                    var match = new MatchHistory
                    {
                        RoomId = roomId,
                        PlayerXId = room.PlayerXId,
                        PlayerOId = room.PlayerOId,
                        WinnerSymbol = moveResult.WinnerSymbol,
                        TotalMoves = room.Session.Engine.MoveCount,
                        PlayedAt = DateTime.Now
                    };
                    _ = _matchRepo.SaveMatchAsync(match);

                    foreach (var participantId in room.GetAllParticipantIds())
                    {
                        var pSession = _sessionManager.GetSession(participantId);
                        if (pSession != null) pSession.CurrentRoomId = null;
                    }
                    _roomManager.RemoveRoom(roomId);
                }
            };
        }

        // Bắt đầu lắng nghe
        public async Task StartListeningAsync()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[TcpServer] Server started on port {NetworkConstants.DefaultPort}");

            try
            {
                while (_isRunning)
                {
                    // Chờ Client kết nối
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine($"[TcpServer] Client connected: {client.Client.RemoteEndPoint}");

                    // Khởi tạo session
                    var session = new PlayerSession(client);
                    // Lưu vào Manager
                    _sessionManager.AddSession(session);

                    // Bắt đầu xử lý dữ liệu từ Client
                    _ = ReceiveDataAsync(session); 
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"[TcpServer] Error while listening: {ex.Message}");
                }
            }
        }

        private async Task ReceiveDataAsync(PlayerSession session)
        {
            try
            {
                using var reader = new StreamReader(session.Stream, System.Text.Encoding.UTF8, leaveOpen: true);
                while (_isRunning)
                {
                    string? line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                    {
                        break; // Client ngắt kết nối
                    }

                    try
                    {
                        // Chuyển JSON thành NetworkMessage
                        var message = JsonSerializer.Deserialize<NetworkMessage>(line, JsonOptions);
                        if (message != null)
                        {
                            await HandleIncomingMessage(session, message);
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"[TcpServer] Invalid JSON from {session.PlayerId}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpServer] Connection lost with {session.PlayerId}: {ex.Message}");
            }
            finally
            {
                // Nếu client đang xem hoặc chơi trong phòng thì rút khỏi danh sách phòng
                if (!string.IsNullOrEmpty(session.CurrentRoomId))
                {
                    _roomManager.RemoveSpectator(session.CurrentRoomId, session.PlayerId);
                }

                // Xóa khỏi Lobby và Session khi Client ngắt kết nối
                _lobbyManager.RemovePlayer(session.PlayerId);
                _sessionManager.RemoveSession(session.PlayerId);

                // Broadcast danh sách mới cho các Client còn lại
                await BroadcastPlayerListAsync();
            }
        }

        private async Task HandleIncomingMessage(PlayerSession senderSession, NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.LoginRequest:
                    await HandleLoginAsync(senderSession, message);
                    break;
                case MessageType.ChallengeRequest:
                    await HandleChallengeAsync(senderSession, message);
                    break;
                case MessageType.ChallengeResponse:
                    await HandleChallengeResponseAsync(senderSession, message);
                    break;
                case MessageType.MakeMoveRequest:
                    await HandleMakeMoveAsync(senderSession, message);
                    break;
                case MessageType.MatchHistoryRequest:
                    await HandleMatchHistoryAsync(senderSession, message);
                    break;
                case MessageType.PlayerListRequest:
                    await HandlePlayerListRequestAsync(senderSession, message);
                    break;
                default:
                    Console.WriteLine($"[TcpServer] Unhandled message type: {message.Type}");
                    break;
            }
        }

        private async Task HandleLoginAsync(PlayerSession session, NetworkMessage message)
        {
            var nicknameElement = (JsonElement)message.Payload!;
            var nickname = nicknameElement.GetString();
            if (string.IsNullOrWhiteSpace(nickname)) return;

            Console.WriteLine($"[Login] {session.PlayerId} logged in as {nickname}");

            // Xóa session ID tạm nhưng KHÔNG dispose socket (giữ kết nối sống)
            _sessionManager.RemoveSession(session.PlayerId, dispose: false);
            session.PlayerId = nickname;
            _sessionManager.AddSession(session);

            // Thêm vào danh sách Lobby
            _lobbyManager.AddPlayer(nickname, nickname);

            // Gửi LoginResponse kèm danh sách online cho người vừa đăng nhập
            var playerList = new PlayerListResponse { PlayerNames = _lobbyManager.GetOnlinePlayerNames() };
            var responseMsg = new NetworkMessage(MessageType.LoginResponse, playerList, message.RequestId);
            await session.SendMessageAsync(responseMsg);

            // Broadcast danh sách mới cho tất cả Client đang online
            await BroadcastPlayerListAsync();
        }

        private async Task HandlePlayerListRequestAsync(PlayerSession session, NetworkMessage message)
        {
            var playerList = new PlayerListResponse { PlayerNames = _lobbyManager.GetOnlinePlayerNames() };
            var responseMsg = new NetworkMessage(MessageType.PlayerListResponse, playerList, message.RequestId);
            await session.SendMessageAsync(responseMsg);
        }

        // Gửi PlayerListResponse cho tất cả Client đang online
        private async Task BroadcastPlayerListAsync()
        {
            var playerList = new PlayerListResponse { PlayerNames = _lobbyManager.GetOnlinePlayerNames() };
            var broadcastMsg = new NetworkMessage(MessageType.PlayerListResponse, playerList);

            foreach (var name in playerList.PlayerNames)
            {
                var s = _sessionManager.GetSession(name);
                if (s != null)
                {
                    await s.SendMessageAsync(broadcastMsg);
                }
            }
        }

        private async Task HandleChallengeAsync(PlayerSession senderSession, NetworkMessage message)
        {
            // Chuyển Payload thành ChallengeRequest
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<ChallengeRequest>(JsonOptions);
            if (request == null) return;

            Console.WriteLine($"[Challenge] {senderSession.PlayerId} -> {request.TargetPlayerId}");

            // Tìm session của người được mời
            var targetSession = _sessionManager.GetSession(request.TargetPlayerId);
            if (targetSession != null)
            {
                // Gửi lời mời đến người chơi được chọn
                var forwardMsg = new NetworkMessage(MessageType.ChallengeRequest, new ChallengeRequest { TargetPlayerId = senderSession.PlayerId }, message.RequestId);
                await targetSession.SendMessageAsync(forwardMsg);
            }
        }

        private async Task HandleChallengeResponseAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var response = jsonElement.Deserialize<ChallengeResponse>(JsonOptions);
            if (response == null) return;

            Console.WriteLine($"[ChallengeResponse] {senderSession.PlayerId} replied to {response.ChallengerId}: {(response.IsAccepted ? "Accept" : "Decline")}");

            var challengerSession = _sessionManager.GetSession(response.ChallengerId);
            if (challengerSession != null)
            {
                if (response.IsAccepted)
                {
                    // Tạo phòng bằng RoomManager
                    string roomId = _roomManager.CreateRoom(challengerSession.PlayerId, senderSession.PlayerId);
                    challengerSession.CurrentRoomId = roomId;
                    senderSession.CurrentRoomId = roomId;

                    // Xóa khỏi Lobby vì đã vào phòng chơi
                    _lobbyManager.RemovePlayer(challengerSession.PlayerId);
                    _lobbyManager.RemovePlayer(senderSession.PlayerId);
                    await BroadcastPlayerListAsync();
                }

                // Gửi kết quả trả lời cho người gửi lời mời
                await challengerSession.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine($"[TcpServer] Challenger {response.ChallengerId} is offline.");
            }
        }

        private async Task HandleMakeMoveAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var moveRequest = jsonElement.Deserialize<MakeMoveRequest>(JsonOptions);
            if (moveRequest == null) return;

            string? roomId = senderSession.CurrentRoomId;
            if (string.IsNullOrEmpty(roomId))
            {
                Console.WriteLine($"[TcpServer] {senderSession.PlayerId} is not in a room.");
                return;
            }

            // Gọi GameEngine của Dev 2
            MoveResult result = _roomManager.HandleMove(roomId, senderSession.PlayerId, moveRequest.X, moveRequest.Y);

            Console.WriteLine($"[MakeMove] {senderSession.PlayerId} at ({moveRequest.X},{moveRequest.Y}): Valid={result.IsValid}");

            // Broadcast kết quả cho tất cả người chơi và khán giả trong phòng
            var responseDto = new MoveMadeEventDto
            {
                RoomId = roomId,
                PlayerId = senderSession.PlayerId,
                X = moveRequest.X,
                Y = moveRequest.Y,
                WinnerSymbol = result.WinnerSymbol,
                IsValid = result.IsValid,
                ErrorMessage = result.ErrorMessage ?? string.Empty
            };

            var resultMsg = new NetworkMessage(MessageType.MoveMadeEvent, responseDto);
            await _broadcaster.BroadcastToRoomAsync(roomId, resultMsg);

            // Nếu game kết thúc, dọn phòng và reset CurrentRoomId cho toàn bộ người tham gia
            if (result.IsGameOver)
            {
                var room = _roomManager.GetRoom(roomId);
                if (room != null)
                {
                    var match = new MatchHistory
                    {
                        RoomId = roomId,
                        PlayerXId = room.PlayerXId,
                        PlayerOId = room.PlayerOId,
                        WinnerSymbol = result.WinnerSymbol,
                        TotalMoves = room.Session.Engine.MoveCount,
                        PlayedAt = DateTime.Now
                    };
                    // Chạy ngầm lưu DB không block luồng mạng
                    _ = _matchRepo.SaveMatchAsync(match);

                    foreach (var participantId in room.GetAllParticipantIds())
                    {
                        var pSession = _sessionManager.GetSession(participantId);
                        if (pSession != null) pSession.CurrentRoomId = null;
                    }
                    _roomManager.RemoveRoom(roomId);
                }
            }
        }

        private async Task HandleMatchHistoryAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<MatchHistoryRequest>(JsonOptions) ?? new MatchHistoryRequest();
            
            // Nếu Client không truyền PlayerId, lấy mặc định là chính người gửi
            string targetPlayerId = request.PlayerId ?? senderSession.PlayerId;

            Console.WriteLine($"[MatchHistory] Fetching history for {targetPlayerId}");

            var histories = await _matchRepo.GetMatchHistoryAsync(targetPlayerId);

            var responseDto = new MatchHistoryResponse();
            foreach (var h in histories)
            {
                responseDto.Matches.Add(new MatchDto
                {
                    RoomId = h.RoomId,
                    PlayerXId = h.PlayerXId,
                    PlayerOId = h.PlayerOId,
                    WinnerSymbol = h.WinnerSymbol,
                    TotalMoves = h.TotalMoves,
                    PlayedAt = h.PlayedAt
                });
            }

            var responseMsg = new NetworkMessage(MessageType.MatchHistoryResponse, responseDto, message.RequestId);
            await senderSession.SendMessageAsync(responseMsg);
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("[TcpServer] Server stopped.");
        }
    }
}
