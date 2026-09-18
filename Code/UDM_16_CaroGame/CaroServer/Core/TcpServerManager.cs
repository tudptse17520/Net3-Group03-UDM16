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

                // Lưu lịch sử đấu sau khi kết thúc do timeout
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
                await HandleClientDisconnectedAsync(session);
            }
        }

        private async Task HandleClientDisconnectedAsync(PlayerSession session)
        {
            // Chỉ xử lý connection hiện tại. Nếu session cũ đã bị thay bằng
            // connection Reconnect mới thì không được xóa connection mới.
            if (_sessionManager.GetSession(session.PlayerId) is not PlayerSession current ||
                !ReferenceEquals(current, session))
            {
                session.Dispose();
                return;
            }

            string? roomId = session.CurrentRoomId;

            if (!string.IsNullOrEmpty(roomId))
            {
                var room = _roomManager.GetRoom(roomId);
                if (room != null && room.IsPlayer(session.PlayerId) && room.Session.Engine.Status == "Playing")
                {
                    room.MarkPlayerDisconnected(session.PlayerId);
                    room.Session.PauseTimer();

                    var pending = _sessionManager.HoldForReconnect(session);
                    _sessionManager.RemoveSession(session.PlayerId, session, dispose: false);
                    session.Dispose();

                    Console.WriteLine($"[Reconnect] {pending.PlayerId} disconnected from {roomId}. Window={GameConstants.ReconnectWindowSeconds}s");
                    _ = ExpireReconnectAsync(pending);
                }
                else
                {
                    _roomManager.RemoveSpectator(roomId, session.PlayerId);
                    _sessionManager.RemoveSession(session.PlayerId, session);
                }
            }
            else
            {
                _lobbyManager.RemovePlayer(session.PlayerId);
                _sessionManager.RemoveSession(session.PlayerId, session);
            }

            await BroadcastPlayerListAsync();
        }

        private async Task ExpireReconnectAsync(PendingReconnectSession pending)
        {
            await Task.Delay(TimeSpan.FromSeconds(GameConstants.ReconnectWindowSeconds));

            if (!_sessionManager.TryTakePendingReconnectSession(
                    pending.SessionToken,
                    out var expired))
            {
                // Session đã được Reconnect thành công hoặc đã được xử lý.
                return;
            }

            if (string.IsNullOrEmpty(expired.RoomId))
            {
                return;
            }

            var room = _roomManager.GetRoom(expired.RoomId);
            if (room == null || !room.IsPlayer(expired.PlayerId))
            {
                return;
            }

            room.MarkPlayerReconnected(expired.PlayerId);
            int symbol = room.GetPlayerSymbol(expired.PlayerId);
            MoveResult result = room.Session.Engine.ForfeitPlayer(
                symbol,
                $"Người chơi không kết nối lại trong {GameConstants.ReconnectWindowSeconds} giây");

            if (!result.IsGameOver)
            {
                return;
            }

            await CompleteGameAsync(expired.RoomId, result);
        }

        private async Task HandleReconnectAsync(PlayerSession newSession, NetworkMessage message)
        {
            if (message.Payload is not JsonElement element)
            {
                await SendReconnectFailureAsync(newSession, message.RequestId, "Thiếu SessionToken.");
                return;
            }

            var request = element.Deserialize<ReconnectRequest>(JsonOptions);
            if (request == null || string.IsNullOrWhiteSpace(request.SessionToken))
            {
                await SendReconnectFailureAsync(newSession, message.RequestId, "SessionToken không hợp lệ.");
                return;
            }

            if (!_sessionManager.TryTakeReconnectSession(
                    request.SessionToken,
                    TimeSpan.FromSeconds(GameConstants.ReconnectWindowSeconds),
                    out var pending))
            {
                await SendReconnectFailureAsync(newSession, message.RequestId,
                    "Phiên reconnect không tồn tại hoặc đã hết thời gian.");
                return;
            }

            if (string.IsNullOrEmpty(pending.RoomId))
            {
                await SendReconnectFailureAsync(newSession, message.RequestId,
                    "Phiên này không có trận đấu đang được giữ.");
                return;
            }

            var room = _roomManager.GetRoom(pending.RoomId);
            if (room == null || !room.IsPlayer(pending.PlayerId) || room.Session.Engine.Status != "Playing")
            {
                await SendReconnectFailureAsync(newSession, message.RequestId,
                    "Trận đấu không còn tồn tại.");
                return;
            }

            // Bỏ session tạm được tạo khi TCP connection mới vào.
            _sessionManager.RemoveSession(newSession.PlayerId, newSession, dispose: false);

            newSession.RestoreConnection(
                newSession.Client,
                pending.PlayerId,
                pending.SessionToken,
                pending.RoomId);
            _sessionManager.AddSession(newSession);

            room.MarkPlayerReconnected(pending.PlayerId);
            if (!room.HasDisconnectedPlayers)
            {
                room.Session.ResumeTimer(turn => _ = HandleRoomTimeoutAsync(room.RoomId, turn));
            }

            var state = new GameStateDto
            {
                Room = new RoomDto
                {
                    RoomId = room.RoomId,
                    PlayerX = room.PlayerXId,
                    PlayerO = room.PlayerOId,
                    SpectatorCount = room.SpectatorCount
                },
                Session = room.Session.ToDto()
            };

            var response = new ReconnectResponse
            {
                Success = true,
                Message = "Reconnect thành công.",
                GameState = state
            };

            await newSession.SendMessageAsync(
                new NetworkMessage(MessageType.ReconnectResponse, response, message.RequestId));

            Console.WriteLine($"[Reconnect] {pending.PlayerId} restored room {pending.RoomId}.");
        }

        private async Task SendReconnectFailureAsync(PlayerSession session, string requestId, string message)
        {
            var response = new ReconnectResponse
            {
                Success = false,
                Message = message
            };

            await session.SendMessageAsync(
                new NetworkMessage(MessageType.ReconnectResponse, response, requestId));
        }

        private async Task HandleRoomTimeoutAsync(string roomId, int timedOutPlayerSymbol)
        {
            var result = _roomManager.HandleTimeout(roomId, timedOutPlayerSymbol);
            if (result != null && result.IsGameOver)
            {
                await CompleteGameAsync(roomId, result);
            }
        }

        private async Task CompleteGameAsync(string roomId, MoveResult result)
        {
            var room = _roomManager.GetRoom(roomId);
            if (room == null) return;

            var responseDto = new MoveMadeEventDto
            {
                RoomId = roomId,
                WinnerSymbol = result.WinnerSymbol,
                IsValid = true,
                ErrorMessage = result.ErrorMessage ?? string.Empty
            };

            await _broadcaster.BroadcastToRoomAsync(
                roomId,
                new NetworkMessage(MessageType.GameOverEvent, responseDto));

            var match = new MatchHistory
            {
                RoomId = roomId,
                PlayerXId = room.PlayerXId,
                PlayerOId = room.PlayerOId,
                WinnerSymbol = result.WinnerSymbol,
                TotalMoves = room.Session.Engine.MoveCount,
                PlayedAt = DateTime.Now
            };
            _ = _matchRepo.SaveMatchAsync(match);

            foreach (var participantId in room.GetAllParticipantIds())
            {
                var participant = _sessionManager.GetSession(participantId);
                if (participant != null) participant.CurrentRoomId = null;
            }

            _roomManager.RemoveRoom(roomId);
        }

        private async Task HandleIncomingMessage(PlayerSession senderSession, NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.LoginRequest:
                    await HandleLoginAsync(senderSession, message);
                    break;
                case MessageType.ReconnectRequest:
                    await HandleReconnectAsync(senderSession, message);
                    break;
                case MessageType.Pong:
                    // Pong chỉ là heartbeat; không cần phản hồi thêm.
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
                case MessageType.SurrenderRequest:
                    await HandleSurrenderAsync(senderSession, message);
                    break;
                case MessageType.NewGameRequest:
                    await HandleNewGameAsync(senderSession, message);
                    break;
                case MessageType.JoinSpectatorRequest:
                    await HandleJoinSpectatorAsync(senderSession, message);
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
            var playerList = new PlayerListResponse
            {
                PlayerNames = _lobbyManager.GetOnlinePlayerNames(),
                SessionToken = session.SessionToken
            };
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

            // Nếu game kết thúc, lưu lịch sử ván đấu vào DB (giữ Room để người chơi có thể chơi ván mới)
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

        private async Task HandleSurrenderAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<SurrenderRequest>(JsonOptions);
            string roomId = !string.IsNullOrEmpty(request?.RoomId) ? request.RoomId : (senderSession.CurrentRoomId ?? string.Empty);

            if (string.IsNullOrEmpty(roomId)) return;

            var room = _roomManager.GetRoom(roomId);
            if (room == null || !room.IsPlayer(senderSession.PlayerId)) return;

            // Xác định người thắng: Đối thủ của người đầu hàng thắng (1 = X, 2 = O)
            int winnerSymbol = (senderSession.PlayerId == room.PlayerXId) ? 2 : 1;

            // Dừng timer ván đấu
            room.Session.StopTimer();

            // Lưu kết quả vào DB
            var match = new MatchHistory
            {
                RoomId = roomId,
                PlayerXId = room.PlayerXId,
                PlayerOId = room.PlayerOId,
                WinnerSymbol = winnerSymbol,
                TotalMoves = room.Session.Engine.MoveCount,
                PlayedAt = DateTime.Now
            };
            _ = _matchRepo.SaveMatchAsync(match);

            // Broadcast GameOverEvent với payload MoveMadeEventDto để Client hiển thị popup
            var gameOverDto = new MoveMadeEventDto
            {
                RoomId = roomId,
                PlayerId = senderSession.PlayerId,
                WinnerSymbol = winnerSymbol,
                IsValid = true,
                ErrorMessage = $"Người chơi {senderSession.PlayerId} đã đầu hàng."
            };

            var gameOverMsg = new NetworkMessage(MessageType.GameOverEvent, gameOverDto, message.RequestId);
            await _broadcaster.BroadcastToRoomAsync(roomId, gameOverMsg);
        }

        private async Task HandleNewGameAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<NewGameRequest>(JsonOptions);
            string roomId = !string.IsNullOrEmpty(request?.RoomId) ? request.RoomId : (senderSession.CurrentRoomId ?? string.Empty);

            if (string.IsNullOrEmpty(roomId)) return;

            var room = _roomManager.GetRoom(roomId);
            if (room == null || !room.IsPlayer(senderSession.PlayerId)) return;

            // Reset ván cờ và timer trong cùng phòng
            bool resetSuccess = _roomManager.ResetRoom(roomId);
            if (!resetSuccess) return;

            var newGameDto = new NewGameEventDto
            {
                RoomId = roomId,
                StartingTurn = 1,
                Message = $"Ván mới đã bắt đầu do {senderSession.PlayerId} yêu cầu!"
            };

            var newGameMsg = new NetworkMessage(MessageType.NewGameEvent, newGameDto, message.RequestId);
            await _broadcaster.BroadcastToRoomAsync(roomId, newGameMsg);
        }

        private async Task HandleJoinSpectatorAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<JoinSpectatorRequest>(JsonOptions);
            string roomId = request?.RoomId ?? string.Empty;

            if (string.IsNullOrWhiteSpace(roomId))
            {
                var errResp = new JoinSpectatorResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Mã phòng không hợp lệ."
                };
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.JoinSpectatorResponse, errResp, message.RequestId));
                return;
            }

            var room = _roomManager.GetRoom(roomId);
            if (room == null)
            {
                var errResp = new JoinSpectatorResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Phòng không tồn tại hoặc trận đấu đã kết thúc."
                };
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.JoinSpectatorResponse, errResp, message.RequestId));
                return;
            }

            bool added = _roomManager.AddSpectator(roomId, senderSession.PlayerId);
            if (!added && !room.IsSpectator(senderSession.PlayerId))
            {
                var errResp = new JoinSpectatorResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Không thể tham gia xem phòng này (bạn có thể là người chơi chính)."
                };
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.JoinSpectatorResponse, errResp, message.RequestId));
                return;
            }

            senderSession.CurrentRoomId = roomId;

            var snapshot = new SpectatorStateSnapshotDto
            {
                Room = new RoomDto
                {
                    RoomId = room.RoomId,
                    PlayerX = room.PlayerXId,
                    PlayerO = room.PlayerOId,
                    SpectatorCount = room.SpectatorCount
                },
                Session = room.Session.ToDto()
            };

            var successResp = new JoinSpectatorResponse
            {
                IsSuccess = true,
                Snapshot = snapshot
            };

            await senderSession.SendMessageAsync(new NetworkMessage(MessageType.JoinSpectatorResponse, successResp, message.RequestId));
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("[TcpServer] Server stopped.");
        }
    }
}
