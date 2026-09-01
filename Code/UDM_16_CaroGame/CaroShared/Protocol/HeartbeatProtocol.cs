using CaroShared.Enums;

namespace CaroShared.Protocol
{
    public static class HeartbeatProtocol
    {
        // Tạo Ping message
        public static NetworkMessage CreatePing()
        {
            return new NetworkMessage(
                MessageType.Ping,
                new
                {
                    SentAtUtc = DateTime.UtcNow
                });
        }

        // Tạo Pong message để trả lời Ping
        public static NetworkMessage CreatePong(
            NetworkMessage pingMessage)
        {
            if (pingMessage == null)
            {
                throw new ArgumentNullException(
                    nameof(pingMessage));
            }

            return new NetworkMessage(
                MessageType.Pong,
                new
                {
                    ReceivedAtUtc = DateTime.UtcNow
                },
                pingMessage.RequestId);
        }
    }
}
