# Yêu cầu kỹ thuật: Tính năng Đầu Hàng & Ván Mới

Gửi Dev 1 (phụ trách Backend & Shared),

Để nhánh `feature/ui-gameboard` có thể hoàn thiện 100% các nút chức năng còn lại, Client đang chờ các thành phần sau từ phía Shared và Server. Vui lòng thêm các nội dung này vào nhánh `develop`.

## 1. Cập nhật CaroShared

### a. Thêm MessageType
Trong file `Code/UDM_16_CaroGame/CaroShared/Enums/MessageType.cs`, vui lòng bổ sung type:
```csharp
public enum MessageType
{
    // ... các type cũ ...
    SurrenderRequest, // Client gửi yêu cầu đầu hàng
    NewGameRequest,   // Client gửi yêu cầu ván mới
    // Không cần SurrenderResponse vì Server sẽ phản hồi bằng GameOverEvent cho cả 2 bên
    // Tùy ý tạo NewGameResponse hoặc dùng event để báo hiệu ván mới bắt đầu
}
```

### b. Tạo DTO mới: `SurrenderRequest` và `NewGameRequest`
Tạo các file tương ứng trong `Code/UDM_16_CaroGame/CaroShared/Contracts/`:
```csharp
namespace CaroShared.Contracts
{
    public record SurrenderRequest
    {
        public string RoomId { get; init; } = string.Empty;
        public string PlayerId { get; init; } = string.Empty; 
    }

    public record NewGameRequest
    {
        public string RoomId { get; init; } = string.Empty;
    }
}
```

## 2. Cập nhật CaroServer (Xử lý Backend)

Vui lòng bắt `MessageType.SurrenderRequest` và `MessageType.NewGameRequest` trong `TcpServerManager.cs` (hoặc class xử lý tương ứng) và thực hiện các luồng logic tương ứng (chuyển state, broadcast event).

---
**Lưu ý:** Ngay khi bạn (Dev 1) hoàn thành và merge vào `develop`, Dev phụ trách Client sẽ pull về và hoàn thiện việc gửi gói tin ở 2 nút Đầu Hàng và Ván Mới trên `GameBoardForm`. Mọi giao diện và logic Client đã sẵn sàng chờ đón!
