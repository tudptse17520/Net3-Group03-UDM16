using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using CaroServer.Game;
using CaroServer.Models;
using CaroShared.Constants;

namespace CaroServer.Managers
{
    // Quản lý các phòng chơi
    public class RoomManager
    {
        // Quản lý Room thread-safe vì nhiều luồng có thể tạo/xóa phòng cùng lúc
        private readonly ConcurrentDictionary<string, Room> _rooms = new();

        // Sự kiện khi hết thời gian của lượt đánh
        public event Action<string, MoveResult>? OnRoomTimeout;

        // Tạo phòng mới
        public string CreateRoom(string playerXId, string playerOId)
        {
            string roomId = "ROOM-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var room = new Room(roomId, playerXId, playerOId);
            if (_rooms.TryAdd(roomId, room))
            {
                Console.WriteLine($"[RoomManager] Room {roomId} created: {playerXId} (X) vs {playerOId} (O)");
                // Bắt đầu đếm giờ cho lượt đầu tiên của Player X
                StartTurnTimer(room.Session, 1, room.Session.Engine.MoveCount + 1);
            }

            return roomId;
        }

        // Xử lý nước đi của người chơi
        public MoveResult HandleMove(string roomId, string playerId, int x, int y)
        {
            var room = GetRoom(roomId);
            if (room == null)
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorMessage = "Phòng không tồn tại",
                    X = x, Y = y
                };
            }

            // Khán giả không được phép thực hiện nước đi
            if (!room.IsPlayer(playerId))
            {
                return new MoveResult
                {
                    IsValid = false,
                    ErrorMessage = "Bạn là khán giả, không được đánh cờ",
                    X = x, Y = y
                };
            }

            int playerSymbol = room.GetPlayerSymbol(playerId);
            MoveResult result = room.Session.Engine.MakeMove(x, y, playerSymbol);

            if (result.IsValid)
            {
                if (result.IsGameOver)
                {
                    room.Session.StopTimer();
                }
                else
                {
                    // Bắt đầu đếm giờ cho lượt kế tiếp
                    StartTurnTimer(room.Session, result.NextTurn, room.Session.Engine.MoveCount + 1);
                }
            }

            return result;
        }

        public MoveResult? HandleTimeout(string roomId, int timedOutPlayerSymbol)
        {
            var room = GetRoom(roomId);
            if (room == null)
            {
                return null;
            }

            MoveResult result = room.Session.Engine.HandleTimeout(timedOutPlayerSymbol);
            if (!result.IsValid)
            {
                return null;
            }

            room.Session.StopTimer();

            Console.WriteLine($"[RoomManager] Room {roomId} timeout: Player {timedOutPlayerSymbol} lost");
            OnRoomTimeout?.Invoke(roomId, result);
            return result;
        }

        public MoveResult? LeaveRoom(string roomId, string playerId)
        {
            var room = GetRoom(roomId);
            if (room == null)
            {
                return null;
            }

            // Nếu khán giả rời phòng
            if (room.IsSpectator(playerId))
            {
                room.RemoveSpectator(playerId);
                Console.WriteLine($"[RoomManager] Spectator {playerId} left room {roomId}");
                return null;
            }

            if (!room.IsPlayer(playerId))
            {
                return null;
            }

            // Dừng timer khi người chơi chính rời phòng
            room.Session.StopTimer();

            // Người chơi chính rời phòng: người còn lại được tính là người thắng
            int winnerSymbol = room.GetPlayerSymbol(playerId) == 1 ? 2 : 1;

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
            if (_rooms.TryRemove(roomId, out var room))
            {
                room.Session.Dispose();
                Console.WriteLine($"[RoomManager] Room {roomId} removed");
            }
        }

        // Tìm Room theo ID
        public Room? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        public GameSession? GetSession(string roomId)
        {
            return GetRoom(roomId)?.Session;
        }

        // Hỗ trợ thêm/xóa khán giả qua RoomManager
        public bool AddSpectator(string roomId, string spectatorId)
        {
            var room = GetRoom(roomId);
            if (room == null) return false;
            return room.AddSpectator(spectatorId);
        }

        public bool RemoveSpectator(string roomId, string spectatorId)
        {
            var room = GetRoom(roomId);
            if (room == null) return false;
            return room.RemoveSpectator(spectatorId);
        }

        // Kích hoạt sự kiện hết giờ của phòng
        public void TriggerRoomTimeout(string roomId, MoveResult moveResult)
        {
            OnRoomTimeout?.Invoke(roomId, moveResult);
        }

        public int GetActiveRoomCount()
        {
            return _rooms.Count;
        }

        private void StartTurnTimer(GameSession session, int playerSymbol, int turnNumber)
        {
            session.Timer.StartTurn(turnNumber, GameConstants.TurnTimeoutSeconds, _ =>
            {
                HandleTimeout(session.RoomId, playerSymbol);
            });
        }
    }
}
