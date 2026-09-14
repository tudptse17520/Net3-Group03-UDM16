using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CaroShared.Constants;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroClient.Network
{
    /// <summary>
    /// Quản lý kết nối TCP phía Client (Singleton).
    /// Sử dụng MessageSerializer + MessageFrameDecoder từ CaroShared.
    /// </summary>
    public class NetworkClient : IDisposable
    {
        private static NetworkClient? _instance;
        public static NetworkClient Instance => _instance ??= new NetworkClient();

        // ── Connection ──
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private bool _isConnected;

        // ── Protocol (từ CaroShared) ──
        private readonly MessageSerializer _serializer = new();
        private readonly MessageFrameDecoder _decoder = new();

        // ── Thread safety ──
        private CancellationTokenSource? _cts;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        // ── Public properties ──
        public bool IsConnected => _isConnected && _tcpClient != null && _tcpClient.Connected;
        public string CurrentNickname { get; private set; } = string.Empty;

        // ── Events: Lobby / Login ──
        public event Action<bool, string>? OnConnectResult;
        public event Action<List<string>>? OnPlayerListReceived;

        // ── Events: Gameplay (Bước 2 plan) ──
        public event Action<MoveMadeEventDto>? OnMoveMade;
        public event Action<NetworkMessage>? OnGameOver;

        // ── Events: General ──
        public event Action<NetworkMessage>? OnMessageReceived;   // catch-all
        public event Action<Exception>? OnError;
        public event Action? OnDisconnected;

        private NetworkClient() { }

        // ────────────────────────────────────────────
        //  ConnectAsync
        // ────────────────────────────────────────────
        public async Task<bool> ConnectAsync(string ip, int port)
        {
            try
            {
                Disconnect();

                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);

                _stream = _tcpClient.GetStream();
                _isConnected = true;

                _cts = new CancellationTokenSource();
                _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                OnConnectResult?.Invoke(false,
                    $"Cannot connect to Server ({ip}:{port}): {ex.Message}");
                return false;
            }
        }

        // ────────────────────────────────────────────
        //  SendLoginAsync  (giữ nguyên logic cũ)
        // ────────────────────────────────────────────
        public async Task SendLoginAsync(string nickname)
        {
            CurrentNickname = nickname;
            var message = new NetworkMessage(MessageType.LoginRequest, nickname);
            await SendMessageAsync(message);
        }

        // ────────────────────────────────────────────
        //  SendMessageAsync  (dùng MessageSerializer + SemaphoreSlim)
        // ────────────────────────────────────────────
        public async Task SendMessageAsync(NetworkMessage message)
        {
            if (!IsConnected || _stream == null)
                throw new InvalidOperationException("Not connected to server.");

            string json = _serializer.Serialize(message);   // đã có "\n" ở cuối
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            await _sendLock.WaitAsync();
            try
            {
                await _stream.WriteAsync(bytes);
                await _stream.FlushAsync();
            }
            finally
            {
                _sendLock.Release();
            }
        }

        // ────────────────────────────────────────────
        //  ReceiveLoopAsync  (dùng MessageFrameDecoder)
        // ────────────────────────────────────────────
        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested && _stream != null)
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead == 0)
                    {
                        // Server đóng kết nối
                        _isConnected = false;
                        OnDisconnected?.Invoke();
                        break;
                    }

                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    IReadOnlyList<string> frames = _decoder.Decode(data);

                    foreach (string frame in frames)
                    {
                        DispatchMessage(frame);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Dừng loop bình thường khi Disconnect() gọi _cts.Cancel()
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
            }
        }

        // ────────────────────────────────────────────
        //  DispatchMessage  — phân phối message theo Type
        // ────────────────────────────────────────────
        private void DispatchMessage(string frame)
        {
            NetworkMessage msg = _serializer.Deserialize(frame);

            switch (msg.Type)
            {
                // ── Lobby / Login ──
                case MessageType.LoginResponse:
                    OnConnectResult?.Invoke(true, "Login successful!");
                    ParseAndNotifyPlayerList(msg);
                    break;

                case MessageType.PlayerListResponse:
                    ParseAndNotifyPlayerList(msg);
                    break;

                // ── Gameplay ──
                case MessageType.MoveMadeEvent:
                    var dto = _serializer.DeserializePayload<MoveMadeEventDto>(msg);
                    OnMoveMade?.Invoke(dto);
                    break;

                case MessageType.GameOverEvent:
                    OnGameOver?.Invoke(msg);
                    break;

                // ── Catch-all ──
                default:
                    OnMessageReceived?.Invoke(msg);
                    break;
            }
        }

        // ────────────────────────────────────────────
        //  ParseAndNotifyPlayerList  (giữ nguyên logic cũ)
        // ────────────────────────────────────────────
        private void ParseAndNotifyPlayerList(NetworkMessage message)
        {
            if (message.Payload is JsonElement element)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var response = element.Deserialize<PlayerListResponse>(options);
                if (response != null && response.PlayerNames != null)
                {
                    OnPlayerListReceived?.Invoke(response.PlayerNames);
                }
            }
        }

        // ────────────────────────────────────────────
        //  Disconnect + Dispose
        // ────────────────────────────────────────────
        public void Disconnect()
        {
            if (!_isConnected && _tcpClient == null) return;

            _isConnected = false;
            _cts?.Cancel();

            try { _stream?.Close(); } catch { }
            try { _tcpClient?.Close(); _tcpClient?.Dispose(); } catch { }

            _stream = null;
            _tcpClient = null;

            OnDisconnected?.Invoke();
        }

        public void Dispose()
        {
            Disconnect();
            _cts?.Dispose();
            _sendLock.Dispose();
        }
    }
}
