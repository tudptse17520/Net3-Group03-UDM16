using System.Collections.Generic;

namespace CaroShared.Contracts
{
    // Danh sách người chơi online ở sảnh chờ gửi về cho Client
    public record PlayerListResponse
    {
        // Chứa danh sách thông tin người chơi
        public List<PlayerInfoDto> Players { get; init; } = new();

        // Token dùng để khôi phục phiên khi Client mất kết nối
        public string? SessionToken { get; init; }
    }
}
