namespace CaroShared.Contracts
{
    // Yêu cầu chơi ván mới từ một người chơi trong cùng một phòng đấu
    public record NewGameRequest
    {
        // Mã phòng muốn bắt đầu ván mới
        public string RoomId { get; init; } = string.Empty;

        // Nickname của người chơi gửi yêu cầu
        public string PlayerId { get; init; } = string.Empty;
    }
}
