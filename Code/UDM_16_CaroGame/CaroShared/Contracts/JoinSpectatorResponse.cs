namespace CaroShared.Contracts
{
    /// <summary>
    /// Phản hồi từ Server khi Client yêu cầu vào xem trận đấu.
    /// Hỗ trợ cả IsSuccess và Success để tương thích ngược 100% với các nhánh Client.
    /// </summary>
    public record JoinSpectatorResponse
    {
        /// <summary>Kết quả: true nếu vào xem thành công.</summary>
        public bool IsSuccess { get; init; }

        /// <summary>Thuộc tính alias cho IsSuccess nhằm đảm bảo tương thích.</summary>
        public bool Success
        {
            get => IsSuccess;
            init => IsSuccess = value;
        }

        /// <summary>Thông báo lỗi nếu không vào được (phòng không tồn tại, đã kết thúc, v.v.).</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Trạng thái bàn cờ hiện tại, chỉ có khi vào xem thành công.</summary>
        public SpectatorStateSnapshotDto? Snapshot { get; init; }
    }
}
