using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
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
                case MessageType.ChallengeRequest:
                    await HandleChallengeAsync(senderSession, message);
                    break;
                case MessageType.ChallengeResponse:
                    await HandleChallengeResponseAsync(senderSession, message);
                    break;
                // Các message khác sẽ được xử lý sau
                default:
                    Console.WriteLine($"[TcpServer] Unhandled message type: {message.Type}");
                    break;
            }
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
                    // Đồng ý: tạo phòng chơi
                    var room = _roomManager.CreateRoom(challengerSession, senderSession);
                }

                // Gửi kết quả trả lời cho người gửi lời mời
                await challengerSession.SendMessageAsync(message);
            }
            else
            {
                Console.WriteLine($"[TcpServer] Challenger {response.ChallengerId} is offline.");
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
