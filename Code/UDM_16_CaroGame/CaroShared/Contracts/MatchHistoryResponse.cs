using System.Collections.Generic;

namespace CaroShared.Contracts
{
    public record MatchHistoryResponse
    {
        public List<MatchDto> Matches { get; init; } = new();
    }
}
