using System;
using System.Collections.Concurrent;
using CaroServer.Game;

namespace CaroServer.Managers
{
    // Quản lý toàn bộ các phòng chơi đang diễn ra trên Server
    // Dùng ConcurrentDictionary giống LobbyManager (Sprint 2) để thread-safe
    // vì nhiều luồng có thể cùng tạo/xóa phòng cùng lúc
    public class RoomManager
    {
        // Key: RoomId, Value: GameSession
        // ConcurrentDictionary an toàn khi nhiều luồng cùng tạo/xóa phòng
        // Mỗi thao tác (tạo phòng, xóa phòng) là đơn lẻ → ConcurrentDictionary đủ
        // (Khác với CaroEngine cần lock vì chuỗi thao tác phải nguyên tử)
        private readonly ConcurrentDictionary<string, GameSession> _rooms = new();

        // Tạo phòng mới khi 2 người chấp nhận thách đấu
        // Dev 1 gọi hàm này từ TcpServerManager khi ChallengeResponse được chấp nhận
        // Trả về RoomId để Dev 1 thông báo cho cả 2 Client
        public string CreateRoom(string playerXId, string playerOId)
        {
            // Sinh mã phòng ngẫu nhiên 6 ký tự (ví dụ: "ROOM-A3F2B1")
            string roomId = "ROOM-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var session = new GameSession(roomId, playerXId, playerOId);

            // TryAdd an toàn khi nhiều cặp người chơi tạo phòng cùng lúc
            _rooms.TryAdd(roomId, session);

            Console.WriteLine($"[RoomManager] Phòng {roomId} đã được tạo: {playerXId} (X) vs {playerOId} (O)");

            return roomId;
        }

        // Xử lý nước đi từ Client
        // Dev 1 gọi hàm này khi nhận MakeMoveRequest từ TCP
        // Hàm này đảm nhận Bước 1 và 2, sau đó giao cho CaroEngine xử lý Bước 3-6
        public MoveResult HandleMove(string roomId, string playerId, int x, int y)
        {
            // Bước 1: Phòng có tồn tại?
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorMessage = "Phòng không tồn tại",
                    X = x, Y = y
                };
            }

            // Bước 2: Người gửi có phải người chơi? (Chặn Spectator đánh cờ)
            if (!session.IsPlayer(playerId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorMessage = "Bạn là khán giả, không được đánh cờ",
                    X = x, Y = y
                };
            }

            // Lấy symbol (1=X, 2=O) của người chơi dựa trên playerId
            int playerSymbol = session.GetPlayerSymbol(playerId);

            // Bước 3 → 6 + xử lý logic: CaroEngine xử lý bên trong lock
            // (Kiểm tra lượt, tọa độ, ô trống, game over đều nằm trong MakeMove)
            MoveResult result = session.Engine.MakeMove(x, y, playerSymbol);

            return result;
        }

        // Xử lý khi người chơi thoát phòng (đầu hàng hoặc rớt mạng)
        // Dev 1 gọi hàm này khi phát hiện Client ngắt kết nối
        public MoveResult? LeaveRoom(string roomId, string playerId)
        {
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return null;
            }

            // Chỉ người chơi mới ảnh hưởng kết quả
            // Spectator thoát thì không cần xử lý gì
            if (!session.IsPlayer(playerId))
            {
                return null;
            }

            // Người còn lại thắng cuộc
            // Nếu X thoát (symbol=1) → O thắng (symbol=2) và ngược lại
            int winnerSymbol = session.GetPlayerSymbol(playerId) == 1 ? 2 : 1;

            return new MoveResult
            {
                IsValid = true,
                IsGameOver = true,
                WinnerSymbol = winnerSymbol,
                IsDraw = false,
                ErrorMessage = "Đối thủ đã rời phòng"
            };
        }

        // Xóa phòng sau khi trận đấu kết thúc
        // Dev 1 gọi hàm này để dọn dẹp bộ nhớ
        public void RemoveRoom(string roomId)
        {
            _rooms.TryRemove(roomId, out _);
            Console.WriteLine($"[RoomManager] Phòng {roomId} đã bị xóa");
        }

        // Lấy thông tin ván đấu trong phòng
        // Dev 1 hoặc Dev 5 có thể cần để kiểm tra trạng thái
        public GameSession? GetSession(string roomId)
        {
            _rooms.TryGetValue(roomId, out var session);
            return session;
        }

        // Đếm số phòng đang hoạt động
        // Dev 3 dùng để hiển thị trên giao diện Lobby
        public int GetActiveRoomCount()
        {
            return _rooms.Count;
        }
    }
}
