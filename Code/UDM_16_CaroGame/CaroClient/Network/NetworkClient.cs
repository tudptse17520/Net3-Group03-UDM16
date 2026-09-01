using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CaroShared.Constants;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroClient.Network
{
    // Quản lý kết nối TCP phía Client (Singleton)
    public class NetworkClient
    {
        private static NetworkClient? _instance;
        public static NetworkClient Instance => _instance ??= new NetworkClient();

        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamReader? _reader;
        private StreamWriter? _writer;
        private bool _isConnected;

        public bool IsConnected => _isConnected && _client != null && _client.Connected;
        public string CurrentNickname { get; private set; } = string.Empty;

        // Events để UI lắng nghe
        public event Action<bool, string>? OnConnectResult;
        public event Action<List<string>>? OnPlayerListReceived;
        public event Action? OnDisconnected;

        private NetworkClient() { }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        // Kết nối tới Server
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                Disconnect();

                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);

                _stream = _client.GetStream();
                _reader = new StreamReader(_stream, Encoding.UTF8);
                _writer = new StreamWriter(_stream, Encoding.UTF8) { AutoFlush = true };
                _isConnected = true;

                // Bắt đầu đọc dữ liệu từ Server
                _ = ReadLoopAsync();

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectResult?.Invoke(false, $"Cannot connect to Server ({ip}:{port}): {ex.Message}");
                return false;
            }
        }

        // Gửi Nickname đăng nhập lên Server
        public async Task SendLoginAsync(string nickname)
        {
            CurrentNickname = nickname;
            var message = new NetworkMessage(MessageType.LoginRequest, nickname);
            await SendMessageAsync(message);
        }

        // Gửi NetworkMessage lên Server
        public async Task SendMessageAsync(NetworkMessage message)
        {
            if (!IsConnected || _writer == null) return;

            try
            {
                string json = JsonSerializer.Serialize(message, JsonOptions) + NetworkConstants.MessageDelimiter;
                await _writer.WriteAsync(json);
                await _writer.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkClient] Error sending data: {ex.Message}");
                Disconnect();
            }
        }

        // Vòng lặp đọc dữ liệu từ Server
        private async Task ReadLoopAsync()
        {
            try
            {
                while (_isConnected && _reader != null)
                {
                    string? jsonLine = await _reader.ReadLineAsync();
                    if (jsonLine == null) break;

                    if (string.IsNullOrWhiteSpace(jsonLine)) continue;

                    var message = JsonSerializer.Deserialize<NetworkMessage>(jsonLine, JsonOptions);
                    if (message != null)
                    {
                        HandleIncomingMessage(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkClient] Error reading stream: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        // Xử lý message nhận từ Server
        private void HandleIncomingMessage(NetworkMessage message)
        {
            switch (message.Type)
            {
                case MessageType.LoginResponse:
                    OnConnectResult?.Invoke(true, "Login successful!");
                    ParseAndNotifyPlayerList(message);
                    break;

                case MessageType.PlayerListResponse:
                    ParseAndNotifyPlayerList(message);
                    break;

                default:
                    Console.WriteLine($"[NetworkClient] Unhandled message type: {message.Type}");
                    break;
            }
        }

        // Parse Payload thành danh sách tên người chơi
        private void ParseAndNotifyPlayerList(NetworkMessage message)
        {
            if (message.Payload is JsonElement element)
            {
                var response = element.Deserialize<PlayerListResponse>(JsonOptions);
                if (response != null && response.PlayerNames != null)
                {
                    OnPlayerListReceived?.Invoke(response.PlayerNames);
                }
            }
        }

        // Ngắt kết nối
        public void Disconnect()
        {
            if (!_isConnected && _client == null) return;

            _isConnected = false;
            try { _reader?.Dispose(); } catch { }
            try { _writer?.Dispose(); } catch { }
            try { _stream?.Dispose(); } catch { }
            try { _client?.Close(); _client?.Dispose(); } catch { }

            _reader = null;
            _writer = null;
            _stream = null;
            _client = null;

            OnDisconnected?.Invoke();
        }
    }
}
