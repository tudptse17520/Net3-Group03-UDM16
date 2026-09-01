using System.Collections.Concurrent;
using CaroShared.Contracts;

namespace CaroServer.Models
{
    // Quản lý một phòng chơi
    public class Room
    {
        // ID của phòng
        public string RoomId { get; private set; }

        // Người cầm cờ X
        public PlayerSession PlayerX { get; private set; }

        // Người cầm cờ O
        public PlayerSession PlayerO { get; private set; }

        // Danh sách khán giả đang xem
        public ConcurrentBag<PlayerSession> Spectators { get; private set; }

        // Thông tin ván đấu
        public GameSessionDto GameSession { get; set; }

        public Room(string roomId, PlayerSession playerX, PlayerSession playerO)
        {
            RoomId = roomId;
            PlayerX = playerX;
            PlayerO = playerO;
            Spectators = new ConcurrentBag<PlayerSession>();
            GameSession = new GameSessionDto(); 
        }
    }
}
