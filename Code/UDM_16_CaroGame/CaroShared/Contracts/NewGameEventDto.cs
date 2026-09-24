namespace CaroShared.Contracts
{
    // Thông báo từ Server khi ván mới được khởi tạo thành công trong phòng
    public record NewGameEventDto
    {
        // Mã phòng đang chơi
        public string RoomId { get; init; } = string.Empty;

        // Ký hiệu người đi trước ở ván mới (1 = X, 2 = O)
        public int StartingTurn { get; init; } = 1;

        // Thông điệp thông báo
        public string Message { get; init; } = "Ván đấu mới đã bắt đầu!";

        // Định danh ván đấu mới
        public System.Guid MatchIdentity { get; init; }
        public GameTimingDto? Timing { get; init; }
    }
}
