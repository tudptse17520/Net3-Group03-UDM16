using System.Collections.Generic;

namespace CaroShared.Contracts
{
    // Danh sách người chơi online ở sảnh chờ gửi về cho Client
    public record PlayerListResponse
    {
        // Chứa danh sách tên người chơi
        public List<string> PlayerNames { get; init; } = new();
    }
}
