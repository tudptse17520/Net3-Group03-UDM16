namespace CaroShared.Contracts
{
    // Client gửi lên tọa độ đánh cờ
    public record MakeMoveRequest
    {
        // Tọa độ X
        public int X { get; init; }

        // Tọa độ Y
        public int Y { get; init; }
    }
}
