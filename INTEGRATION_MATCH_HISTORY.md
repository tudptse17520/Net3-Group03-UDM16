# Hướng dẫn tích hợp: Nút "Lịch sử đấu" vào LobbyForm

## Trạng thái hiện tại
- ✅ `MatchHistoryForm.cs` — **Đã hoàn thiện 100%** (giao diện + gọi API thật từ Server)
- ✅ `NetworkClient.cs` — **Đã có** event `OnMatchHistoryReceived` và handler cho `MatchHistoryResponse`
- ❌ `LobbyForm` — **Chưa có** nút để mở form Lịch sử đấu

## Việc cần làm (2 bước)

### Bước 1: Thêm nút trong `LobbyForm.Designer.cs`

Trong hàm `InitializeComponent()`, thêm khai báo và cấu hình Button mới:

```csharp
// Khai báo biến (đầu hàm, cùng chỗ với các Button khác)
BtnMatchHistory = new Button();

// Cấu hình (đặt sau BtnRefresh hoặc BtnLogout)
// BtnMatchHistory
BtnMatchHistory.BackColor = ColorTranslator.FromHtml("#5C3A21");
BtnMatchHistory.Cursor = Cursors.Hand;
BtnMatchHistory.FlatAppearance.BorderSize = 0;
BtnMatchHistory.FlatStyle = FlatStyle.Flat;
BtnMatchHistory.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
BtnMatchHistory.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
BtnMatchHistory.Location = new Point(580, 355);  // Điều chỉnh vị trí Y phù hợp
BtnMatchHistory.Name = "BtnMatchHistory";
BtnMatchHistory.Size = new Size(170, 35);
BtnMatchHistory.Text = "LỊCH SỬ ĐẤU";
BtnMatchHistory.UseVisualStyleBackColor = false;
BtnMatchHistory.Click += BtnMatchHistory_Click;

// Thêm vào Controls (cùng chỗ Controls.Add)
Controls.Add(BtnMatchHistory);
```

Và khai báo field ở cuối file Designer:

```csharp
private Button BtnMatchHistory;
```

### Bước 2: Thêm handler trong `LobbyForm.cs`

Thêm hàm xử lý sự kiện click:

```csharp
private void BtnMatchHistory_Click(object sender, EventArgs e)
{
    var historyForm = new MatchHistoryForm(PlayerName);
    historyForm.ShowDialog();
}
```

### Tùy chọn: Bo góc cho nút

Nếu muốn nút có bo góc giống các nút khác, thêm dòng sau vào constructor `LobbyForm(string playerName)`:

```csharp
BtnMatchHistory.Paint += Button_Paint;
```

## Lưu ý
- Constructor của `MatchHistoryForm` chỉ nhận 1 tham số: `string playerName`
- Form tự động gọi API lấy dữ liệu từ Server khi mở, không cần truyền thêm gì
- Khi đóng form, event sẽ tự động hủy đăng ký (đã xử lý trong `FormClosed`)
