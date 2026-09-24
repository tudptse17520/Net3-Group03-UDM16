namespace CaroShared.Contracts
{
    // Yêu cầu khôi phục phiên chơi sau khi Client mất kết nối
    public record ReconnectRequest
    {
        public string SessionToken { get; init; } = string.Empty;
    }
}
