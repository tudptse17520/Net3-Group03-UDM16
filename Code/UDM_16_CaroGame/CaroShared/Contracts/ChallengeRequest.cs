namespace CaroShared.Contracts
{
    // Dữ liệu yêu cầu thách đấu
    public class ChallengeRequest
    {
        // ID của người được mời
        public string TargetPlayerId { get; set; } = string.Empty;
    }
}