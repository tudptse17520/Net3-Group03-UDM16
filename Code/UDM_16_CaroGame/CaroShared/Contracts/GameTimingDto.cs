namespace CaroShared.Contracts;

// Server samples are compared with each other, never with the client's wall clock.
public record GameTimingDto
{
    public DateTime MatchStartedAtUtc { get; init; }
    public DateTime? MatchEndedAtUtc { get; init; }
    public DateTime ServerNowUtc { get; init; }
    public DateTime? TurnDeadlineUtc { get; init; }
    public int TurnDurationSeconds { get; init; }
    public int RemainingTimeSeconds { get; init; }
    public int CurrentTurn { get; init; }
    public int TurnNumber { get; init; }
    public bool IsPaused { get; init; }
}
