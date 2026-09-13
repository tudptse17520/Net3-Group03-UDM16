namespace CaroShared.Contracts
{
    public record MatchHistoryRequest
    {
        // Có thể mở rộng thêm PageNumber, PageSize sau này nếu cần thiết
        // Nếu PlayerId = null, Server sẽ tự động lấy lịch sử của người gửi Request
        public string? PlayerId { get; init; }
    }
}
