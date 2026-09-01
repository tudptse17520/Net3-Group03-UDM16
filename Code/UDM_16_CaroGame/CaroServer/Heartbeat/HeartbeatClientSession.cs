namespace CaroServer.Heartbeat
{
    public sealed class HeartbeatClientSession
    {
        // ID của Client
        public string ClientId { get; }

        // Lần cuối Server nhận được phản hồi từ Client
        public DateTime LastSeenUtc { get; private set; }

        // Thời điểm Server gửi Ping gần nhất
        public DateTime? LastPingUtc { get; private set; }

        // Client còn được xem là kết nối hay không
        public bool IsConnected { get; private set; }

        public HeartbeatClientSession(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException(
                    "ClientId cannot be empty.",
                    nameof(clientId));
            }

            ClientId = clientId;

            // Khi Client vừa đăng ký,
            // coi như Client đang hoạt động
            LastSeenUtc = DateTime.UtcNow;

            IsConnected = true;
        }

        // Ghi nhận Server vừa gửi Ping
        public void MarkPingSent()
        {
            LastPingUtc = DateTime.UtcNow;
        }

        // Ghi nhận Client đã trả lời Pong
        public void MarkPongReceived()
        {
            LastSeenUtc = DateTime.UtcNow;

            LastPingUtc = null;
        }

        // Đánh dấu Client mất kết nối
        public void MarkDisconnected()
        {
            IsConnected = false;
        }

        // Kiểm tra Client có timeout hay chưa
        public bool HasTimedOut(
            TimeSpan timeout,
            DateTime utcNow)
        {
            if (!IsConnected)
            {
                return false;
            }

            return utcNow - LastSeenUtc >= timeout;
        }
    }
}
