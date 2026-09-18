namespace CaroShared.Contracts
{
    // Yêu cầu từ Client xin tham gia làm khán giả của một phòng chơi
    public record JoinSpectatorRequest
    {
        // Mã phòng muốn vào xem
        public string RoomId { get; init; } = string.Empty;
    }
}
