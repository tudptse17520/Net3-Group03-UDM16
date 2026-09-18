# Yêu cầu bổ sung CaroShared để hoàn thiện các nhánh Client

Tài liệu này liệt kê **toàn bộ** những gì còn thiếu trong `CaroShared` (Contract, Enum) cần được Dev phụ trách Shared bổ sung để các nhánh Client có thể hoàn thiện 100%.

---

## Tổng quan các nhánh bị ảnh hưởng

| Nhánh | Tính năng còn thiếu | Nguyên nhân |
|---|---|---|
| `feature/client-gameplay` | Nút Đầu Hàng (Surrender) | Thiếu `SurrenderRequest` |
| `feature/ui-gameboard` | Nút Đầu Hàng + Nút Ván Mới | Thiếu `SurrenderRequest` + `NewGameRequest` |
| `feature/client-spectator` | Luồng gửi yêu cầu vào xem | Thiếu `JoinSpectatorRequest` |

---

## 1. Thiếu: `JoinSpectatorRequest.cs`

**Ảnh hưởng:** `feature/client-spectator`

**Tình trạng hiện tại:**  
`MessageType.JoinSpectatorRequest` đã có trong Enum. `JoinSpectatorResponse.cs` cũng đã có. **Nhưng class DTO để Client gửi yêu cầu vào xem thì chưa tồn tại.**

**Yêu cầu tạo mới:**  
File: `Code/UDM_16_CaroGame/CaroShared/Contracts/JoinSpectatorRequest.cs`

```csharp
namespace CaroShared.Contracts
{
    public record JoinSpectatorRequest
    {
        /// <summary>Mã phòng mà người dùng muốn vào xem.</summary>
        public string RoomId { get; init; } = string.Empty;
    }
}
```

**Ghi chú quan trọng:**  
Class `JoinSpectatorResponse` hiện tại dùng field là `Success` (bool), nhưng bên code Client Spectator (NetworkClient.cs) đang gọi là `response.IsSuccess`. Cần sửa một trong hai để khớp nhau. **Đề nghị sửa trong CaroShared**: đổi tên field thành `IsSuccess` cho đồng nhất với convention chung.

```csharp
// Đề nghị đổi từ:
public bool Success { get; init; }
// Thành:
public bool IsSuccess { get; init; }
```

---

## 2. Thiếu: `SurrenderRequest.cs`

**Ảnh hưởng:** `feature/client-gameplay` + `feature/ui-gameboard`

**Tình trạng hiện tại:**  
Enum `MessageType` **chưa có** `SurrenderRequest`. Class DTO cũng **chưa có**.

**Yêu cầu bổ sung vào Enum:**  
File: `Code/UDM_16_CaroGame/CaroShared/Enums/MessageType.cs`

```csharp
// Thêm vào phần "Trong trận đấu":
SurrenderRequest,   // Client gửi yêu cầu đầu hàng
// Không cần SurrenderResponse — Server sẽ broadcast GameOverEvent cho cả 2 bên
```

**Yêu cầu tạo mới:**  
File: `Code/UDM_16_CaroGame/CaroShared/Contracts/SurrenderRequest.cs`

```csharp
namespace CaroShared.Contracts
{
    public record SurrenderRequest
    {
        /// <summary>Mã phòng đang diễn ra trận đấu.</summary>
        public string RoomId { get; init; } = string.Empty;

        /// <summary>Nickname của người chơi chủ động đầu hàng.</summary>
        public string PlayerId { get; init; } = string.Empty;
    }
}
```

**Yêu cầu xử lý Backend (CaroServer):**  
Trong `TcpServerManager.cs`, bắt `MessageType.SurrenderRequest` và:
1. Xác định người thắng (người còn lại sau khi đối thủ đầu hàng).
2. Lưu kết quả vào DB nếu có (`MatchHistoryRepository`).
3. Broadcast `GameOverEvent` tới **cả hai người chơi và toàn bộ khán giả** trong phòng, với payload là lý do kết thúc, ví dụ: `"Người chơi [Tên] đã đầu hàng!"`.

---

## 3. Thiếu: `NewGameRequest.cs`

**Ảnh hưởng:** `feature/ui-gameboard`

**Tình trạng hiện tại:**  
Enum `MessageType` **chưa có** `NewGameRequest`. Class DTO cũng **chưa có**.

**Yêu cầu bổ sung vào Enum:**  
File: `Code/UDM_16_CaroGame/CaroShared/Enums/MessageType.cs`

```csharp
// Thêm vào phần "Trong trận đấu":
NewGameRequest,   // Client yêu cầu chơi ván mới trong cùng phòng
NewGameEvent,     // Server broadcast thông báo ván mới bắt đầu cho cả 2 bên
```

**Yêu cầu tạo mới:**  
File: `Code/UDM_16_CaroGame/CaroShared/Contracts/NewGameRequest.cs`

```csharp
namespace CaroShared.Contracts
{
    public record NewGameRequest
    {
        /// <summary>Mã phòng muốn bắt đầu ván mới.</summary>
        public string RoomId { get; init; } = string.Empty;
    }
}
```

**Yêu cầu xử lý Backend (CaroServer):**  
Trong `TcpServerManager.cs`, bắt `MessageType.NewGameRequest` và:
1. Reset lại `GameSession` (board trống, đổi lượt bắt đầu hoặc giữ nguyên).
2. Broadcast `NewGameEvent` (hoặc `GameStateUpdate`) tới cả 2 người chơi để Client gọi `ResetBoard()`.

---

## Checklist xác nhận hoàn thành

- [ ] Tạo `JoinSpectatorRequest.cs` trong `CaroShared/Contracts`
- [ ] Đổi tên field `Success` → `IsSuccess` trong `JoinSpectatorResponse.cs`
- [ ] Thêm `SurrenderRequest` vào `MessageType` enum
- [ ] Tạo `SurrenderRequest.cs` trong `CaroShared/Contracts`
- [ ] Viết xử lý `SurrenderRequest` trong `TcpServerManager.cs` + broadcast `GameOverEvent`
- [ ] Thêm `NewGameRequest` + `NewGameEvent` vào `MessageType` enum
- [ ] Tạo `NewGameRequest.cs` trong `CaroShared/Contracts`
- [ ] Viết xử lý `NewGameRequest` trong `TcpServerManager.cs` + broadcast `NewGameEvent`
- [ ] Merge vào `develop` và thông báo cho các nhánh Client kéo về

---

*Sau khi Dev Shared hoàn thành checklist này và merge vào develop, các Dev phụ trách Client sẽ pull code về và hoàn thiện phần gửi gói tin ở các nút tương ứng trong `GameBoardForm.cs` và `LobbyForm.cs`. Mọi logic UI và event handling phía Client đã được chuẩn bị sẵn sàng.*
