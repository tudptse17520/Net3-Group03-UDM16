using CaroShared.Enums;

namespace CaroShared.Contracts
{
    // Phản hồi lỗi chuẩn hóa từ Server
    public record ErrorResponse
    {
        public ErrorCode Code { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
