namespace CaroShared.Enums
{
    public enum MessageType
    {
        // 1. Connection & Session
        LoginRequest,
        LoginResponse,
        ReconnectRequest,
        ReconnectResponse,
        
        // Heartbeat - Sprint 2 - Dev 5
        Ping,
        Pong,

        // 2. Sảnh chờ (Lobby)
        PlayerListRequest,
        PlayerListResponse,

        // 3. Thách đấu
        ChallengeRequest,
        ChallengeResponse,

        // 4. Trong trận đấu
        MakeMoveRequest,
        MoveMadeEvent,
        GameStateUpdate, // Nếu cần thiết để update trạng thái toàn ván
        GameOverEvent,
        SurrenderRequest,
        NewGameRequest,
        NewGameEvent,

        // 5. Khán giả (Spectator)
        JoinSpectatorRequest,
        JoinSpectatorResponse,
        RoomListRequest,
        RoomListResponse,

        // 6. Lịch sử đấu
        MatchHistoryRequest,
        MatchHistoryResponse,

        // 7. Hoà (Draw)
        DrawOfferRequest,
        DrawOfferEvent,
        DrawResponseRequest,
        DrawOfferResolvedEvent,

        // 8. Cá nhân hoá & Avatar
        AvatarUpdateRequest,
        AvatarUpdateResponse,
        AvatarRemoveRequest,
        AvatarRemoveResponse,
        AvatarRequest,
        AvatarDataEvent,
        AvatarChangedEvent,

        // 9. Xử lý lỗi
        ErrorResponse,
    }
}
