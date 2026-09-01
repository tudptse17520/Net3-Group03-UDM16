using System;
using System.Collections.Concurrent;
using CaroServer.Models;

namespace CaroServer.Managers
{
    // Quản lý các phòng chơi
    public class RoomManager
    {
        private readonly ConcurrentDictionary<string, Room> _rooms = new();

        // Tạo phòng mới
        public Room CreateRoom(PlayerSession playerX, PlayerSession playerO)
        {
            string roomId = Guid.NewGuid().ToString("N");
            var room = new Room(roomId, playerX, playerO);
            
            _rooms.TryAdd(roomId, room);
            Console.WriteLine($"[RoomManager] Created room {roomId} (X: {playerX.PlayerId}, O: {playerO.PlayerId})");
            
            return room;
        }

        // Tìm phòng theo ID
        public Room? GetRoom(string roomId)
        {
            _rooms.TryGetValue(roomId, out var room);
            return room;
        }

        // Xóa phòng
        public void RemoveRoom(string roomId)
        {
            if (_rooms.TryRemove(roomId, out _))
            {
                Console.WriteLine($"[RoomManager] Removed room {roomId}");
            }
        }
    }
}
