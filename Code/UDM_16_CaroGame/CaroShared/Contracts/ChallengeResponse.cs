namespace CaroShared.Contracts
{
    // Dữ liệu phản hồi lời mời thách đấu
    public record ChallengeResponse
    {
        // ID của người gửi lời mời
        public string ChallengerId { get; init; } = string.Empty;

        // Đồng ý hay từ chối
        public bool IsAccepted { get; init; }

        // --- Thêm các trường sau để Server báo lại cho Client khi tạo phòng ---
        public string RoomId { get; init; } = string.Empty;
        public int MySymbol { get; init; } // 1 cho X, 2 cho O
        public string OpponentName { get; init; } = string.Empty;
    }
}
