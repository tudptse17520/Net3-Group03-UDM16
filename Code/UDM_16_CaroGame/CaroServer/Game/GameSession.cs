using System;

namespace CaroServer.Game
{
    // Đại diện cho một ván đấu đang diễn ra trong phòng
    // Phân biệt rõ ràng theo đặc tả Sprint 3:
    //   Room quản lý NGƯỜI (Player X, O, Spectators) → do Dev 1 xử lý
    //   GameSession quản lý TRẬN ĐẤU (Board, Turn, Status) → do Dev 2 (tôi) xử lý
    public class GameSession
    {
        // Mã phòng (do RoomManager sinh khi tạo phòng)
        public string RoomId { get; private set; }

        // ID người chơi cầm X (luôn đánh trước)
        public string PlayerXId { get; private set; }

        // ID người chơi cầm O
        public string PlayerOId { get; private set; }

        // Bộ máy logic xử lý luật chơi cho ván đấu này
        // Mỗi phòng có 1 CaroEngine riêng, không dùng chung
        public CaroEngine Engine { get; private set; }

        // Thời điểm tạo ván đấu (dùng để tính thời gian trận)
        public DateTime CreatedAt { get; private set; }

        public GameSession(string roomId, string playerXId, string playerOId)
        {
            RoomId = roomId;
            PlayerXId = playerXId;
            PlayerOId = playerOId;

            // Mỗi ván đấu khởi tạo CaroEngine mới hoàn toàn
            Engine = new CaroEngine();
            CreatedAt = DateTime.Now;
        }

        // Kiểm tra xem playerId có phải người chơi trong phòng này không
        // Dùng để chặn Spectator (khán giả) gửi MakeMoveRequest
        // Bước 2 trong luồng kiểm tra 6 bước
        public bool IsPlayer(string playerId)
        {
            return playerId == PlayerXId || playerId == PlayerOId;
        }

        // Lấy symbol (1 hoặc 2) của người chơi dựa trên playerId
        // X = 1, O = 2, Không phải người chơi = 0
        // Dùng để truyền vào CaroEngine.MakeMove(x, y, playerSymbol)
        public int GetPlayerSymbol(string playerId)
        {
            if (playerId == PlayerXId) return 1;
            if (playerId == PlayerOId) return 2;
            return 0; // Spectator hoặc ID không hợp lệ
        }
    }
}
