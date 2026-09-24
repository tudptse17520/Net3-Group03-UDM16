using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
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
        private string _lastServerIp = "";
        private int _lastServerPort;

        public Task<bool> ReconnectToLastServerAsync() => ReconnectAsync(_lastServerIp, _lastServerPort);

        // ── Protocol (từ CaroShared) ──
        private readonly MessageSerializer _serializer = new();

        // ── Thread safety ──
        private CancellationTokenSource? _cts;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        // ── Public properties ──
        public bool IsConnected => _isConnected && _tcpClient != null && _tcpClient.Connected;
        public string ConnectedEndpoint => IsConnected ? $"{_lastServerIp}:{_lastServerPort}" : "";
        public string LastConnectionError { get; private set; } = "";
        public string CurrentNickname { get; private set; } = string.Empty;
        public string SessionToken { get; private set; } = string.Empty;
        public List<PlayerInfoDto> PlayerList { get; private set; } = new List<PlayerInfoDto>();

        // ── Events: Lobby / Login ──
        public event Action<bool, string>? OnConnectResult;
        public event Action<bool, ReconnectResponse>? OnReconnectResult;
        public event Action<GameStateDto>? OnGameStateRestored;
        public event Action<List<PlayerInfoDto>>? OnPlayerListReceived;
        public event Action<ChallengeRequest>? OnChallengeReceived;
        public event Action<ChallengeResponse>? OnChallengeResponseReceived;
        public event Action<List<MatchDto>>? OnMatchHistoryReceived;
        public event Action? OnDisconnected;
        public event Action<JoinSpectatorResponse>? OnSpectatorJoined;
        public event Action<List<RoomDto>>? OnRoomListReceived;
        public event Action<MoveMadeEventDto>? OnMoveMade;
        public event Action<NetworkMessage>? OnGameOver;
        public event Action<DrawOfferEventDto>? OnDrawOfferReceived;
        public event Action<DrawOfferResolvedDto>? OnDrawOfferResolved;
        public event Action<NetworkMessage>? OnMessageReceived;
        public event Action<Exception>? OnError;

        // ── Events: Avatar ──
        public event Action<AvatarUpdateResponse>? OnAvatarUpdateResponse;
        public event Action<AvatarRemoveResponse>? OnAvatarRemoveResponse;
        public event Action<AvatarDataEvent>? OnAvatarDataReceived;
        public event Action<AvatarChangedEvent>? OnAvatarChanged;

        private NetworkClient() { }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        // ────────────────────────────────────────────
        //  ConnectAsync
        // ────────────────────────────────────────────
        public async Task<bool> ConnectAsync(string ip, int port, CancellationToken cancellationToken = default)
        {
            try
            {
                Disconnect();

                _lastServerIp = ip;
                _lastServerPort = port;
                new ServerConfiguration { Host = ip, Port = port }.Validate();
                _cts?.Dispose();
                using var connectTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                connectTimeout.CancelAfter(TimeSpan.FromSeconds(10));
                ClientLog.Write($"Resolving {ip}:{port}");
                var addresses = await Dns.GetHostAddressesAsync(ip.Trim('[', ']'), connectTimeout.Token);
                ClientLog.Write($"Resolved {ip}: {string.Join(", ", addresses.Select(x => x.ToString()))}");
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(addresses, port, connectTimeout.Token);
                LastConnectionError = "";
                ClientLog.Write($"TCP connected to {ip}:{port}");

                _stream = _tcpClient.GetStream();
                _isConnected = true;

                _cts = new CancellationTokenSource();
                var connectionStream = _stream;
                var connectionToken = _cts.Token;
                _ = Task.Run(() => ReceiveLoopAsync(connectionStream, connectionToken));

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _tcpClient?.Dispose();
                _tcpClient = null;
                LastConnectionError = ConnectionDiagnostics.Explain(ex, $"{ip}:{port}");
                ClientLog.Write($"Connect failed: {ex.GetType().Name}: {ex.Message}");
                OnConnectResult?.Invoke(false, LastConnectionError);
                return false;
            }
        }

        // ────────────────────────────────────────────
        //  SendLoginAsync  (giữ nguyên logic cũ)
        // ────────────────────────────────────────────
        public async Task SendLoginAsync(string nickname)
        {
            SessionToken = "";
            PlayerList = new();
            CurrentNickname = nickname;
            var message = new NetworkMessage(MessageType.LoginRequest, nickname);
            await SendMessageAsync(message);
        }

        // ────────────────────────────────────────────
        //  Challenge
        // ────────────────────────────────────────────
        public async Task SendChallengeRequestAsync(string targetPlayerId)
        {
            var message = new NetworkMessage(MessageType.ChallengeRequest, new ChallengeRequest { TargetPlayerId = targetPlayerId });
            await SendMessageAsync(message);
        }

        public async Task SendChallengeResponseAsync(string challengerId, bool isAccepted)
        {
            var message = new NetworkMessage(MessageType.ChallengeResponse, new ChallengeResponse { ChallengerId = challengerId, IsAccepted = isAccepted });
            await SendMessageAsync(message);
        }

        // ────────────────────────────────────────────
        //  ReconnectAsync (dùng SessionToken)
        // ────────────────────────────────────────────
        public async Task<bool> ReconnectAsync(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(SessionToken))
            {
                OnReconnectResult?.Invoke(false, new ReconnectResponse
                {
                    Success = false,
                    Message = "Chưa có SessionToken để reconnect."
                });
                return false;
            }

            bool connected = await ConnectAsync(ip, port);
            if (!connected)
            {
                return false;
            }

            var request = new ReconnectRequest { SessionToken = SessionToken };
            await SendMessageAsync(new NetworkMessage(MessageType.ReconnectRequest, request));
            return true;
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
        private async Task ReceiveLoopAsync(NetworkStream stream, CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            char[] chars = new char[Encoding.UTF8.GetMaxCharCount(4096)];
            var utf8 = Encoding.UTF8.GetDecoder();
            var decoder = new MessageFrameDecoder();
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead == 0)
                    {
                        // Server đóng kết nối
                        if (ReferenceEquals(stream, _stream))
                        {
                            _isConnected = false;
                            OnDisconnected?.Invoke();
                        }
                        break;
                    }

                    int charCount = utf8.GetChars(buffer, 0, bytesRead, chars, 0, false);
                    string data = new(chars, 0, charCount);
                    IReadOnlyList<string> frames = decoder.Decode(data);

                    foreach (string frame in frames)
                    {
                        try
                        {
                            if (token.IsCancellationRequested || !ReferenceEquals(stream, _stream)) return;
                            DispatchMessage(frame);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[NetworkClient] Lỗi khi xử lý message: {ex.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Dừng loop bình thường khi Disconnect() gọi _cts.Cancel()
            }
            catch (Exception ex)
            {
                if (token.IsCancellationRequested || !ReferenceEquals(stream, _stream)) return;
                _isConnected = false;
                OnDisconnected?.Invoke();
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
                    ParseLoginResponse(msg);
                    ParseAndNotifyPlayerList(msg);
                    ClientLog.Write($"Login accepted; snapshot={PlayerList.Count}");
                    OnConnectResult?.Invoke(true, "Đăng nhập thành công.");
                    break;

                case MessageType.ReconnectResponse:
                    ParseReconnectResponse(msg);
                    break;

                case MessageType.Ping:
                    _ = SendMessageAsync(HeartbeatProtocol.CreatePong(msg));
                    break;

                case MessageType.PlayerListResponse:
                    ParseAndNotifyPlayerList(msg);
                    break;
                    
                case MessageType.JoinSpectatorResponse:
                    if (msg.Payload is JsonElement specRespElement)
                    {
                        var specResp = specRespElement.Deserialize<JoinSpectatorResponse>(JsonOptions);
                        if (specResp != null)
                        {
                            OnSpectatorJoined?.Invoke(specResp);
                        }
                    }
                    break;
                    
                case MessageType.AvatarUpdateResponse:
                    if (msg.Payload is JsonElement aurElement)
                    {
                        var aur = aurElement.Deserialize<AvatarUpdateResponse>(JsonOptions);
                        if (aur != null) OnAvatarUpdateResponse?.Invoke(aur);
                    }
                    break;
                case MessageType.AvatarRemoveResponse:
                    if (msg.Payload is JsonElement arrElement)
                    {
                        var arr = arrElement.Deserialize<AvatarRemoveResponse>(JsonOptions);
                        if (arr != null) OnAvatarRemoveResponse?.Invoke(arr);
                    }
                    break;
                case MessageType.AvatarDataEvent:
                    if (msg.Payload is JsonElement adeElement)
                    {
                        var ade = adeElement.Deserialize<AvatarDataEvent>(JsonOptions);
                        if (ade != null) OnAvatarDataReceived?.Invoke(ade);
                    }
                    break;
                case MessageType.AvatarChangedEvent:
                    if (msg.Payload is JsonElement aceElement)
                    {
                        var ace = aceElement.Deserialize<AvatarChangedEvent>(JsonOptions);
                        if (ace != null) OnAvatarChanged?.Invoke(ace);
                    }
                    break;

                case MessageType.MatchHistoryResponse:
                    ParseAndNotifyMatchHistory(msg);
                    break;

                case MessageType.RoomListResponse:
                    if (msg.Payload is JsonElement roomListElement)
                    {
                        var roomListResp = roomListElement.Deserialize<RoomListResponse>(JsonOptions);
                        if (roomListResp != null)
                            OnRoomListReceived?.Invoke(roomListResp.Rooms);
                    }
                    break;

                case MessageType.ChallengeRequest:
                    if (msg.Payload is JsonElement challengeReqElement)
                    {
                        var req = challengeReqElement.Deserialize<ChallengeRequest>(JsonOptions);
                        if (req != null)
                        {
                            OnChallengeReceived?.Invoke(req);
                        }
                    }
                    break;

                case MessageType.ChallengeResponse:
                    if (msg.Payload is JsonElement challengeRespElement)
                    {
                        var resp = challengeRespElement.Deserialize<ChallengeResponse>(JsonOptions);
                        if (resp != null)
                        {
                            OnChallengeResponseReceived?.Invoke(resp);
                        }
                    }
                    break;

                // ── Gameplay ──
                case MessageType.MoveMadeEvent:
                    var dto = _serializer.DeserializePayload<MoveMadeEventDto>(msg);
                    OnMoveMade?.Invoke(dto);
                    break;

                case MessageType.GameOverEvent:
                    OnGameOver?.Invoke(msg);
                    break;

                case MessageType.DrawOfferEvent:
                    if (msg.Payload is JsonElement drawEventElement)
                    {
                        var drawEvent = drawEventElement.Deserialize<DrawOfferEventDto>(JsonOptions);
                        if (drawEvent != null)
                        {
                            OnDrawOfferReceived?.Invoke(drawEvent);
                        }
                    }
                    break;

                case MessageType.DrawOfferResolvedEvent:
                    if (msg.Payload is JsonElement resolvedElement)
                    {
                        var resolvedEvent = resolvedElement.Deserialize<DrawOfferResolvedDto>(JsonOptions);
                        if (resolvedEvent != null)
                        {
                            OnDrawOfferResolved?.Invoke(resolvedEvent);
                        }
                    }
                    break;

                // ── Catch-all ──
                case MessageType.ErrorResponse:
                    if (msg.Payload is JsonElement errorElement)
                    {
                        var error = errorElement.Deserialize<ErrorResponse>(JsonOptions);
                        if (error != null) OnError?.Invoke(new ServerResponseException(error.Code, error.Message));
                    }
                    break;
                default:
                    Console.WriteLine($"[NetworkClient] Unhandled message type: {msg.Type}");
                    OnMessageReceived?.Invoke(msg);
                    break;
            }
        }

        // ────────────────────────────────────────────
        //  Helper Methods (Parsing)
        // ────────────────────────────────────────────
        private void ParseLoginResponse(NetworkMessage message)
        {
            if (message.Payload is JsonElement element)
            {
                var response = element.Deserialize<PlayerListResponse>(JsonOptions);
                if (!string.IsNullOrWhiteSpace(response?.SessionToken))
                {
                    SessionToken = response.SessionToken;
                }
            }
        }

        private void ParseReconnectResponse(NetworkMessage message)
        {
            if (message.Payload is not JsonElement element) return;

            var response = element.Deserialize<ReconnectResponse>(JsonOptions);
            if (response == null) return;

            OnReconnectResult?.Invoke(response.Success, response);

            if (response.Success && response.GameState != null)
            {
                OnGameStateRestored?.Invoke(response.GameState);
            }
        }

        // Parse Payload thành danh sách tên người chơi
        private void ParseAndNotifyPlayerList(NetworkMessage message)
        {
            if (message.Payload is JsonElement element)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var response = element.Deserialize<PlayerListResponse>(options);
                if (response != null && response.Players != null)
                {
                    ClientLog.Write($"Player snapshot: {response.Players.Count}");
                    PlayerList = response.Players;
                    OnPlayerListReceived?.Invoke(response.Players);
                }
            }
        }

        // Parse Payload thành danh sách lịch sử đấu
        private void ParseAndNotifyMatchHistory(NetworkMessage message)
        {
            if (message.Payload is JsonElement element)
            {
                var response = element.Deserialize<MatchHistoryResponse>(JsonOptions);
                if (response != null && response.Matches != null)
                {
                    OnMatchHistoryReceived?.Invoke(response.Matches);
                }
            }
        }

        // ── Avatar Methods ──
        public Task SendAvatarUpdateAsync(string base64Image)
        {
            var request = new AvatarUpdateRequest { Base64Image = base64Image };
            return SendMessageAsync(new NetworkMessage(MessageType.AvatarUpdateRequest, request));
        }

        public Task SendAvatarRemoveAsync()
        {
            return SendMessageAsync(new NetworkMessage(MessageType.AvatarRemoveRequest, new AvatarRemoveRequest()));
        }

        public Task SendAvatarRequestAsync(string targetPlayerId)
        {
            var request = new AvatarRequest { PlayerId = targetPlayerId };
            return SendMessageAsync(new NetworkMessage(MessageType.AvatarRequest, request));
        }

        // ────────────────────────────────────────────
        //  Disconnect + Dispose
        // ────────────────────────────────────────────
        public void Disconnect()
        {
            if (!_isConnected && _tcpClient == null) return;

            ClientLog.Write($"Disconnected from {_lastServerIp}:{_lastServerPort}");
            _isConnected = false;
            PlayerList = new();
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
