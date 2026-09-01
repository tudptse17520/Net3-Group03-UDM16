using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using CaroServer.Game;
using CaroServer.Managers;
using CaroServer.Models;
using CaroShared.Constants;
using CaroShared.Enums;
using CaroShared.Protocol;
using CaroShared.Contracts;

namespace CaroServer.Core
{
    // Quản lý Server và các kết nối TCP
    public class TcpServerManager
    {
        private readonly TcpListener _listener;
        private readonly SessionManager _sessionManager;
        private readonly RoomManager _roomManager;
        private bool _isRunning;

        public TcpServerManager(SessionManager sessionManager, RoomManager roomManager)
        {
            _sessionManager = sessionManager;
            _roomManager = roomManager;
            // Lắng nghe kết nối trên port mặc định
            _listener = new TcpListener(IPAddress.Any, NetworkConstants.DefaultPort);
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
                        var message = JsonSerializer.Deserialize<NetworkMessage>(line);
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
                // Xóa session khi Client ngắt kết nối
                _sessionManager.RemoveSession(session.PlayerId);
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

            // Cập nhật lại ID theo Nickname
            _sessionManager.RemoveSession(session.PlayerId);
            session.PlayerId = nickname;
            _sessionManager.AddSession(session);

            // Gửi phản hồi thành công
            var responseMsg = new NetworkMessage(MessageType.LoginResponse, null, message.RequestId);
            await session.SendMessageAsync(responseMsg);
        }

        private async Task HandleChallengeAsync(PlayerSession senderSession, NetworkMessage message)
        {
            // Chuyển Payload thành ChallengeRequest
            var jsonElement = (JsonElement)message.Payload!;
            var request = jsonElement.Deserialize<ChallengeRequest>();
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
            var response = jsonElement.Deserialize<ChallengeResponse>();
            if (response == null) return;

            Console.WriteLine($"[ChallengeResponse] {senderSession.PlayerId} replied to {response.ChallengerId}: {(response.IsAccepted ? "Accept" : "Decline")}");

            var challengerSession = _sessionManager.GetSession(response.ChallengerId);
            if (challengerSession != null)
            {
                if (response.IsAccepted)
                {
                    // Tạo phòng bằng RoomManager của Dev 2
                    string roomId = _roomManager.CreateRoom(challengerSession.PlayerId, senderSession.PlayerId);
                    challengerSession.CurrentRoomId = roomId;
                    senderSession.CurrentRoomId = roomId;
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
            var moveRequest = jsonElement.Deserialize<MakeMoveRequest>();
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

            // Broadcast kết quả cho cả hai người chơi
            var session = _roomManager.GetSession(roomId);
            if (session != null)
            {
                var responseDto = new MoveMadeEventDto
                {
                    RoomId = roomId,
                    PlayerId = senderSession.PlayerId,
                    X = moveRequest.X,
                    Y = moveRequest.Y,
                    WinnerSymbol = result.WinnerSymbol,
                    IsValid = result.IsValid,
                    ErrorMessage = result.ErrorMessage
                };

                var resultMsg = new NetworkMessage(MessageType.MoveMadeEvent, responseDto);

                var playerX = _sessionManager.GetSession(session.PlayerXId);
                var playerO = _sessionManager.GetSession(session.PlayerOId);

                if (playerX != null) await playerX.SendMessageAsync(resultMsg);
                if (playerO != null) await playerO.SendMessageAsync(resultMsg);

                // Nếu game kết thúc, dọn phòng
                if (result.IsGameOver)
                {
                    if (playerX != null) playerX.CurrentRoomId = null;
                    if (playerO != null) playerO.CurrentRoomId = null;
                    _roomManager.RemoveRoom(roomId);
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
