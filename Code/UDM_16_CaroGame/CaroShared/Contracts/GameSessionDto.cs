namespace CaroShared.Contracts
{
    // Trạng thái ván đấu hiện tại
    public record GameSessionDto
    {
        // Mảng bàn cờ (0: trống, 1: X, 2: O)
        public int[][] Board { get; init; } = [];

        // Lượt của ai (1 là X, 2 là O)
        public int CurrentTurn { get; init; }

        // Trạng thái game (Playing, Paused, ...)
        public string Status { get; init; } = string.Empty;

        // Thời gian còn lại của lượt này
        public int RemainingTimeSeconds { get; init; }
    }
}
