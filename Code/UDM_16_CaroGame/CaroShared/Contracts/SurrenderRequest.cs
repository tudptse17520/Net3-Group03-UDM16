namespace CaroShared.Contracts
{
    /// <summary>
    /// Yêu cầu đầu hàng từ một người chơi trong trận đấu.
    /// </summary>
    public record SurrenderRequest
    {
        /// <summary>Mã phòng đang diễn ra trận đấu.</summary>
        public string RoomId { get; init; } = string.Empty;

        /// <summary>Nickname của người chơi chủ động đầu hàng.</summary>
        public string PlayerId { get; init; } = string.Empty;
    }
}
