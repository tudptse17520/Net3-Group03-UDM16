namespace CaroShared.Contracts
{
    /// <summary>
    /// Yêu cầu chơi ván mới từ một người chơi trong cùng một phòng đấu.
    /// </summary>
    public record NewGameRequest
    {
        /// <summary>Mã phòng muốn bắt đầu ván mới.</summary>
        public string RoomId { get; init; } = string.Empty;

        /// <summary>Nickname của người chơi gửi yêu cầu.</summary>
        public string PlayerId { get; init; } = string.Empty;
    }
}
