namespace CaroShared.Contracts
{
    // Kết quả khôi phục phiên và trạng thái ván đấu
    public record ReconnectResponse
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public GameStateDto? GameState { get; init; }
    }
}
