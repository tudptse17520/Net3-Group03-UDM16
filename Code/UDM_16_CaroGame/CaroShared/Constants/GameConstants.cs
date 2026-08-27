namespace CaroShared.Constants
{
    // Cấu hình cơ bản của game Caro
    public static class GameConstants
    {
        // Bàn cờ 15x15
        public const int BoardSize = 15;

        // Mỗi lượt đánh tối đa 30 giây
        public const int TurnTimeoutSeconds = 30;

        // Chờ tối đa 60s khi rớt mạng trước khi xử thua
        public const int ReconnectWindowSeconds = 60;

        // Khoảng thời gian Ping/Pong ngầm (5s)
        public const int HeartbeatIntervalSeconds = 5;
    }
}
