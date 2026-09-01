namespace CaroShared.Contracts
{
    // Dữ liệu phản hồi lời mời thách đấu
    public class ChallengeResponse
    {
        // ID của người gửi lời mời
        public string ChallengerId { get; set; } = string.Empty;

        // Đồng ý hay từ chối
        public bool IsAccepted { get; set; }
    }
}
