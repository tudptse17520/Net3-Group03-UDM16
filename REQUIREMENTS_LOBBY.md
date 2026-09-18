# Yêu cầu bổ sung LobbyForm để hoàn thiện các nhánh Client

Tài liệu này chỉ ghi lại những thay đổi **bắt buộc** trong `LobbyForm` liên quan trực tiếp đến 4 nhánh đang phát triển.

---

## Tổng quan

| Nhánh | Cần Lobby làm gì | Trạng thái |
|---|---|---|
| `feature/client-spectator` | Thêm nút/menu "Xem Trận" gửi `JoinSpectatorRequest` | ❌ Chưa có |
| `feature/client-gameplay` | Bắt `ChallengeResponse` từ Server để mở `GameBoardForm` | ❌ Chưa kết nối |
| `feature/ui-gameboard` | Bắt `ChallengeResponse` từ Server để mở `GameBoardForm` | ❌ Chưa kết nối |
| `feature/match-history-ui` | `BtnMatchHistory` mở `MatchHistoryForm` | ✅ Đã hoàn chỉnh |

---

## 1. Thêm nút "Xem Trận" — Dành cho `feature/client-spectator`

`NetworkClient` trên nhánh `feature/client-spectator` đã có event `OnSpectatorJoined` và `GameBoardForm` đã có constructor Spectator. **Chỉ còn thiếu điểm kích hoạt từ Lobby.**

### Bước 1 — Thêm vào `LobbyForm.Designer.cs`
- Tạo `ContextMenuStrip` tên `CmsRoomActions`.
- Thêm `ToolStripMenuItem` tên `TsmSpectate`, text = `"👁️ Xem trận"`.
- Gắn `CmsRoomActions` vào `LstRooms.ContextMenuStrip`.

### Bước 2 — Thêm vào `LobbyForm.cs`

```csharp
// Trong Constructor — đăng ký lắng nghe:
CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined += HandleSpectatorJoined;

// Handler gửi yêu cầu khi người dùng chọn "Xem trận":
private void TsmSpectate_Click(object sender, EventArgs e)
{
    if (LstRooms.SelectedItem == null)
    {
        MessageBox.Show("Vui lòng chọn một phòng để vào xem!", "Thông báo",
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    // TODO: Parse RoomId từ chuỗi trong LstRooms theo format dữ liệu thực tế
    string roomId = LstRooms.SelectedItem.ToString() ?? string.Empty;

    var request = new CaroShared.Contracts.JoinSpectatorRequest { RoomId = roomId };
    var msg = new CaroShared.Protocol.NetworkMessage(
        CaroShared.Enums.MessageType.JoinSpectatorRequest, request);
    _ = CaroClient.Network.NetworkClient.Instance.SendMessageAsync(msg);
}

// Handler nhận phản hồi từ Server — mở GameBoardForm ở chế độ Spectator:
private void HandleSpectatorJoined(CaroShared.Contracts.JoinSpectatorResponse response)
{
    if (this.InvokeRequired)
    {
        this.Invoke(new Action(() => HandleSpectatorJoined(response)));
        return;
    }

    // ⚠️ field trong JoinSpectatorResponse hiện là "Success"
    // cần đổi thành "IsSuccess" — xem REQUIREMENTS_SHARED.md
    if (response.IsSuccess && response.Snapshot != null)
    {
        var spectatorForm = new GameBoardForm(response.Snapshot);
        this.Hide();
        spectatorForm.ShowDialog();
        this.Show();
    }
    else
    {
        MessageBox.Show($"Không thể vào xem: {response.ErrorMessage}", "Lỗi",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

// Trong LobbyForm_FormClosing — hủy đăng ký:
CaroClient.Network.NetworkClient.Instance.OnSpectatorJoined -= HandleSpectatorJoined;
```

---

## 2. Mở `GameBoardForm` khi vào phòng — Dành cho `feature/client-gameplay` + `feature/ui-gameboard`

`BtnJoinRoom_Click` hiện tại chỉ hiện `MessageBox`. Cần lắng nghe phản hồi từ Server để mở `GameBoardForm`.

> **Lưu ý phụ thuộc:** `NetworkClient` hiện chưa có event `OnChallengeAccepted`. Dev phụ trách Lobby cần phối hợp với Dev Shared/Network để bổ sung event này (tương tự cách `OnSpectatorJoined` đã được làm ở nhánh `feature/client-spectator`).

```csharp
// Trong Constructor — đăng ký lắng nghe (sau khi NetworkClient có event):
// CaroClient.Network.NetworkClient.Instance.OnChallengeAccepted += HandleChallengeAccepted;

// Handler mở GameBoardForm khi đối thủ chấp nhận thách đấu:
private void HandleChallengeAccepted(/* kiểu do Dev Shared định nghĩa */ object gameStartInfo)
{
    if (this.InvokeRequired)
    {
        this.Invoke(new Action(() => HandleChallengeAccepted(gameStartInfo)));
        return;
    }

    var gameForm = new GameBoardForm(/* truyền thông tin trận đấu */);
    this.Hide();
    gameForm.ShowDialog();
    this.Show();
}

// Trong LobbyForm_FormClosing — hủy đăng ký:
// CaroClient.Network.NetworkClient.Instance.OnChallengeAccepted -= HandleChallengeAccepted;
```

---

## Checklist

- [ ] Thêm `ContextMenuStrip` "Xem trận" vào `LstRooms` (Designer + Code)
- [ ] Đăng ký `OnSpectatorJoined` → mở `GameBoardForm(snapshot)` ở chế độ Spectator
- [ ] Đăng ký `OnChallengeAccepted` (khi có) → mở `GameBoardForm` ở chế độ chơi
- [ ] Hủy đăng ký tất cả event trong `LobbyForm_FormClosing`
