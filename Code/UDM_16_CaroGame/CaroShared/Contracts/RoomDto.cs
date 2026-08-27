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

        // Tên người chơi 1 & 2 (Alias tương thích cho các Manager)
        public string? Player1 { get => PlayerX; init => PlayerX = value; }
        public string? Player2 { get => PlayerO; init => PlayerO = value; }

        // Số người xem
        public int SpectatorCount { get; init; }
    }
}
