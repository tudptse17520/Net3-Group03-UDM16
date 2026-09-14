namespace CaroShared.Contracts
{
    /// <summary>
    /// Phản hồi từ Server khi Client yêu cầu vào xem trận đấu.
    /// </summary>
    public record JoinSpectatorResponse
    {
        /// <summary>Kết quả: true nếu vào xem thành công.</summary>
        public bool Success { get; init; }

        /// <summary>Thông báo lỗi nếu không vào được (phòng không tồn tại, đã kết thúc, v.v.).</summary>
        public string? ErrorMessage { get; init; }

        /// <summary>Trạng thái bàn cờ hiện tại, chỉ có khi Success = true.</summary>
        public SpectatorStateSnapshotDto? Snapshot { get; init; }
    }
}
