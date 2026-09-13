using System;

namespace CaroServer.Models
{
    // Lịch sử một ván đấu Caro
    public class MatchHistory
    {
        // Khóa chính (Tự tăng trong SQL Server)
        public int Id { get; set; }

        // Mã phòng (vd: ROOM-A1B2C3)
        public string RoomId { get; set; } = string.Empty;

        // Tên (ID) người chơi cầm X
        public string PlayerXId { get; set; } = string.Empty;

        // Tên (ID) người chơi cầm O
        public string PlayerOId { get; set; } = string.Empty;

        // 1 = X thắng, 2 = O thắng, 0 = Hòa
        public int WinnerSymbol { get; set; }

        // Tổng số nước đi trong trận
        public int TotalMoves { get; set; }

        // Thời gian trận đấu kết thúc
        public DateTime PlayedAt { get; set; }
    }
}
