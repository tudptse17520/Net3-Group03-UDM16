using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using CaroServer.Game;

namespace CaroServer.Managers
{
    // Quản lý các phòng chơi đang diễn ra trên Server
    public class RoomManager
    {
        // Thread-safe vì nhiều luồng có thể tạo/xóa phòng cùng lúc
        private readonly ConcurrentDictionary<string, GameSession> _rooms = new();

        public string CreateRoom(string playerXId, string playerOId)
        {
            string roomId = "ROOM-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var session = new GameSession(roomId, playerXId, playerOId);
            _rooms.TryAdd(roomId, session);

            Console.WriteLine($"[RoomManager] Phòng {roomId} đã được tạo: {playerXId} (X) vs {playerOId} (O)");

            // DEV 5: Ghi log sự kiện tạo phòng.
            WriteGameEvent(roomId, "RoomCreated", playerXId,
                message: $"Phòng được tạo: {playerXId} (X) vs {playerOId} (O)");

            return roomId;
        }

        public MoveResult HandleMove(string roomId, string playerId, int x, int y)
        {
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                var result = new MoveResult
                {
                    IsValid = false,
                    ErrorMessage = "Phòng không tồn tại",
                    X = x, Y = y
                };

                // DEV 5: Ghi log nước đi không hợp lệ.
                WriteGameEvent(roomId, "InvalidMove", playerId, x, y,
                    isValid: false, message: result.ErrorMessage);

                return result;
            }

            // Khán giả không được phép thực hiện nước đi
            if (!session.IsPlayer(playerId))
            {
                var result = new MoveResult
                {
                    IsValid = false,
                    ErrorMessage = "Bạn là khán giả, không được đánh cờ",
                    X = x, Y = y
                };

                // DEV 5: Ghi log spectator cố thực hiện nước đi.
                WriteGameEvent(roomId, "InvalidMove", playerId, x, y,
                    isValid: false, message: result.ErrorMessage);

                return result;
            }

            int playerSymbol = session.GetPlayerSymbol(playerId);
            MoveResult result = session.Engine.MakeMove(x, y, playerSymbol);

           
            if (result.IsValid)
            {
                WriteGameEvent(roomId, "MoveMade", playerId, x, y, playerSymbol,
                    isValid: true,
                    result: result.IsGameOver
                        ? (result.IsDraw ? "Draw" : "GameOver")
                        : "Playing");

                if (result.IsGameOver)
                {
                    WriteGameEvent(roomId,
                        result.IsDraw ? "GameDraw" : "GameOver",
                        playerId, x, y, playerSymbol,
                        isValid: true,
                        result: result.IsDraw ? "Draw" : $"Winner: {result.WinnerSymbol}",
                        message: result.IsDraw
                            ? "Trận đấu kết thúc với kết quả hòa"
                            : $"Người chơi {result.WinnerSymbol} thắng");
                }
            }
            else
            {
                // GameEngine tự quyết định lý do nước đi không hợp lệ.
                WriteGameEvent(roomId, "InvalidMove", playerId, x, y, playerSymbol,
                    isValid: false, message: result.ErrorMessage);
            }

            return result;
        }

        public MoveResult? LeaveRoom(string roomId, string playerId)
        {
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                WriteGameEvent(roomId, "InvalidLeave", playerId,
                    isValid: false, message: "Phòng không tồn tại");
                return null;
            }

            if (!session.IsPlayer(playerId))
            {
                WriteGameEvent(roomId, "InvalidLeave", playerId,
                    isValid: false, message: "Người gửi không phải người chơi");
                return null;
            }

            // Người còn lại được tính là người thắng
            int leavingSymbol = session.GetPlayerSymbol(playerId);
            int winnerSymbol = leavingSymbol == 1 ? 2 : 1;

            // DEV 5: Ghi log người chơi rời phòng.
            WriteGameEvent(roomId, "PlayerLeft", playerId,
                playerSymbol: leavingSymbol,
                isValid: true,
                result: $"Winner: {winnerSymbol}",
                message: "Đối thủ đã rời phòng");

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
            _rooms.TryRemove(roomId, out _);
            Console.WriteLine($"[RoomManager] Phòng {roomId} đã bị xóa");

            // DEV 5: Ghi log xóa phòng.
            WriteGameEvent(roomId, "RoomRemoved",
                message: "Phòng đã bị xóa khỏi RoomManager");
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

        

        private static readonly object _logLock = new();

        private static readonly string _logDirectory =
            Path.Combine(AppContext.BaseDirectory, "Logs");

        private static readonly string _logFile =
            Path.Combine(_logDirectory, "game-events.jsonl");

        private static void WriteGameEvent(
            string roomId,
            string eventType,
            string? playerId = null,
            int? x = null,
            int? y = null,
            int? playerSymbol = null,
            bool? isValid = null,
            string? result = null,
            string? message = null)
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);

                var gameEvent = new
                {
                    TimestampUtc = DateTime.UtcNow,
                    RoomId = roomId,
                    EventType = eventType,
                    PlayerId = playerId,
                    X = x,
                    Y = y,
                    PlayerSymbol = playerSymbol,
                    IsValid = isValid,
                    Result = result,
                    Message = message
                };

                string json = JsonSerializer.Serialize(gameEvent);

                lock (_logLock)
                {
                    File.AppendAllText(_logFile, json + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Logging lỗi không được làm ảnh hưởng đến game.
                Console.WriteLine($"[GameEventLog] Không thể ghi log: {ex.Message}");
            }
        }
    }
}
