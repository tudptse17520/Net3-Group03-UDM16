# Báo cáo bàn giao Caro

> Đây là báo cáo đợt trước. Kết quả network và bộ cài mới nhất nằm trong [NETWORK_RELEASE_REPORT.md](NETWORK_RELEASE_REPORT.md).

Ngày kiểm tra: 23/09/2026. Yêu cầu mới nhất **Caro / C A R O** thay thế tên Tic Tac Toe trong yêu cầu trước.

## PROJECT ANALYSIS

Solution `Code/UDM_16_CaroGame/UDM_16_CaroGame.slnx` gồm WinForms client .NET 10, TCP server .NET 10 và thư viện giao thức dùng chung. Trận đấu, luật thắng, đổi lượt, timer 30 giây và thời gian kết nối lại 60 giây do server quyết định. Lịch sử dùng EF Core/SQL Server. Không thay GameEngine, luật chơi hoặc cấu trúc database.

## BRANDING

- Application name: **Caro**.
- UI title: **C A R O**; các cửa sổ đăng nhập/sảnh/lịch sử/khán giả có thêm mô tả chức năng.
- EXE: **Caro.exe**, metadata Product/Title Caro.
- Installer: **CaroSetup.exe**; mục gỡ cài đặt và shortcut tên Caro.
- Đã quét mã UI: không còn tên Tic Tac Toe hoặc CARO ONLINE. Giữ namespace `CaroClient` và đường dẫn thiết lập cũ vì đây là chi tiết nội bộ, giúp bảo toàn cấu hình đã có.

## ICON

- Source image used: ảnh đính kèm `codex-clipboard-bd305615-99f6-4141-b6db-e7fa2522955a.png`.
- Bản lưu nguyên gốc: `CaroClient/Assets/Branding/caro-source.png`.
- SHA-256 ảnh gốc và bản lưu cùng là `6A1D76DA168CD9EE115E8E0811EBD002EDD4C08AB69E2DF14CC56A8A3CD48927`.
- Generated ICO: `CaroClient/Assets/Branding/caro.ico`.
- Sizes: 16, 24, 32, 48, 64, 128, 256 px.
- Applied to: EXE, titlebar các form, splash, Setup EXE, shortcut và mục gỡ cài đặt.
- Chỉ chuyển đổi định dạng/kích thước bằng Pillow; không vẽ lại, đổi màu, crop hay thêm chữ lên ảnh.

## SPLASH SCREEN

- File/Class: `CaroSplashForm.cs`, quản lý vòng đời bởi `CaroApplicationContext.cs`.
- Layout: nền be, ảnh gốc 180 px chế độ Zoom, tiêu đề C A R O, trạng thái tiếng Việt và thanh tiến độ nâu.
- Loading stages: đọc cấu hình → chuẩn bị thiết lập giao diện → kiểm tra/khởi động server cục bộ nếu cần → đăng nhập.
- Không thêm delay giả. Công việc chờ server chạy ngoài UI thread; lỗi có nút thử lại hoặc chọn máy chủ.
- Đã sửa và kiểm tra việc giải phóng ApplicationContext nhiều lần để đóng cửa sổ cũng thoát tiến trình sạch.

## NETWORK

- Local mode: mặc định `127.0.0.1:8888`; server chạy ẩn từ thư mục `Server`.
- Remote mode: đọc Host/Port trong `server-config.json`, có thể đổi trong màn hình đăng nhập. Host từ xa không tự khởi chạy server cục bộ.
- Server behavior: giữ TCP và bind IPv4 `IPAddress.Any`. Named mutex chống chạy trùng theo cổng; named event báo sẵn sàng sau khi listener hoạt động. Không tạo kết nối TCP giả để dò readiness.
- Multi-instance behavior: nhiều Caro.exe dùng chung một server trong phiên Windows. Mở đồng thời hai ứng dụng được kiểm tra; thử chạy thêm server cùng cổng thì tiến trình thứ hai thoát sạch.
- Đóng một ứng dụng giữ server cho ứng dụng còn lại. Bộ gỡ cài đặt gửi lệnh dừng server theo cổng cấu hình.
- Đăng nhập chờ phản hồi thực từ server mới mở sảnh; có timeout và thông báo dễ hiểu.

## FLICKER / UI

