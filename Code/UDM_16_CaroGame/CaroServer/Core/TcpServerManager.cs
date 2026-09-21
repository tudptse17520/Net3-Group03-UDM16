using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CaroServer.Game;
using CaroServer.Heartbeat;
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
        private HeartbeatManager? _heartbeatManager;
        private bool _isRunning;

        // Cho phép Program.cs inject HeartbeatManager sau khi khởi tạo
        public void SetHeartbeatManager(HeartbeatManager hb) => _heartbeatManager = hb;

        // Dùng chung cho Serialize và Deserialize
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public TcpServerManager(SessionManager sessionManager, RoomManager roomManager, LobbyManager lobbyManager, MatchHistoryRepository matchRepo, int port = NetworkConstants.DefaultPort)
            : this(sessionManager, roomManager, lobbyManager, matchRepo, new EventBroadcaster(sessionManager, roomManager), port)
        {
        }

        public TcpServerManager(SessionManager sessionManager, RoomManager roomManager, LobbyManager lobbyManager, MatchHistoryRepository matchRepo, EventBroadcaster broadcaster, int port = NetworkConstants.DefaultPort)
        {
            _sessionManager = sessionManager;
            _roomManager = roomManager;
            _lobbyManager = lobbyManager;
            _matchRepo = matchRepo;
            _broadcaster = broadcaster;
            // Lắng nghe kết nối trên port được cấu hình
            _listener = new TcpListener(IPAddress.Any, port);

            // Đăng ký xử lý khi phòng hết thời gian (chuyển sang gọi CompleteGameAsync qua event hoặc trực tiếp)
            _roomManager.OnRoomTimeout += async (roomId, moveResult) =>
            {
                await CompleteGameAsync(roomId, moveResult);
            };
        }

        // Bắt đầu lắng nghe
        public async Task StartListeningAsync()
        {
            _listener.Start();
            _isRunning = true;
            var actualPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            Console.WriteLine($"[TcpServer] Server started on port {actualPort}");

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
                        try
                        {
                            var errorResp = new ErrorResponse
                            {
                                Code = ErrorCode.MalformedPayload,
                                Message = "Tin nhắn JSON không hợp lệ."
                            };
                            await session.SendMessageAsync(
                                new NetworkMessage(MessageType.ErrorResponse, errorResp));
                        }
                        catch { /* Ignore send failure */ }
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
            _heartbeatManager?.MarkDisconnected(session.PlayerId);
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
                    PlayerX = CreatePlayerInfo(room.PlayerXId),
                    PlayerO = CreatePlayerInfo(room.PlayerOId),
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

            // Đảm bảo chỉ xử lý kết thúc game 1 lần duy nhất cho mỗi ván đấu (Match)
            if (!room.Session.TryClaimCompletion()) return;

            room.Session.StopTimer();
            room.Session.ClearDrawOffer(); // Xóa trạng thái hòa khi ván kết thúc (bởi mọi lý do)

            var match = new MatchHistory
            {
                RoomId = roomId,
                PlayerXId = room.PlayerXId,
                PlayerOId = room.PlayerOId,
                WinnerSymbol = result.WinnerSymbol,
                TotalMoves = room.Session.Engine.MoveCount,
                PlayedAt = DateTime.Now
            };
            
            // Construct the event before sending to DB so we don't delay the client
            var responseDto = new MoveMadeEventDto
            {
                RoomId = roomId,
                WinnerSymbol = result.WinnerSymbol,
                IsValid = true,
                ErrorMessage = result.ErrorMessage ?? string.Empty
            };

            // BROADCAST IMMEDIATELY to avoid latency locking Phase B on client
            await _broadcaster.BroadcastToRoomAsync(
                roomId,
                new NetworkMessage(MessageType.GameOverEvent, responseDto));

            // Run persistence securely without blocking the broadcast flow
            _ = Task.Run(async () =>
            {
                try
                {
                    await _matchRepo.SaveMatchAsync(match);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MatchHistory] Save failed for match in room {roomId}: {ex.Message}");
                }
            });
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
                    _heartbeatManager?.ReceivePong(senderSession.PlayerId);
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
                case MessageType.RoomListRequest:
                    await HandleRoomListRequestAsync(senderSession, message);
                    break;
                
                case MessageType.DrawOfferRequest:
                    await HandleDrawOfferRequestAsync(senderSession, message);
                    break;
                case MessageType.DrawResponseRequest:
                    await HandleDrawResponseRequestAsync(senderSession, message);
                    break;
                    
                case MessageType.AvatarUpdateRequest:
                    await HandleAvatarUpdateAsync(senderSession, message);
                    break;
                case MessageType.AvatarRemoveRequest:
                    await HandleAvatarRemoveAsync(senderSession, message);
                    break;
                case MessageType.AvatarRequest:
                    await HandleAvatarRequestAsync(senderSession, message);
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
            if (string.IsNullOrWhiteSpace(nickname))
            {
                var errorResp = new ErrorResponse
                {
                    Code = ErrorCode.InvalidRequest,
                    Message = "Nickname không được để trống."
                };
                await session.SendMessageAsync(
                    new NetworkMessage(MessageType.ErrorResponse, errorResp, message.RequestId));
                return;
            }

            Console.WriteLine($"[Login] {session.PlayerId} logged in as {nickname}");

            // Xóa session ID tạm nhưng KHÔNG dispose socket (giữ kết nối sống)
            _sessionManager.RemoveSession(session.PlayerId, dispose: false);
            session.PlayerId = nickname;
            _sessionManager.AddSession(session);

            // Thêm vào danh sách Lobby
            _lobbyManager.AddPlayer(nickname, nickname);
            _heartbeatManager?.RegisterClient(nickname);

            // Gửi LoginResponse kèm danh sách online cho người vừa đăng nhập
            var playerList = new PlayerListResponse
            {
                Players = GetOnlinePlayersInfo(),
                SessionToken = session.SessionToken
            };
            var responseMsg = new NetworkMessage(MessageType.LoginResponse, playerList, message.RequestId);
            await session.SendMessageAsync(responseMsg);

            // Broadcast danh sách mới cho tất cả Client đang online
            await BroadcastPlayerListAsync();
        }

        private async Task HandlePlayerListRequestAsync(PlayerSession session, NetworkMessage message)
        {
            var playerList = new PlayerListResponse { Players = GetOnlinePlayersInfo() };
            var responseMsg = new NetworkMessage(MessageType.PlayerListResponse, playerList, message.RequestId);
            await session.SendMessageAsync(responseMsg);
        }

        // Gửi PlayerListResponse cho tất cả Client đang online
        private async Task BroadcastPlayerListAsync()
        {
            var playerList = new PlayerListResponse { Players = GetOnlinePlayersInfo() };
            var broadcastMsg = new NetworkMessage(MessageType.PlayerListResponse, playerList);

            foreach (var info in playerList.Players)
            {
                var s = _sessionManager.GetSession(info.PlayerName);
                if (s != null)
                {
                    await s.SendMessageAsync(broadcastMsg);
                }
            }
        }

        private List<PlayerInfoDto> GetOnlinePlayersInfo()
        {
            var names = _lobbyManager.GetOnlinePlayerNames();
            var list = new List<PlayerInfoDto>();
            foreach (var name in names)
            {
                list.Add(CreatePlayerInfo(name));
            }
            return list;
        }

        private PlayerInfoDto CreatePlayerInfo(string? playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null!;
            var s = _sessionManager.GetSession(playerId);
            if (s != null)
            {
                return new PlayerInfoDto 
                { 
                    PlayerName = playerId, 
                    HasAvatar = s.AvatarBytes != null, 
                    AvatarVersion = s.AvatarVersion 
                };
            }
            return new PlayerInfoDto { PlayerName = playerId };
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

                    // Gửi kết quả cho người mời (Challenger) — họ cầm X (1)
                    var room = _roomManager.GetRoom(roomId);
                    var matchId = room?.Session.MatchId ?? Guid.Empty;

                    var acceptForChallenger = new ChallengeResponse
                    {
                        ChallengerId = response.ChallengerId,
                        IsAccepted = true,
                        RoomId = roomId,
                        MySymbol = 1,
                        OpponentName = senderSession.PlayerId,
                        MatchIdentity = matchId
                    };
                    await challengerSession.SendMessageAsync(new NetworkMessage(MessageType.ChallengeResponse, acceptForChallenger, message.RequestId));

                    // Gửi kết quả cho người được mời (Sender) — họ cầm O (2)
                    var acceptForSender = new ChallengeResponse
                    {
                        ChallengerId = response.ChallengerId,
                        IsAccepted = true,
                        RoomId = roomId,
                        MySymbol = 2,
                        OpponentName = challengerSession.PlayerId,
                        MatchIdentity = matchId
                    };
                    await senderSession.SendMessageAsync(new NetworkMessage(MessageType.ChallengeResponse, acceptForSender, message.RequestId));
                }
                else
                {
                    // Từ chối -> chỉ gửi cho người mời
                    await challengerSession.SendMessageAsync(message);
                }
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

            var room = _roomManager.GetRoom(roomId);
            if (room == null) return;

            // Gọi GameEngine của Dev 2
            MoveResult result = _roomManager.HandleMove(roomId, senderSession.PlayerId, moveRequest.X, moveRequest.Y);

            Console.WriteLine($"[MakeMove] {senderSession.PlayerId} at ({moveRequest.X},{moveRequest.Y}): Valid={result.IsValid}");

            // Nếu nước đi hợp lệ, Hủy đề nghị hòa đang chờ (nếu có)
            if (result.IsValid && room.Session.PendingDrawOfferId.HasValue)
            {
                var cancelledOfferId = room.Session.PendingDrawOfferId.Value;
                var matchId = room.Session.MatchId;
                room.Session.ClearDrawOffer();
                
                var cancelDto = new DrawOfferResolvedDto
                {
                    RoomId = roomId,
                    MatchIdentity = matchId,
                    OfferIdentity = cancelledOfferId,
                    Accepted = false,
                    Cancelled = true,
                    Message = "Đề nghị hòa đã bị hủy vì có nước cờ mới."
                };
                await _broadcaster.BroadcastToRoomAsync(roomId, new NetworkMessage(MessageType.DrawOfferResolvedEvent, cancelDto));
            }

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

            // Nếu game kết thúc, xử lý GameOver qua luồng tập trung
            if (result.IsGameOver)
            {
                result.ErrorMessage = result.IsDraw ? "Hòa cờ!" : $"Người chơi {senderSession.PlayerId} đã chiến thắng!";
                await CompleteGameAsync(roomId, result);
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

            int surrenderingSymbol = room.Session.GetPlayerSymbol(senderSession.PlayerId);
            
            // Xử lý đầu hàng qua Engine để cập nhật trạng thái "Finished" và lấy MoveResult chuẩn
            var result = room.Session.Engine.ForfeitPlayer(surrenderingSymbol, $"Người chơi {senderSession.PlayerId} đã đầu hàng.");
            if (result.IsValid)
            {
                result.ErrorMessage = $"Người chơi {senderSession.PlayerId} đã đầu hàng.";
                await CompleteGameAsync(roomId, result);
            }
            else
            {
                // Trận đấu đã kết thúc, người chơi gửi Surrender thực chất là rời phòng (Về Sảnh)
                room.MarkPlayerDisconnected(senderSession.PlayerId);
                senderSession.CurrentRoomId = null;

                if (room.IsPlayerDisconnected(room.PlayerXId) && room.IsPlayerDisconnected(room.PlayerOId))
                {
                    _roomManager.RemoveRoom(roomId);
                }
            }
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
                MatchIdentity = room.Session.MatchId,
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
                    PlayerX = CreatePlayerInfo(room.PlayerXId),
                    PlayerO = CreatePlayerInfo(room.PlayerOId),
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

        // Xử lý yêu cầu lấy danh sách phòng đang hoạt động
        private async Task HandleRoomListRequestAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var rooms = _roomManager.GetActiveRooms();
            var dtos = new List<RoomDto>();
            foreach (var r in rooms)
            {
                dtos.Add(new RoomDto
                {
                    RoomId = r.RoomId,
                    PlayerX = CreatePlayerInfo(r.PlayerXId),
                    PlayerO = CreatePlayerInfo(r.PlayerOId),
                    SpectatorCount = r.SpectatorCount
                });
            }
            var response = new RoomListResponse { Rooms = dtos };
            var responseMsg = new NetworkMessage(MessageType.RoomListResponse, response, message.RequestId);
            await senderSession.SendMessageAsync(responseMsg);
        }

        private async Task HandleDrawOfferRequestAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<DrawOfferRequestDto>(JsonOptions);
            if (request == null) return;

            var room = _roomManager.GetRoom(request.RoomId);
            if (room == null || !room.IsPlayer(senderSession.PlayerId))
            {
                // Thông báo lỗi nếu không hợp lệ
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.DrawOfferResolvedEvent, new DrawOfferResolvedDto
                {
                    RoomId = request.RoomId,
                    MatchIdentity = request.MatchIdentity,
                    Accepted = false,
                    Cancelled = true,
                    Message = "Yêu cầu không hợp lệ."
                }, message.RequestId));
                return;
            }

            if (room.Session.MatchId != request.MatchIdentity)
            {
                // Lỗi stale match
                return;
            }

            if (room.Session.Engine.IsGameOver)
            {
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.DrawOfferResolvedEvent, new DrawOfferResolvedDto
                {
                    RoomId = request.RoomId,
                    MatchIdentity = request.MatchIdentity,
                    Accepted = false,
                    Cancelled = true,
                    Message = "Trận đấu đã kết thúc."
                }, message.RequestId));
                return;
            }

            string opponentId = senderSession.PlayerId == room.PlayerXId ? room.PlayerOId : room.PlayerXId;

            if (room.Session.TrySetDrawOffer(senderSession.PlayerId, opponentId, out Guid offerId))
            {
                var eventDto = new DrawOfferEventDto
                {
                    RoomId = room.RoomId,
                    MatchIdentity = room.Session.MatchId,
                    OfferIdentity = offerId,
                    OfferedByPlayerId = senderSession.PlayerId,
                    OfferedByPlayerName = senderSession.PlayerId // Có thể lấy name thật nếu khác ID
                };
                
                // Gửi event cho đối thủ
                var opponentSession = _sessionManager.GetSession(opponentId);
                if (opponentSession != null)
                {
                    await opponentSession.SendMessageAsync(new NetworkMessage(MessageType.DrawOfferEvent, eventDto));
                }
            }
            else
            {
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.DrawOfferResolvedEvent, new DrawOfferResolvedDto
                {
                    RoomId = request.RoomId,
                    MatchIdentity = request.MatchIdentity,
                    Accepted = false,
                    Cancelled = true,
                    Message = "Đang có lời mời hòa chờ xử lý hoặc bạn phải đánh thêm 1 nước cờ mới được mời tiếp."
                }, message.RequestId));
            }
        }

        private async Task HandleDrawResponseRequestAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<DrawResponseRequestDto>(JsonOptions);
            if (request == null) return;

            var room = _roomManager.GetRoom(request.RoomId);
            if (room == null || !room.IsPlayer(senderSession.PlayerId)) return;

            if (room.Session.MatchId != request.MatchIdentity) return;
            if (room.Session.PendingDrawOfferId != request.OfferIdentity) return;
            if (room.Session.PendingDrawOfferedTo != senderSession.PlayerId) return;

            // Xóa offer
            room.Session.ClearDrawOffer();

            if (request.Accept)
            {
                // Dừng game với kết quả hòa (WinnerSymbol = 0)
                var result = room.Session.Engine.CompleteAsDraw();
                await CompleteGameAsync(room.RoomId, result);
            }
            else
            {
                // Thông báo từ chối cho cả hai
                var resolvedDto = new DrawOfferResolvedDto
                {
                    RoomId = request.RoomId,
                    MatchIdentity = request.MatchIdentity,
                    OfferIdentity = request.OfferIdentity,
                    Accepted = false,
                    Cancelled = false,
                    Message = $"Người chơi {senderSession.PlayerId} đã từ chối hòa."
                };
                await _broadcaster.BroadcastToRoomAsync(room.RoomId, new NetworkMessage(MessageType.DrawOfferResolvedEvent, resolvedDto));
            }
        }

        private async Task HandleAvatarUpdateAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<AvatarUpdateRequest>(JsonOptions);
            if (request == null || string.IsNullOrWhiteSpace(request.Base64Image)) return;

            int newVersion;
            try
            {
                byte[] bytes = Convert.FromBase64String(request.Base64Image);
                if (bytes.Length > 262144) // 256KB max normalized limit
                {
                    await senderSession.SendMessageAsync(new NetworkMessage(MessageType.AvatarUpdateResponse, new AvatarUpdateResponse
                    {
                        Success = false,
                        Message = "Avatar vượt quá dung lượng cho phép."
                    }, message.RequestId));
                    return;
                }

                lock (senderSession.AvatarLock)
                {
                    senderSession.AvatarBytes = bytes;
                    senderSession.AvatarVersion++;
                    newVersion = senderSession.AvatarVersion;
                }

                // Gửi response thành công
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.AvatarUpdateResponse, new AvatarUpdateResponse
                {
                    Success = true,
                    AvatarVersion = newVersion
                }, message.RequestId));

                // Broadcast AvatarChangedEvent
                await BroadcastAvatarChangedAsync(senderSession.PlayerId, newVersion, true);
            }
            catch (Exception ex)
            {
                await senderSession.SendMessageAsync(new NetworkMessage(MessageType.AvatarUpdateResponse, new AvatarUpdateResponse
                {
                    Success = false,
                    Message = "Lỗi xử lý hình ảnh."
                }, message.RequestId));
                Console.WriteLine($"[Avatar] Error: {ex.Message}");
            }
        }

        private async Task HandleAvatarRemoveAsync(PlayerSession senderSession, NetworkMessage message)
        {
            int newVersion;
            lock (senderSession.AvatarLock)
            {
                senderSession.AvatarBytes = null;
                senderSession.AvatarVersion++;
                newVersion = senderSession.AvatarVersion;
            }

            await senderSession.SendMessageAsync(new NetworkMessage(MessageType.AvatarRemoveResponse, new AvatarRemoveResponse
            {
                Success = true,
                AvatarVersion = newVersion
            }, message.RequestId));

            await BroadcastAvatarChangedAsync(senderSession.PlayerId, newVersion, false);
        }

        private async Task HandleAvatarRequestAsync(PlayerSession senderSession, NetworkMessage message)
        {
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<AvatarRequest>(JsonOptions);
            if (request == null || string.IsNullOrWhiteSpace(request.PlayerId)) return;

            var targetSession = _sessionManager.GetSession(request.PlayerId);
            if (targetSession != null)
            {
                byte[]? bytes;
                int version;
                lock (targetSession.AvatarLock)
                {
                    bytes = targetSession.AvatarBytes;
                    version = targetSession.AvatarVersion;
                }

                if (bytes != null)
                {
                    var dataEvent = new AvatarDataEvent
                    {
                        PlayerId = targetSession.PlayerId,
                        AvatarVersion = version,
                        Base64Image = Convert.ToBase64String(bytes)
                    };
                    await senderSession.SendMessageAsync(new NetworkMessage(MessageType.AvatarDataEvent, dataEvent, message.RequestId));
                }
            }
        }

        private async Task BroadcastAvatarChangedAsync(string playerId, int version, bool hasAvatar)
        {
            var changeEvent = new AvatarChangedEvent
            {
                PlayerId = playerId,
                AvatarVersion = version,
                HasAvatar = hasAvatar
            };
            var msg = new NetworkMessage(MessageType.AvatarChangedEvent, changeEvent);

            // Gửi cho tất cả những người đang online (trong lobby hoặc trong room)
            var allNames = _lobbyManager.GetOnlinePlayerNames();
            foreach (var name in allNames)
            {
                var s = _sessionManager.GetSession(name);
                if (s != null)
                {
                    await s.SendMessageAsync(msg);
                }
            }
            
            // Xử lý gửi cho những người đang trong phòng (họ không còn ở lobby)
            // Lấy danh sách session từ roomManager
            var activeRooms = _roomManager.GetActiveRooms();
            foreach (var r in activeRooms)
            {
                var px = _sessionManager.GetSession(r.PlayerXId);
                var po = _sessionManager.GetSession(r.PlayerOId);
                if (px != null && !allNames.Contains(px.PlayerId)) await px.SendMessageAsync(msg);
                if (po != null && !allNames.Contains(po.PlayerId)) await po.SendMessageAsync(msg);
                
                foreach (var spec in r.GetSpectators())
                {
                    var ps = _sessionManager.GetSession(spec);
                    if (ps != null && !allNames.Contains(ps.PlayerId)) await ps.SendMessageAsync(msg);
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("[TcpServer] Server stopped.");
        }
    }
}
