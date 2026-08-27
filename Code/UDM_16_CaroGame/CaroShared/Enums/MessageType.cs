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
        GetPlayerListRequest,
        PlayerListResponse,
        LogoutRequest,

        // 3. Thách đấu
        ChallengeRequest,
        ChallengeResponse,

        // 4. Trong trận đấu
        MakeMoveRequest,
        MoveMadeEvent,
        GameStateUpdate, // Nếu cần thiết để update trạng thái toàn ván
        GameOverEvent,

        // Mở rộng sau:
        // JoinSpectatorRequest,
        // ReconnectRequest, vân vân...
    }
}
