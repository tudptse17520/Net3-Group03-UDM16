namespace CaroServer.Logging
{
    // Một bản ghi diễn biến trong phòng/trận đấu.
    public sealed record GameEventLog
    {
        public DateTime TimestampUtc { get; init; }
        public string RoomId { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public string PlayerId { get; init; } = string.Empty;
        public int X { get; init; }
        public int Y { get; init; }
        public int PlayerSymbol { get; init; }
        public bool IsValid { get; init; }
        public string Result { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}