- Đã đọc metadata và kiểm tra toàn video 18,5 giây bằng contact sheet 1 ảnh/giây, 93 ảnh lấy mẫu 5 fps và 556 khung phân tích 30 fps.
- Hiện tượng thấy rõ: nền thống kê chớp thành khối chữ nhật ở khoảng 3,8s, 11,8s, 13,9s và 17,0s.
- Nguyên nhân trong mã: panel thống kê không được buffer; các control nền trong suốt phụ thuộc việc vẽ lại parent; nhãn timer/badge vừa được Label mặc định vẽ vừa có custom Paint; hiệu ứng chuyển lượt vô hiệu hóa cả thẻ.
- Sửa: buffer đúng control, nền thống kê ổn định, nhãn chỉ vẽ một lần, chuyển lượt chỉ invalidate viền/bóng; snapshot chỉ invalidate ô thực sự thay đổi. Không tái tạo bàn cờ mỗi tick.
- Test xác nhận countdown không làm repaint vùng thống kê không đổi. Đã xem ảnh render splash, login và bàn cờ ở kích thước thường/tối thiểu/tối đa.
- Lỗi kết quả thắng: định vị cửa sổ trước khi Show; chỉ đồng bộ Visible sau khi Show, tránh mở lại cửa sổ đang hiển thị dưới dạng modal. Có kiểm tra lặp sự kiện, minimize/restore và xác nhận server đến sớm/muộn.

## PROGRESS SYSTEM

- Thẻ người chơi phân biệt PLAYER 1/X và PLAYER 2/O, vẫn giữ nickname và tooltip tên đầy đủ.
- Thanh lượt 30 giây, đồng hồ thời gian ván và banner chuyển lượt dùng dữ liệu timing từ server; timer UI không tự quyết định thua.
- Một animation timer dùng chung cho UI; đồng hồ chỉ đổi chữ khi giây thay đổi. Reconnect dừng đồng hồ lượt nhưng thời gian ván vẫn tiếp tục; kết thúc ván đóng băng thời gian; ván mới tạo timing mới.
- Loading gắn với tác vụ thật: kết nối/đăng nhập, lịch sử, cập nhật/xóa avatar, đề nghị hòa, ván mới và kết nối lại.
- GameOver được gửi trước thao tác lưu database, nên kết quả không phụ thuộc độ trễ SQL.
- Database lịch sử hiện không có cột thời lượng; không thêm số liệu thời gian không có nguồn vào lịch sử.

## FILES MODIFIED

Đường dẫn dưới đây tương đối với `Code/UDM_16_CaroGame/`:

- `CaroClient/CaroClient.csproj`, `Program.cs`: metadata, icon/resource, startup context.
- `CaroClient/LoginForm.cs`, `LoginForm.Designer.cs`: branding, bỏ chi tiết dư trên nút, tiến độ và đăng nhập có ACK.
- `CaroClient/LobbyForm.cs`, `LobbyForm.Designer.cs`: branding, cập nhật danh sách lúc vào sảnh, bỏ popup chặn khi bắt đầu trận.
- `CaroClient/GameBoardForm.cs`, `GameBoardForm.Designer.cs`: result lifecycle, timing, loading, repaint, layout và branding.
- `CaroClient/UIComponents.cs`, `CaroTheme.cs`, `WinCelebrationOverlay.cs`: control vẽ, màu progress, icon form; bảo toàn các thay đổi animation đã có.
- `CaroClient/MatchHistoryForm.cs`, `MatchHistoryForm.Designer.cs`, `PersonalizationForm.cs`: loading/ACK/cancellation và icon/branding.
- `CaroClient/Network/NetworkClient.cs`: timeout kết nối, nối lại máy chủ trước, tách stream/token từng phiên nhận dữ liệu.
- `CaroServer/Program.cs`: server chạy ẩn, singleton, readiness, log và stop; không chặn listener vì SQL chưa sẵn sàng.
- `CaroServer/Core/TcpServerManager.cs`, `Game/GameSession.cs`, `Models/Room.cs`: thời điểm trận/lượt/kết thúc và hạn kết nối lại.
- `CaroShared/Contracts/{ChallengeResponse,GameSessionDto,GameStateDto,MoveMadeEventDto,NewGameEventDto}.cs`: timing bổ sung, giữ giao thức hiện có.

Các file đã bị xóa hoặc sửa bởi người dùng/công cụ khác trước đó được giữ nguyên; không tự khôi phục, commit hoặc push.

## FILES CREATED

