using CaroShared.Protocol;

namespace CaroServer.Heartbeat
{
    public sealed class HeartbeatManager : IAsyncDisposable
    {
        private readonly Dictionary<
            string,
            HeartbeatClientSession> _sessions = new();

        private readonly object _lock = new();

        private readonly TimeSpan _pingInterval;

        private readonly TimeSpan _timeout;

        // Hàm gửi message cho Client
        private readonly Func<
            string,
            NetworkMessage,
            Task> _sendMessageAsync;

        // Hàm ngắt kết nối Client
        private readonly Func<
            string,
            Task> _disconnectAsync;

        private CancellationTokenSource? _cts;

        private Task? _heartbeatTask;

        public HeartbeatManager(
            TimeSpan pingInterval,
            TimeSpan timeout,
            Func<string, NetworkMessage, Task> sendMessageAsync,
            Func<string, Task> disconnectAsync)
        {
            if (pingInterval <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pingInterval));
            }

            if (timeout <= pingInterval)
            {
                throw new ArgumentException(
                    "Timeout must be greater than ping interval.",
                    nameof(timeout));
            }

            _pingInterval = pingInterval;

            _timeout = timeout;

            _sendMessageAsync =
                sendMessageAsync
                ?? throw new ArgumentNullException(
                    nameof(sendMessageAsync));

            _disconnectAsync =
                disconnectAsync
                ?? throw new ArgumentNullException(
                    nameof(disconnectAsync));
        }

        // Danh sách các Client đang được heartbeat
        public IReadOnlyCollection<HeartbeatClientSession>
            Sessions
        {
            get
            {
                lock (_lock)
                {
                    return _sessions
                        .Values
                        .ToList()
                        .AsReadOnly();
                }
            }
        }

        // Đăng ký Client mới
        public void RegisterClient(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException(
                    "ClientId cannot be empty.",
                    nameof(clientId));
            }

            lock (_lock)
            {
                _sessions[clientId] =
                    new HeartbeatClientSession(clientId);
            }
        }

        // Gọi khi Server nhận Pong từ Client
        public void ReceivePong(string clientId)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(
                    clientId,
                    out var session))
                {
                    session.MarkPongReceived();
                }
            }
        }

        // Gọi khi Client ngắt kết nối bình thường
        public void MarkDisconnected(string clientId)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(
                    clientId,
                    out var session))
                {
                    session.MarkDisconnected();

                    _sessions.Remove(clientId);
                }
            }
        }

        // Lấy LastSeen của Client
        public bool TryGetLastSeen(
            string clientId,
            out DateTime lastSeenUtc)
        {
            lock (_lock)
            {
                if (_sessions.TryGetValue(
                    clientId,
                    out var session))
                {
                    lastSeenUtc =
                        session.LastSeenUtc;

                    return true;
                }
            }

            lastSeenUtc = default;

            return false;
        }

        // Bắt đầu heartbeat
        public void Start(
            CancellationToken cancellationToken = default)
        {
            if (_heartbeatTask != null)
            {
                return;
            }

            _cts =
                CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

            _heartbeatTask =
                RunAsync(_cts.Token);
        }

        // Vòng lặp heartbeat
        private async Task RunAsync(
            CancellationToken cancellationToken)
        {
            using PeriodicTimer timer =
                new(_pingInterval);

            while (
                await timer.WaitForNextTickAsync(
                    cancellationToken))
            {
                await CheckClientsAsync(
                    cancellationToken);
            }
        }

        // Kiểm tra toàn bộ Client
        private async Task CheckClientsAsync(
            CancellationToken cancellationToken)
        {
            List<HeartbeatClientSession> sessions;

            lock (_lock)
            {
                sessions =
                    _sessions
                    .Values
                    .ToList();
            }

            DateTime now =
                DateTime.UtcNow;

            foreach (var session in sessions)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                // Client timeout
                if (session.HasTimedOut(
                    _timeout,
                    now))
                {
                    session.MarkDisconnected();

                    await _disconnectAsync(
                        session.ClientId);

                    lock (_lock)
                    {
                        _sessions.Remove(
                            session.ClientId);
                    }

                    continue;
                }

                // Gửi Ping
                session.MarkPingSent();

                NetworkMessage ping =
                    HeartbeatProtocol.CreatePing();

                await _sendMessageAsync(
                    session.ClientId,
                    ping);
            }
        }

        // Dừng heartbeat
        public async ValueTask DisposeAsync()
        {
            if (_cts == null)
            {
                return;
            }

            await _cts.CancelAsync();

            if (_heartbeatTask != null)
            {
                try
                {
                    await _heartbeatTask;
                }
                catch (OperationCanceledException)
                {
                    // Dừng heartbeat bình thường
                }
            }

            _cts.Dispose();

            _cts = null;

            _heartbeatTask = null;
        }
    }
}
