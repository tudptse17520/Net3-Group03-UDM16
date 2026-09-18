namespace CaroShared.Contracts
{
    /// <summary>
    /// Yêu cầu từ Client xin tham gia làm khán giả của một phòng chơi.
    /// </summary>
    public record JoinSpectatorRequest
    {
        /// <summary>Mã phòng muốn vào xem.</summary>
        public string RoomId { get; init; } = string.Empty;
    }
}
