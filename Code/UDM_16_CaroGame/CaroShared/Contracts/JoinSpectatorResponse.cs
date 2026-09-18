namespace CaroShared.Contracts
{
    // Phản hồi từ Server khi Client yêu cầu vào xem trận đấu
    // Hỗ trợ cả IsSuccess và Success để tương thích ngược 100% với các nhánh Client
    public record JoinSpectatorResponse
    {
        // Kết quả: true nếu vào xem thành công
        public bool IsSuccess { get; init; }

        // Thuộc tính alias cho IsSuccess nhằm đảm bảo tương thích
        public bool Success
        {
            get => IsSuccess;
            init => IsSuccess = value;
        }

        // Thông báo lỗi nếu không vào được (phòng không tồn tại, đã kết thúc, v.v.)
        public string? ErrorMessage { get; init; }

        // Trạng thái bàn cờ hiện tại, chỉ có khi vào xem thành công
        public SpectatorStateSnapshotDto? Snapshot { get; init; }
    }
}
