using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CaroServer.Managers
{
    // Quản lý danh sách người chơi đang online ở sảnh chờ
    public class LobbyManager
    {
        // Lưu trữ người chơi an toàn trong môi trường đa luồng (Multi-threading)
        // Key: PlayerId, Value: PlayerName
        // ConcurrentDictionary tự xử lý lock bên trong nên ta không cần dùng từ khóa lock thủ công ở ngoài
        private readonly ConcurrentDictionary<string, string> _onlinePlayers = new();

        // Thêm người chơi mới vào sảnh
        public bool AddPlayer(string playerId, string playerName)
        {
            // TryAdd tự động xử lý an toàn luồng (thread-safe)
            return _onlinePlayers.TryAdd(playerId, playerName);
        }

        // Xóa người chơi khỏi sảnh khi họ thoát game hoặc vào phòng chơi
        public void RemovePlayer(string playerId)
        {
            // TryRemove xóa an toàn không lo xung đột dữ liệu
            _onlinePlayers.TryRemove(playerId, out _);
        }

        // Lấy danh sách tên người chơi để trả về Client
        public List<string> GetOnlinePlayerNames()
        {
            // Trích xuất Values thành List. Thao tác đọc này an toàn trong ConcurrentDictionary
            return _onlinePlayers.Values.ToList();
        }
    }
}
