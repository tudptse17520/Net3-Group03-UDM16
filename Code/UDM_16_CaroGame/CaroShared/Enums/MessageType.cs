namespace CaroShared.Enums
{
    public enum MessageType
    {
        // 1. Connection & Session
        LoginRequest,
        LoginResponse,

        // 2. Sảnh chờ (Lobby)
        PlayerListResponse,

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
