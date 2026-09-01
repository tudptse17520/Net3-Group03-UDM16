namespace CaroShared.Contracts
{
    // Dữ liệu Server gửi về Client khi có người vừa đánh cờ
    public class MoveMadeEventDto
    {
        public string RoomId { get; set; } = string.Empty;
        public string PlayerId { get; set; } = string.Empty;
        public int X { get; set; }
        public int Y { get; set; }
        
        // 0: Đang chơi, 1: X thắng, 2: O thắng
        public int WinnerSymbol { get; set; }
        
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
