# Caro — phát hành Windows x64

Bản cài nằm tại `artifacts/installer/CaroSetup.exe`. Shortcut Desktop **Caro** được chọn mặc định. Mở từ Desktop hoặc Start Menu; giao diện hiển thị **C A R O**. Xem [hướng dẫn LAN/remote](NETWORK_GUIDE.md), [kiểm tra hai mạng bằng Tailscale](TAILSCALE_TEST.md) và [báo cáo mới nhất](NETWORK_RELEASE_REPORT.md).

Bản portable nằm tại `artifacts/publish/Caro/`. Khi gửi bản portable, gửi **cả thư mục**, gồm `Caro.exe`, `server-config.json` và thư mục `Server`. Runtime .NET được đóng gói sẵn; người chơi không cần Visual Studio, SDK hay terminal.

## Chơi trên cùng máy

Cấu hình mặc định trong `server-config.json`:

```json
{
  "Host": "127.0.0.1",
  "Port": 8888,
  "AutoStartLocalServer": true
}
```

Mở hai cửa sổ Caro, chọn **Chơi trên máy này**, đăng nhập hai tên khác nhau, gửi và chấp nhận lời mời đấu. Server chỉ khởi chạy khi bấm **VÀO GAME**, không khởi chạy trong splash. Hai ứng dụng dùng chung một server chạy ẩn. Đóng một cửa sổ không làm mất server của cửa sổ còn lại. Server được giữ để tái sử dụng và bộ gỡ cài đặt gửi lệnh dừng server cục bộ.

## Chơi qua máy chủ từ xa

Chọn **Kết nối máy chủ** ngay trên màn hình đăng nhập, nhập IP/DNS thực tế và cổng. Không cần sửa file để chơi từ xa. Có thể đặt mặc định bằng `server-config.json` cạnh `Caro.exe`, ví dụ:

```json
{
  "Host": "dia-chi-may-chu-cua-ban",
  "Port": 8888,
  "AutoStartLocalServer": false
}
```

Thay địa chỉ ví dụ bằng IP hoặc DNS thực tế của server. Lựa chọn giao diện áp dụng cho lần kết nối hiện tại; chưa lưu endpoint gần đây. Remote không tự khởi chạy server local, kể cả khi thất bại; không fallback localhost. Lobby hiển thị endpoint thực tế ở đáy cửa sổ. Chỉ vào sảnh sau ACK và snapshot từ server.

Người quản trị triển khai thư mục `Server` lên máy chủ và chạy `CaroServer.exe 8888`. Server lắng nghe IPv4 trên tất cả card mạng. Để hai máy khác mạng kết nối, địa chỉ máy chủ và cổng TCP phải truy cập được từ cả hai máy; nếu đặt sau router thì cần cấu hình chuyển tiếp cổng phù hợp. Chưa cấu hình hay kiểm thử một server Internet công khai trong lần bàn giao này.

Lịch sử trận đấu vẫn sử dụng SQL Server theo kiến trúc hiện có. Người quản trị cấu hình `Server/appsettings.json`. Việc không có SQL Server không chặn khởi động hoặc chơi trận; lịch sử chỉ được lưu bền vững khi database hoạt động. Bộ cài không tự cài SQL Server.

## Build và kiểm tra

Từ thư mục gốc repository:

```powershell
dotnet build Code/UDM_16_CaroGame/UDM_16_CaroGame.slnx -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File Installer/Build-Release.ps1
dotnet run --project Code/UDM_16_CaroGame/Test/ClientResultSmokeTests
dotnet run --project Code/UDM_16_CaroGame/Test/ProgressSystemTests
dotnet run --project Code/UDM_16_CaroGame/Test/ReleaseSmokeTests -- artifacts/publish/Caro
```

`Build-Release.ps1` publish client/server bằng profile `win-x64`, sau đó dùng Inno Setup để tạo `CaroSetup.exe`. Có thể truyền `-InnoCompiler 'đường-dẫn/ISCC.exe'`. Bộ biên dịch lấy từ [trang tải chính thức Inno Setup](https://jrsoftware.org/isdl.php); bản dùng trong kiểm tra là 6.7.3, chữ ký tải về hợp lệ của Pyrsys B.V.

`ReleaseSmokeTests` sao chép bản publish vào thư mục riêng, dùng cổng trống và database cố ý không tồn tại. Windows UI Automation thao tác hai EXE thật: đăng nhập, danh sách, mời, nhận lời, chín nước đánh, thắng và về sảnh. Test peer TCP kiểm tra thêm hòa, đấu lại, reconnect, timeout thật 30 giây và hết hạn reconnect thật 60 giây qua IP card mạng.

`Installer/Test-Installer.ps1` cài vào thư mục riêng rồi gỡ; kiểm tra shortcut Desktop mặc định, Start Menu, icon, đăng ký uninstall cùng hai EXE đã cài. Script từ chối chạy nếu Caro đã có đăng ký cài đặt; các shortcut có sẵn được sao lưu và khôi phục, kiểm tra SHA256. Không dùng cờ `--installed` của test trên bản cài cá nhân vì test thay cấu hình server/database trong thư mục kiểm thử. Cấu hình và thiết lập cá nhân được giữ khi gỡ.

## Nhật ký và tài nguyên

- Server chạy ẩn: `%LOCALAPPDATA%/Caro/Logs/server-<port>.log`.
- Client: `%LOCALAPPDATA%/Caro/Logs/caro-<PID>.log`; ghi mode, DNS, endpoint, kết quả và số người, không ghi token.
- Thiết lập cá nhân tiếp tục dùng thư mục cũ `%LOCALAPPDATA%/CaroClient` để giữ tùy chỉnh đã có.
- Ảnh gốc: `Code/UDM_16_CaroGame/CaroClient/Assets/Branding/caro-source.png`.
- ICO: cùng thư mục, `caro.ico`; gồm 16, 24, 32, 48, 64, 128, 256 px.
- Video kiểm tra: `artifacts/video-review/`; ảnh render giao diện: `Code/UDM_16_CaroGame/Test/ProgressSystemTests/bin/Debug/net10.0-windows/screenshots/`.

Ứng dụng và bộ cài hiện chưa được ký bằng chứng thư phát hành của chủ dự án.
