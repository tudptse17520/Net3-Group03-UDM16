namespace CaroShared.Contracts
{
    // Thông tin phòng chơi
    public record RoomDto
    {
        // ID phòng
        public string RoomId { get; init; } = string.Empty;

        // Tên người cầm X
        public string? PlayerX { get; init; }

        // Tên người cầm O
        public string? PlayerO { get; init; }

        // Số người xem
        public int SpectatorCount { get; init; }
    }
}
