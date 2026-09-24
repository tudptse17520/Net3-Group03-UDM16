namespace CaroShared.Contracts
{
    // Thông tin phòng chơi
    public record RoomDto
    {
        // ID phòng
        public string RoomId { get; init; } = string.Empty;

        // Thông tin người cầm X
        public PlayerInfoDto? PlayerX { get; init; }

        // Thông tin người cầm O
        public PlayerInfoDto? PlayerO { get; init; }

        // Số người xem
        public int SpectatorCount { get; init; }
        public bool IsSpectatorLocked { get; init; }
        public long Revision { get; init; }
        public List<PlayerInfoDto> Spectators { get; init; } = [];
        public override string ToString() => RoomCodes.Display(RoomId);
    }
}
