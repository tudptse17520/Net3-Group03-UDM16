namespace CaroShared.Contracts
{
    // Gói dữ liệu gửi cho khán giả lúc mới vào xem
    public record SpectatorStateSnapshotDto
    {
        // Thông tin phòng
        public RoomDto? Room { get; init; }

        // Diễn biến ván cờ
        public GameSessionDto? Session { get; init; }
    }
}
