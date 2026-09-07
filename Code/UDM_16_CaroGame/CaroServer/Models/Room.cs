using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CaroServer.Game;

namespace CaroServer.Models
{
    // Đại diện cho một phòng chơi: quản lý nhân sự (Player X, Player O, Spectators) và ván đấu
    public class Room
    {
        public string RoomId { get; }
        public string PlayerXId { get; }
        public string PlayerOId { get; }
        public GameSession Session { get; }

        // Sử dụng ConcurrentDictionary làm Thread-safe Set để lưu trữ khán giả
        private readonly ConcurrentDictionary<string, byte> _spectators = new();

        public Room(string roomId, string playerXId, string playerOId)
        {
            RoomId = roomId;
            PlayerXId = playerXId;
            PlayerOId = playerOId;
            Session = new GameSession(roomId, playerXId, playerOId);
        }

        // Thêm khán giả vào phòng
        public bool AddSpectator(string playerId)
        {
            if (IsPlayer(playerId)) return false;
            return _spectators.TryAdd(playerId, 0);
        }

        // Xóa khán giả khỏi phòng
        public bool RemoveSpectator(string playerId)
        {
            return _spectators.TryRemove(playerId, out _);
        }

        // Lấy danh sách ID khán giả
        public IEnumerable<string> GetSpectators()
        {
            return _spectators.Keys;
        }

        public int SpectatorCount => _spectators.Count;

        // Kiểm tra xem playerId có phải người chơi chính hay không
        public bool IsPlayer(string playerId)
        {
            return playerId == PlayerXId || playerId == PlayerOId;
        }

        // Kiểm tra xem playerId có phải là khán giả hay không
        public bool IsSpectator(string playerId)
        {
            return _spectators.ContainsKey(playerId);
        }

        // Lấy ký hiệu cờ: 1 = X, 2 = O, 0 = Không phải người chơi
        public int GetPlayerSymbol(string playerId)
        {
            if (playerId == PlayerXId) return 1;
            if (playerId == PlayerOId) return 2;
            return 0;
        }

        // Lấy toàn bộ ID người tham gia phòng (Player X, Player O và toàn bộ Spectators)
        public IEnumerable<string> GetAllParticipantIds()
        {
            yield return PlayerXId;
            yield return PlayerOId;
            foreach (var spectatorId in _spectators.Keys)
            {
                yield return spectatorId;
            }
        }
    }
}
