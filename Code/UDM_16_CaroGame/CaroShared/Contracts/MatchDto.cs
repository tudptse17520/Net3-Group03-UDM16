using System;

namespace CaroShared.Contracts
{
    public record MatchDto
    {
        public string RoomId { get; init; } = string.Empty;
        public string PlayerXId { get; init; } = string.Empty;
        public string PlayerOId { get; init; } = string.Empty;
        
        // 1 = X thắng, 2 = O thắng, 0 = Hòa
        public int WinnerSymbol { get; init; }
        
        public int TotalMoves { get; init; }
        public DateTime PlayedAt { get; init; }
    }
}
