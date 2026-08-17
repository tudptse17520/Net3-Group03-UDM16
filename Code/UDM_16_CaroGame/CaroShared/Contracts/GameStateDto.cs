namespace CaroShared.Contracts
{
    // Gói toàn bộ dữ liệu game để gửi khi Reconnect
    public record GameStateDto
    {
        // Thông tin phòng
        public RoomDto? Room { get; init; }

        // Diễn biến ván cờ
        public GameSessionDto? Session { get; init; }
    }
}