- Client: `CaroBranding.cs`, `CaroApplicationContext.cs`, `CaroSplashForm.cs`, `ServerConfiguration.cs`, `LocalServerRuntime.cs`, `server-config.json`.
- UI/timing: `ProgressControls.cs`, `GamePresentationState.cs`, `GameBoardForm.Progress.cs`.
- Shared: `Contracts/GameTimingDto.cs`.
- Tài nguyên: `Assets/Branding/caro-source.png`, `caro.ico`.
- Profile publish `Properties/PublishProfiles/win-x64.pubxml` cho client và server.
- Test projects: `Test/ClientResultSmokeTests`, `Test/ProgressSystemTests`, `Test/ReleaseSmokeTests`.
- Installer: `CaroInstaller.iss`, `Build-Release.ps1`, `Test-Installer.ps1`, `README.md`, báo cáo này.

## BUILD

**Success** — build Release solution cuối: 0 warnings, 0 errors. Log: `artifacts/final-build.log`.

## PUBLISH

**Success** — Release, win-x64, self-contained, single-file cho từng executable, không trimming WinForms.

- Output: `artifacts/publish/Caro/Caro.exe`.
- Server đi kèm: `artifacts/publish/Caro/Server/CaroServer.exe`.
- Portable cần gửi cả thư mục `artifacts/publish/Caro`, không chỉ client EXE.
- SHA-256 Caro.exe: `0A942D64A1B29E1375E6BC8C9AE74192A75F3E4DC116F6D22CEC4DA4D9F765BE`.

## INSTALLER

**Success** — đã biên dịch thực bằng Inno Setup 6.7.3, không chỉ tạo file cấu hình.

- Output: `artifacts/installer/CaroSetup.exe` (~62 MiB).
- Wizard modern, cài theo người dùng, shortcut Start Menu và tùy chọn Desktop, uninstaller có icon Caro.
- Đã cài vào thư mục kiểm thử riêng, chạy hai ứng dụng từ bản cài, sau đó gỡ thành công.
- Log biên dịch bản cuối: `artifacts/installer-build.log`. Log kiểm tra: `artifacts/installer-tests.log`.

## TESTS

| Hạng mục | Kết quả |
|---|---|
| App start | PASS: hai EXE publish cùng chạy và thoát sạch |
| Splash | PASS: ảnh/icon/title, render và chuyển sang đăng nhập |
| Local Client 1 / 2 | PASS: hai màn hình đăng nhập dùng đúng một server |
| Login ACK | PASS: form thật giữ loading đến khi server xác nhận, rồi mở sảnh |
| Remote configuration | PASS: config được đưa vào login, host remote không mở server local |
| Gameplay | PASS qua TCP: X/O, thắng, hòa chấp nhận/từ chối, đầu hàng, ván mới |
| Network | PASS: reconnect giữ trận; kết nối qua IP LAN của máy thử |
| Turn timeout | PASS: chờ thực 30 giây, server xử thua đúng bên |
| Disconnect timeout | PASS: chờ thực 60 giây, server xử thua người mất kết nối |
| Result UI | PASS 5 trường hợp: ownership/lặp/minimize/restore, thắng xác nhận sớm/muộn, đối thủ thắng, hòa |
| Progress/UI | PASS: thời gian ván/lượt tách biệt, freeze/reset, sự kiện trùng, tên dài, bố cục và repaint |
| Avatar/history | PASS phản hồi mạng, xử lý lỗi avatar và lịch sử khi database không hoạt động |
| Installer | PASS: file, shortcut Start Menu/icon, mục gỡ cài đặt, hai ứng dụng dùng một server |
| Uninstall | PASS: xóa executable/shortcut/registration, dừng server; giữ cấu hình người dùng |

Test gameplay dùng các TCP peer tự động; không khẳng định đã click chơi toàn bộ trận bằng tay trên hai cửa sổ. Bộ test dùng cổng riêng và SQL endpoint cố ý không tồn tại để tránh ghi vào database thật.

## REMAINING ISSUES

- Chưa có địa chỉ server Internet công khai và hai máy khác mạng để kiểm tra WAN thực tế. Đường kết nối TCP qua LAN và nhánh cấu hình remote đã qua kiểm tra.
- Chưa xác nhận lưu/đọc lịch sử với SQL Server thật. Bản cài không kèm SQL Server; cần cấu hình database trên máy chủ nếu sử dụng lịch sử bền vững.
- Máy thử ở DPI 120 (125%); chưa xác nhận trực tiếp ở DPI 100%/150% hoặc các máy khác.
- Caro.exe và CaroSetup.exe chưa có chữ ký phát hành của chủ dự án.

Hướng dẫn sử dụng và build lại: [README](README.md).
