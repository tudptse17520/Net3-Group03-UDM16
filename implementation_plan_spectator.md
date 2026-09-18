# Kế hoạch triển khai Feature: Khán giả (Client Spectator)

Tính năng này cho phép người chơi ở sảnh (Lobby) tham gia vào một phòng đang chơi dưới vai trò Khán giả. Khán giả sẽ nhận được toàn bộ trạng thái bàn cờ hiện tại, danh tính 2 người chơi đang đấu, và liên tục cập nhật các nước đi mới nhất.

## User Review Required

> [!WARNING]
> Kế hoạch này hiện đang tập trung vào thay đổi phía **Client** và các package **Shared**. Để tính năng hoạt động hoàn chỉnh, phía **Server** cũng cần phải xử lý `JoinSpectatorRequest` và broadcast `MoveMadeEvent` / `GameOverEvent` cho các Session của khán giả trong phòng đó. 
> 
> **Câu hỏi cho bạn:** Bạn có muốn tôi thiết kế và triển khai cả phần xử lý trên **Server** (CaroServer) cho tính năng này không, hay bạn muốn tôi chỉ làm phần Client trước?

## Proposed Changes

### 1. CaroShared (Giao thức & DTO)

Chúng ta cần định nghĩa rõ ràng request tham gia xem và mở các MessageType cần thiết.

#### [MODIFY] [MessageType.cs](file:///c:/Users/chitr/source/repos/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Enums/MessageType.cs)
- Mở khóa các MessageType liên quan đến Spectator đã bị comment: `JoinSpectatorRequest`, `JoinSpectatorResponse`.

#### [NEW] `JoinSpectatorRequest.cs` (trong thư mục Contracts)
- Payload dùng khi Client bấm "Vào xem":
```csharp
public record JoinSpectatorRequest
{
    public string RoomId { get; init; } = string.Empty;
}
```

### 2. CaroClient (Mạng & Giao diện)

Chúng ta sẽ mở rộng `NetworkClient` để bắt sự kiện Spectator và cập nhật `GameBoardForm` thành một giao diện "Chỉ xem".

#### [MODIFY] [NetworkClient.cs](file:///c:/Users/chitr/source/repos/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroClient/Network/NetworkClient.cs)
- Thêm các Events:
  - `public event Action<SpectatorStateSnapshotDto>? OnSpectatorJoined;`
  - `public event Action<string>? OnJoinSpectatorFailed;`
- Trong `DispatchMessage`: Parse payload của `JoinSpectatorResponse` thành `SpectatorStateSnapshotDto` và kích hoạt event `OnSpectatorJoined` nếu thành công.

#### [MODIFY] [LobbyForm.cs](file:///c:/Users/chitr/source/repos/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroClient/LobbyForm.cs)
- Thêm một nút mới (Ví dụ: `btnSpectate` - "Vào xem") hoặc thêm logic khi người chơi chọn một phòng có trạng thái `(Đang chơi)` và ấn "Vào xem".
- Gửi `NetworkClient.Instance.SendMessageAsync` mang theo `JoinSpectatorRequest`.
- Lắng nghe `OnSpectatorJoined`. Khi kích hoạt, ẩn `LobbyForm` và mở `GameBoardForm` dưới dạng khán giả.

#### [MODIFY] [GameBoardForm.cs](file:///c:/Users/chitr/source/repos/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroClient/GameBoardForm.cs)
- Thêm cờ `_isSpectator = false`.
- Thêm một Constructor mới cho Spectator: `public GameBoardForm(SpectatorStateSnapshotDto snapshot)`
- **Khi là Spectator (`_isSpectator = true`):**
  - Cập nhật UI: Đổi nút "Đầu hàng" / "Cầu hòa" thành nút "Thoát phòng".
  - Chặn thao tác cờ: Tại hàm `Cell_Click()`, kiểm tra `if (_isSpectator) return;`.
  - Khởi tạo bàn cờ: Gọi `UpdateBoard(snapshot.Session.Board)` để tải trạng thái hiện tại.
  - Tên người chơi: `lblPlayer1Name.Text = snapshot.Room.PlayerX`, tương tự cho PlayerO.
- **Tiếp nhận nước đi:** Tiếp tục dùng hàm `HandleMoveMade` để vẽ X/O lên bàn cờ, nhưng bỏ qua các hộp thoại như "Lỗi gửi nước đi" nếu bạn là khán giả.

## Verification Plan

### Manual Verification
1. Khởi động 1 Server và 3 Client (A, B, C).
2. Client A và B tạo phòng và bắt đầu đánh với nhau được vài nước đi.
3. Client C đăng nhập vào sảnh (Lobby), chọn phòng của A và B, bấm "Vào xem".
4. Client C mở ra Form Bàn cờ:
   - Các nút đánh đã đánh của A và B hiển thị đúng vị trí.
   - Không thể click vào bàn cờ.
   - Nút "Đầu hàng" ẩn đi, thay bằng nút "Thoát phòng".
5. Client A đánh 1 nước mới:
   - Nước cờ ngay lập tức đồng bộ hiển thị sang màn hình của Client C.
6. Khi trận đấu kết thúc, Client C nhận được màn hình thông báo người thắng cuộc/hoà.

---
Vui lòng xem xét các thay đổi đề xuất. Nếu bạn đồng ý hoặc cần điều chỉnh thêm logic trên Server, hãy cho tôi biết!
