using CaroShared.Contracts;

namespace CaroServer.Spectator
{
    public enum SpectatorJoinResultCode
    {
        Joined,
        InvalidRequest,
        RoomNotFound,
        GameStateUnavailable
    }

    public sealed record SpectatorJoinResult(
        SpectatorJoinResultCode Code,
        SpectatorStateSnapshotDto? Snapshot = null,
        string? Error = null);
}
