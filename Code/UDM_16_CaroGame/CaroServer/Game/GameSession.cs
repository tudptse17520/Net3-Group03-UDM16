using System;

namespace CaroServer.Game
{
    // Đại diện cho một ván đấu trong phòng
    public class GameSession
    {
        public string RoomId { get; private set; }
        public string PlayerXId { get; private set; }
        public string PlayerOId { get; private set; }

        // Mỗi ván đấu sử dụng một CaroEngine riêng
        public CaroEngine Engine { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public GameSession(string roomId, string playerXId, string playerOId)
        {
            RoomId = roomId;
            PlayerXId = playerXId;
            PlayerOId = playerOId;
            Engine = new CaroEngine();
            CreatedAt = DateTime.Now;
        }

        public bool IsPlayer(string playerId)
        {
            return playerId == PlayerXId || playerId == PlayerOId;
        }

        // X = 1, O = 2, không phải người chơi = 0
        public int GetPlayerSymbol(string playerId)
        {
            if (playerId == PlayerXId) return 1;
            if (playerId == PlayerOId) return 2;
            return 0;
        }
    }
}
