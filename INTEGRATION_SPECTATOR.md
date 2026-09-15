# Hướng dẫn tích hợp: Chế độ Khán giả (Spectator)

Gửi Dev 1 (Shared) và Dev 3 (Lobby),

Để hoàn thiện luồng tính năng "Vào xem trận" cho Khán giả, vui lòng thực hiện các cập nhật sau vào phần code mà các bạn đang phụ trách:

## 1. Dành cho Dev quản lý `CaroShared` (Dev 1 / Dev 5)

Chúng ta cần có một Class DTO để Client gửi yêu cầu vào xem một phòng cụ thể.
Vui lòng tạo mới file `CaroShared/Contracts/JoinSpectatorRequest.cs` với nội dung sau:

```csharp
namespace CaroShared.Contracts
{
    public record JoinSpectatorRequest
    {
        // Mã phòng mà người dùng muốn vào xem
        public string RoomId { get; init; } = string.Empty;
    }
}
```
*Lưu ý: Enum `MessageType.JoinSpectatorRequest` đã có sẵn trong `MessageType.cs`.*

## 2. Dành cho Dev quản lý `CaroClient/LobbyForm` (Dev 3)

Để người dùng có thể kích hoạt chế độ xem, hãy thêm nút hoặc Menu chuột phải (Context Menu) vào danh sách phòng `LstRooms`.

### Cấu hình giao diện:
- Tạo một `ContextMenuStrip` và gắn vào `LstRooms.ContextMenuStrip`.
- Thêm một item: `TsmSpectate` với chữ "Vào Xem Trận".
- Gắn sự kiện `Click` cho item này.

### Xử lý logic sự kiện (Event Handler):
Trong `LobbyForm.cs`, hãy viết đoạn logic xử lý khi người dùng chọn "Vào Xem Trận":

```csharp
// 1. Đăng ký lắng nghe sự kiện khi khởi tạo LobbyForm:
CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined += HandleSpectatorJoined;

// 2. Logic khi bấm nút/menu "Vào Xem Trận":
private void TsmSpectate_Click(object sender, EventArgs e)
{
    // Giả sử lấy được RoomId từ danh sách đang chọn
    string selectedRoom = LstRooms.SelectedItem?.ToString();
    if (string.IsNullOrEmpty(selectedRoom)) return;
    
    // (Parse roomId từ string danh sách phòng tùy format của bạn)
    string roomId = "101"; 

    // Gửi yêu cầu lên server
    var req = new CaroShared.Contracts.JoinSpectatorRequest { RoomId = roomId };
    var msg = new CaroShared.Protocol.NetworkMessage(CaroShared.Enums.MessageType.JoinSpectatorRequest, req);
    _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
}

// 3. Logic mở form khi nhận phản hồi thành công từ Server:
private void HandleSpectatorJoined(CaroShared.Contracts.JoinSpectatorResponse response)
{
    if (this.InvokeRequired)
    {
        this.Invoke(new Action(() => HandleSpectatorJoined(response)));
        return;
    }

    if (response.IsSuccess && response.Snapshot != null)
    {
        var spectatorForm = new GameBoardForm(response.Snapshot);
        this.Hide();
        spectatorForm.ShowDialog();
        this.Show();
    }
    else
    {
        MessageBox.Show($"Lỗi vào xem: {response.ErrorMessage}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

// 4. Nhớ Hủy đăng ký sự kiện khi đóng Lobby:
// CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined -= HandleSpectatorJoined;
```

Sau khi các bạn hoàn thành những thay đổi trên, tính năng Khán giả sẽ hoạt động 100% trơn tru từ đầu đến cuối!
