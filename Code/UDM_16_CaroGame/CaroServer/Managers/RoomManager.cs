using System;
using System.Collections.Concurrent;
using CaroServer.Game;
using CaroShared.Enums;

namespace CaroServer.Managers
{
    // Quản lý các phòng chơi đang diễn ra trên Server
    public class RoomManager
    {
        // Thread-safe vì nhiều luồng có thể tạo/xóa phòng cùng lúc
        private readonly ConcurrentDictionary<string, GameSession> _rooms = new();

        public string CreateRoom(string playerXId, string playerOId)
        {
            if (string.IsNullOrWhiteSpace(playerXId))
            {
                Console.WriteLine("[RoomManager] CreateRoom thất bại: playerXId trống");
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(playerOId))
            {
                Console.WriteLine("[RoomManager] CreateRoom thất bại: playerOId trống");
                return string.Empty;
            }

            if (string.Equals(playerXId, playerOId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[RoomManager] CreateRoom thất bại: không thể tự đấu với chính mình");
                return string.Empty;
            }

            string roomId = "ROOM-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var session = new GameSession(roomId, playerXId, playerOId);

            if (!_rooms.TryAdd(roomId, session))
            {
                Console.WriteLine($"[RoomManager] CreateRoom thất bại: trùng roomId {roomId}");
                return string.Empty;
            }

            Console.WriteLine($"[RoomManager] Phòng {roomId} đã được tạo: {playerXId} (X) vs {playerOId} (O)");

            return roomId;
        }

        public MoveResult HandleMove(string roomId, string playerId, int x, int y)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "roomId không hợp lệ",
                    X = x, Y = y
                };
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "playerId không hợp lệ",
                    X = x, Y = y
                };
            }

            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.RoomNotFound,
                    ErrorMessage = "Phòng không tồn tại",
                    X = x, Y = y
                };
            }

            // Khán giả không được phép thực hiện nước đi
            if (!session.IsPlayer(playerId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.PlayerNotInRoom,
                    ErrorMessage = "Bạn không phải người chơi trong phòng này",
                    X = x, Y = y
                };
            }

            int playerSymbol = session.GetPlayerSymbol(playerId);
            MoveResult result = session.Engine.MakeMove(x, y, playerSymbol);

            return result;
        }

        public MoveResult? LeaveRoom(string roomId, string playerId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.InvalidRequest,
                    ErrorMessage = "roomId không hợp lệ"
                };
            }

            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.RoomNotFound,
                    ErrorMessage = "Phòng không tồn tại"
                };
            }

            if (!session.IsPlayer(playerId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorCode = ErrorCode.PlayerNotInRoom,
                    ErrorMessage = "Người chơi không thuộc phòng này"
                };
            }

            // Người còn lại được tính là người thắng
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

        public void RemoveRoom(string roomId)
        {
            if (string.IsNullOrWhiteSpace(roomId))
            {
                Console.WriteLine("[RoomManager] RemoveRoom: roomId trống, bỏ qua");
                return;
            }

            if (_rooms.TryRemove(roomId, out _))
            {
                Console.WriteLine($"[RoomManager] Phòng {roomId} đã bị xóa");
            }
            else
            {
                Console.WriteLine($"[RoomManager] RemoveRoom: Phòng {roomId} không tồn tại");
            }
        }

        public GameSession? GetSession(string roomId)
        {
            _rooms.TryGetValue(roomId, out var session);
            return session;
        }

        public int GetActiveRoomCount()
        {
            return _rooms.Count;
        }
    }
}
