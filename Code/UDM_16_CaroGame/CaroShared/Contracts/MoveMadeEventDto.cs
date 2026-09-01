namespace CaroShared.Contracts
{
    public record MoveMadeEventDto
    {
        public string RoomId { get; init; } = string.Empty;
        public string PlayerId { get; init; } = string.Empty;
        public int X { get; init; }
        public int Y { get; init; }
        
        // 0: Trống (không ai thắng), 1: X thắng, 2: O thắng
        public int WinnerSymbol { get; init; }
        
        public bool IsValid { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
    }
}
