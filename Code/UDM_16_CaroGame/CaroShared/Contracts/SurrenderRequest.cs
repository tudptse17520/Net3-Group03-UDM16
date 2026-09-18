namespace CaroShared.Contracts
{
    // Yêu cầu đầu hàng từ một người chơi trong trận đấu
    public record SurrenderRequest
    {
        // Mã phòng đang diễn ra trận đấu
        public string RoomId { get; init; } = string.Empty;

        // Nickname của người chơi chủ động đầu hàng
        public string PlayerId { get; init; } = string.Empty;
    }
}
