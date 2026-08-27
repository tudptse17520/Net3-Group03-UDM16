using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CaroServer.Managers;
using CaroServer.Models;
using CaroShared.Constants;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroServer.Core
{
    // Quản lý Server và các kết nối TCP
    public class TcpServerManager
    {
        private readonly TcpListener _listener;
        private readonly SessionManager _sessionManager;
        private readonly LobbyManager _lobbyManager;
        private bool _isRunning;

        public TcpServerManager(SessionManager sessionManager, LobbyManager lobbyManager)
        {
            _sessionManager = sessionManager;
            _lobbyManager = lobbyManager;
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

                    // Khởi tạo session và lưu vào Manager
                    var session = new PlayerSession(client);
                    _sessionManager.AddSession(session);

                    // Bắt đầu luồng lắng nghe dữ liệu từ Client này
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

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        // Vòng lặp nhận dữ liệu bất đồng bộ từ từng session
        private async Task ReceiveDataAsync(PlayerSession session)
        {
            try
            {
                using var reader = new StreamReader(session.Stream, Encoding.UTF8, leaveOpen: true);
                while (_isRunning && session.Client.Connected)
                {
                    string? jsonLine = await reader.ReadLineAsync();
                    if (jsonLine == null) break; // Client đã ngắt kết nối

                    if (string.IsNullOrWhiteSpace(jsonLine)) continue;

                    var message = JsonSerializer.Deserialize<NetworkMessage>(jsonLine, JsonOptions);
                    if (message != null)
                    {
                        await ProcessMessageAsync(session, message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TcpServer] Connection error with {session.PlayerId}: {ex.Message}");
            }
            finally
            {
                // Dọn dẹp khi Client ngắt kết nối
                Console.WriteLine($"[TcpServer] Client disconnected: {session.PlayerId} ({session.Nickname})");
                if (!string.IsNullOrEmpty(session.PlayerId))
                {
                    _lobbyManager.RemovePlayer(session.PlayerId);
                    _sessionManager.RemoveSession(session.PlayerId);
                    await BroadcastPlayerListAsync();
                }
            }
        }

        // Xử lý thông điệp từ Client
        private async Task ProcessMessageAsync(PlayerSession session, NetworkMessage message)
        {
            Console.WriteLine($"[TcpServer] Received {message.Type} from {session.PlayerId}");

            switch (message.Type)
            {
                case MessageType.LoginRequest:
                    string nickname = ExtractNickname(message.Payload);
                    if (string.IsNullOrWhiteSpace(nickname))
                    {
                        nickname = session.PlayerId;
                    }

                    session.Nickname = nickname;
                    _lobbyManager.AddPlayer(session.PlayerId, nickname);

                    Console.WriteLine($"[TcpServer] Player logged in: Nickname={nickname}, PlayerId={session.PlayerId}");

                    // Trả lời LoginResponse cho Client kết nối
                    var responsePayload = new PlayerListResponse { PlayerNames = _lobbyManager.GetOnlinePlayerNames() };
                    var responseMsg = new NetworkMessage(MessageType.LoginResponse, responsePayload, message.RequestId);
                    await session.SendAsync(responseMsg);

                    // Broadcast danh sách người chơi online mới cho TẤT CẢ các client
                    await BroadcastPlayerListAsync();
                    break;

                case MessageType.GetPlayerListRequest:
                    var listResponse = new PlayerListResponse { PlayerNames = _lobbyManager.GetOnlinePlayerNames() };
                    var listMsg = new NetworkMessage(MessageType.PlayerListResponse, listResponse, message.RequestId);
                    await session.SendAsync(listMsg);
                    break;

                case MessageType.LogoutRequest:
                    Console.WriteLine($"[TcpServer] Player logged out: Nickname={session.Nickname}, PlayerId={session.PlayerId}");
                    _lobbyManager.RemovePlayer(session.PlayerId);
                    _sessionManager.RemoveSession(session.PlayerId);
                    await BroadcastPlayerListAsync();
                    break;

                case MessageType.Ping:
                    var pongMsg = new NetworkMessage(MessageType.Pong, null, message.RequestId);
                    await session.SendAsync(pongMsg);
                    break;

                default:
                    Console.WriteLine($"[TcpServer] Unhandled message type: {message.Type}");
                    break;
            }
        }

        // Trích xuất Nickname từ Payload linh hoạt (chuỗi hoặc PlayerDto hoặc JsonElement)
        private string ExtractNickname(object? payload)
        {
            if (payload == null) return string.Empty;
            if (payload is string str) return str;
            if (payload is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.String) return elem.GetString() ?? string.Empty;
                if (elem.ValueKind == JsonValueKind.Object)
                {
                    if (elem.TryGetProperty("Nickname", out var nickProp) && nickProp.ValueKind == JsonValueKind.String)
                        return nickProp.GetString() ?? string.Empty;
                    if (elem.TryGetProperty("Username", out var userProp) && userProp.ValueKind == JsonValueKind.String)
                        return userProp.GetString() ?? string.Empty;
                }
            }
            return payload.ToString() ?? string.Empty;
        }

        // Broadcast danh sách người chơi online tới tất cả Client
        private async Task BroadcastPlayerListAsync()
        {
            var onlineList = _lobbyManager.GetOnlinePlayerNames();
            Console.WriteLine($"[TcpServer] Broadcasting player list ({onlineList.Count} online): [{string.Join(", ", onlineList)}]");
            var broadcastMsg = new NetworkMessage(MessageType.PlayerListResponse, new PlayerListResponse { PlayerNames = onlineList });

            foreach (var s in _sessionManager.GetAllSessions())
            {
                await s.SendAsync(broadcastMsg);
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
