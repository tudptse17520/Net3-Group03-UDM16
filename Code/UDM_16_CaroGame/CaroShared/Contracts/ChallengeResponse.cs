namespace CaroShared.Contracts
{
    // Dữ liệu phản hồi lời mời thách đấu
    public record ChallengeResponse
    {
        // ID của người gửi lời mời
        public string ChallengerId { get; init; } = string.Empty;

        // Đồng ý hay từ chối
        public bool IsAccepted { get; init; }
    }
}
