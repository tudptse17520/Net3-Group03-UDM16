namespace CaroShared.Contracts
{
    // Dữ liệu yêu cầu thách đấu
    public record ChallengeRequest
    {
        // ID của người được mời
        public string TargetPlayerId { get; init; } = string.Empty;
    }
}