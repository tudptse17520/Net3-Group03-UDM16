namespace CaroShared.Constants
{
    public static class NetworkConstants
    {
        // Port mặc định của Server
        public const int DefaultPort = 8888;

        // Ký tự phân cách giữa các message
        public const string MessageDelimiter = "\n";

        // Heartbeat - Sprint 2 - Dev 5
        // Server gửi Ping mỗi 5 giây
        public const int HeartbeatIntervalSeconds = 5;

        // Nếu Client không phản hồi trong 15 giây
        // thì Server xem là mất kết nối
        public const int HeartbeatTimeoutSeconds = 15;
    }
}
