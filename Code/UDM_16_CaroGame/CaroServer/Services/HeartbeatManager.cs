using CaroShared.Constants;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroServer.Services
{
    public class HeartbeatManager
    {
        public static NetworkMessage CreatePing() => new() { Type = MessageType.Ping, Payload = "PING" };
        public static NetworkMessage CreatePong() => new() { Type = MessageType.Pong, Payload = "PONG" };

        public static bool IsHeartbeat(MessageType type) => type == MessageType.Ping || type == MessageType.Pong;

        // Kiểm tra xem Client có bị quá hạn phản hồi Ping không
        public static bool IsTimedOut(DateTime lastActiveTime)
        {
            return (DateTime.Now - lastActiveTime).TotalSeconds > (GameConstants.HeartbeatIntervalSeconds * 3);
        }
    }
}