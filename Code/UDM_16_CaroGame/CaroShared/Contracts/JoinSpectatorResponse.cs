namespace CaroShared.Contracts
{
    public class JoinSpectatorResponse
    {
        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public SpectatorStateSnapshotDto? Snapshot { get; set; }
    }
}