namespace CaroShared.Contracts
{
    // Client gửi yêu cầu tham gia phòng với vai trò khán giả.
    public record JoinSpectatorRequest
    {
        public string RoomId { get; init; } = string.Empty;
        public string SpectatorName { get; init; } = string.Empty;
    }
}
