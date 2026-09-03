namespace CaroShared.Enums
{
    public enum MessageType
    {
        // 1. Connection & Session
        LoginRequest,
        LoginResponse,
        
        // Heartbeat - Sprint 2 - Dev 5
        Ping,
        Pong,

        // 2. Sảnh chờ (Lobby)
        PlayerListResponse,

        // 3. Thách đấu
        ChallengeRequest,
        ChallengeResponse,

        // 4. Trong trận đấu
        MakeMoveRequest,
        MoveMadeEvent,
        GameStateUpdate,
        GameOverEvent,

        // 5. Khán giả
        JoinSpectatorRequest,
        SpectatorStateSnapshot,
        LeaveSpectatorRequest,
        SpectatorError
    }
}
