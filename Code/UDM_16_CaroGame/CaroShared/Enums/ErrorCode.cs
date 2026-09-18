namespace CaroShared.Enums
{
    // Mã lỗi chuẩn hóa cho toàn bộ hệ thống
    // Sử dụng explicit numeric values để giữ ổn định khi thêm mới
    public enum ErrorCode
    {
        None = 0,

        // Room errors (100-199)
        RoomNotFound = 100,

        // Player errors (200-299)
        PlayerNotInRoom = 201,

        // Game logic errors (300-399)
        GameAlreadyFinished = 300,
        NotPlayerTurn = 301,
        InvalidCoordinates = 302,
        CellOccupied = 303,

        // Request errors (400-499)
        InvalidRequest = 400,
        MalformedPayload = 401,
        MissingPayload = 402
    }
}
