using CaroShared.Constants;
using CaroShared.Contracts;

namespace CaroClient;

// Monotonic presentation estimates, calibrated only by authoritative server events.
// Reaching zero here never produces a game-over or sends a timeout request.
public sealed class GamePresentationState
{
    private readonly Func<double> _now;
    private double _matchSample;
    private double _elapsedAtSample;
    private double _turnSample;
    private double _remainingAtSample;
    private DateTime _lastServerSample;
    public Guid MatchId { get; private set; }
    public int CurrentTurn { get; private set; }
    public int TurnNumber { get; private set; }
    public int TurnDuration { get; private set; } = GameConstants.TurnTimeoutSeconds;
    public bool Running { get; private set; }
    public bool Paused { get; private set; }
    public bool Terminal { get; private set; }
    public TimeSpan Elapsed => TimeSpan.FromSeconds(Math.Max(0, _elapsedAtSample + (Running ? _now() - _matchSample : 0)));
    public double Remaining => Math.Max(0, _remainingAtSample - (Running && !Paused ? _now() - _turnSample : 0));

    public GamePresentationState(Func<double>? now = null) => _now = now ?? (() => Environment.TickCount64 / 1000d);
    public bool Begin(Guid matchId, GameTimingDto? timing, int startingTurn = 1)
    {
        if (matchId != Guid.Empty && MatchId == matchId && (Running || Terminal)) return Apply(timing);
        MatchId = matchId;
        Terminal = false;
        Running = true;
        Paused = false;
        _elapsedAtSample = 0;
        _matchSample = _now();
        _lastServerSample = default;
        CurrentTurn = 0;
        TurnNumber = 0;
        return timing != null ? Apply(timing) : SetTurn(startingTurn, GameConstants.TurnTimeoutSeconds);
    }
    public bool Apply(GameTimingDto? timing)
    {
        if (timing == null || (_lastServerSample != default && timing.ServerNowUtc <= _lastServerSample) || timing.TurnNumber < TurnNumber) return false;
        if (Terminal && timing.MatchEndedAtUtc == null) return false;
        _lastServerSample = timing.ServerNowUtc;
        if (timing.MatchStartedAtUtc != default)
        {
            _elapsedAtSample = Math.Max(0, ((timing.MatchEndedAtUtc ?? timing.ServerNowUtc) - timing.MatchStartedAtUtc).TotalSeconds);
            _matchSample = _now();
        }
        bool changed = CurrentTurn != timing.CurrentTurn && !Terminal;
        CurrentTurn = timing.CurrentTurn;
        TurnNumber = timing.TurnNumber;
        TurnDuration = timing.TurnDurationSeconds > 0 ? timing.TurnDurationSeconds : GameConstants.TurnTimeoutSeconds;
        _remainingAtSample = timing.TurnDeadlineUtc is DateTime deadline
            ? Math.Clamp((deadline - timing.ServerNowUtc).TotalSeconds, 0, TurnDuration)
            : Math.Clamp(timing.RemainingTimeSeconds, 0, TurnDuration);
        _turnSample = _now();
        Paused = timing.IsPaused;
        if (timing.MatchEndedAtUtc != null) { Terminal = true; Running = false; }
        return changed;
    }
    public bool SetTurn(int symbol, double remaining)
    {
        if (Terminal) return false;
        bool changed = CurrentTurn != symbol;
        CurrentTurn = symbol;
        _remainingAtSample = Math.Clamp(remaining, 0, TurnDuration);
        _turnSample = _now();
        return changed;
    }
    public void Pause() { _remainingAtSample = Remaining; _turnSample = _now(); Paused = true; }
    public void Freeze(GameTimingDto? timing = null)
    {
        _elapsedAtSample = Elapsed.TotalSeconds;
        _remainingAtSample = Remaining;
        _matchSample = _turnSample = _now();
        Running = false;
        Terminal = true;
        if (timing?.MatchEndedAtUtc != null) Apply(timing);
    }
}
