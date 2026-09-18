namespace CaroShared.Contracts
{
    /// <summary>
    /// Thông báo từ Server khi ván mới được khởi tạo thành công trong phòng.
    /// </summary>
    public record NewGameEventDto
    {
        /// <summary>Mã phòng đang chơi.</summary>
        public string RoomId { get; init; } = string.Empty;

        /// <summary>Ký hiệu người đi trước ở ván mới (1 = X, 2 = O).</summary>
        public int StartingTurn { get; init; } = 1;

        /// <summary>Thông điệp thông báo.</summary>
        public string Message { get; init; } = "Ván đấu mới đã bắt đầu!";
    }
}
