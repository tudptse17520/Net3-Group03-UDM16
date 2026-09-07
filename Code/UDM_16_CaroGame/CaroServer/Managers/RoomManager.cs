using System;
using System.Collections.Concurrent;
using CaroServer.Game;
using CaroShared.Constants;

namespace CaroServer.Managers
{
    // Quản lý các phòng chơi đang diễn ra trên Server
    public class RoomManager
    {
        // Thread-safe vì nhiều luồng có thể tạo/xóa phòng cùng lúc
        private readonly ConcurrentDictionary<string, GameSession> _rooms = new();

        public event Action<string, MoveResult>? OnRoomTimeout;

        public string CreateRoom(string playerXId, string playerOId)
        {
            string roomId = "ROOM-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();

            var session = new GameSession(roomId, playerXId, playerOId);
            if (_rooms.TryAdd(roomId, session))
            {
                Console.WriteLine($"[RoomManager] Phòng {roomId} đã được tạo: {playerXId} (X) vs {playerOId} (O)");
                StartTurnTimer(session, 1, session.Engine.MoveCount + 1);
            }

            return roomId;
        }

        public MoveResult HandleMove(string roomId, string playerId, int x, int y)
        {
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return new MoveResult
                {
                    IsValid = false,
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
                    ErrorMessage = "Bạn là khán giả, không được đánh cờ",
                    X = x, Y = y
                };
            }

            int playerSymbol = session.GetPlayerSymbol(playerId);
            MoveResult result = session.Engine.MakeMove(x, y, playerSymbol);

            if (result.IsValid)
            {
                if (result.IsGameOver)
                {
                    session.StopTimer();
                }
                else
                {
                    StartTurnTimer(session, result.NextTurn, session.Engine.MoveCount + 1);
                }
            }

            return result;
        }

        public MoveResult? HandleTimeout(string roomId, int timedOutPlayerSymbol)
        {
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return null;
            }

            MoveResult result = session.Engine.HandleTimeout(timedOutPlayerSymbol);
            if (!result.IsValid)
            {
                return null;
            }

            session.StopTimer();

            Console.WriteLine($"[RoomManager] Phòng {roomId} hết giờ: Người chơi {timedOutPlayerSymbol} thua");
            OnRoomTimeout?.Invoke(roomId, result);
            return result;
        }

        public MoveResult? LeaveRoom(string roomId, string playerId)
        {
            if (!_rooms.TryGetValue(roomId, out var session))
            {
                return null;
            }

            if (!session.IsPlayer(playerId))
            {
                return null;
            }

            session.StopTimer();

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
            if (_rooms.TryRemove(roomId, out var session))
            {
                session.Dispose();
                Console.WriteLine($"[RoomManager] Phòng {roomId} đã bị xóa");
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

        private void StartTurnTimer(GameSession session, int playerSymbol, int turnNumber)
        {
            session.Timer.StartTurn(turnNumber, GameConstants.TurnTimeoutSeconds, _ =>
            {
                HandleTimeout(session.RoomId, playerSymbol);
            });
        }
    }
}
