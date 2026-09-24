# Báo cáo sửa kết nối và phát hành Caro — 23/09/2026

ROOT CAUSE:
- Cấu hình mặc định `127.0.0.1:8888` + tự mở server local làm hai máy cài riêng kết nối tới hai server riêng. Đây là nguyên nhân cấu hình xác định từ source; chưa tái hiện trên hai máy vật lý vì không có thiết bị thứ hai.
- Registry dùng gán đè theo nickname, không từ chối tên trùng. Test thêm trước sửa đã FAIL ở trường hợp này (`artifacts/presence-before.log`); sau sửa PASS. Tên được giữ atomically, so sánh không phân biệt hoa/thường, phiên cũ không bị thay.
- Client phát tín hiệu login thành công trước khi cập nhật snapshot; đã đổi thứ tự để snapshot/token sẵn sàng trước khi mở sảnh.
- Sau trận, UI về sảnh nhưng không gửi yêu cầu rời phòng tới server; registry sảnh không được khôi phục. Kiểm tra EXE thật phát hiện tên cũ còn tồn tại sau đóng app. Đã nối lại luồng rời phòng bằng semantics `SurrenderRequest` sau trận vốn có, thêm lại người về sảnh và phát snapshot; không thêm giao thức mới.
- Các lần gửi đồng thời trên cùng socket chưa được serialize; đã thêm khóa gửi từng session và decoder UTF8 liên tục phía client. Connection chưa login không được thực hiện thao tác game.

WHY TWO MACHINES DID NOT SEE EACH OTHER:
- Loopback chỉ chính máy đang dùng. Hai người phải cùng kết nối tới một máy chủ, không phải mỗi máy bật local. Bản sửa cho chọn Local/Remote rõ ràng và hiển thị endpoint trong sảnh; không thể biến hai server riêng thành một bằng việc chỉ cài cùng EXE.

PROJECT ARCHITECTURE:
- Client/UI: WinForms .NET10, splash → đọc config → chọn mode → TCP → LoginResponse + snapshot → lobby → invite → board.
- Server: TCP .NET10, bind IPv4 `0.0.0.0`, quản lý session/lobby/room, server quyết định nước đi/lượt/thắng/timer/reconnect. SQL Server qua EF Core dùng cho lịch sử.
- Protocol: giữ TCP JSON phân dòng, MessageType/DTO hiện có; bổ sung mã lỗi UsernameInUse. Không thay GameEngine, luật hoặc schema database.
- Config: `server-config.json` cạnh EXE; host/port đổi ngay trên UI, lựa chọn áp dụng cho lần kết nối hiện tại. Server SQL ở `Server/appsettings.json`.

BRANDING:
- App name: Caro.
- UI name: C A R O (yêu cầu mới nhất thay tên Tic Tac Toe trước đó).
- EXE: Caro.exe.
- Installer: CaroSetup.exe; Desktop/Start Menu/Uninstall tên Caro.
- Icon: ảnh X/O người dùng cung cấp, nhúng ICO 16/24/32/48/64/128/256 px. SHA256 ảnh nguồn: `6A1D76DA168CD9EE115E8E0811EBD002EDD4C08AB69E2DF14CC56A8A3CD48927`.

SPLASH:
- implementation: CaroApplicationContext/CaroSplashForm, async, cleanup idempotent.
- image: ảnh X/O gốc được nhúng trong assembly.
- progress: đọc config, chuẩn bị giao diện, mở login; chỉ khởi động server sau khi người dùng chọn local và bấm vào game.

LOCAL MODE:
- implementation: “Chơi trên máy này”; chuẩn hóa loopback về IPv4 local. Nhận biết localhost/127.0.0.1/::1/[::1].
- server behavior: hidden process, mutex theo cổng và named event readiness; không dùng TCP probe tạo ghost player.
- 2-instance behavior: PASS; một server, đóng A không dừng server của B; duplicate server process tự thoát.

REMOTE MODE:
- host: IP/DNS thực nhập trên UI; không hard-code Tailscale/VPS.
- port: mặc định 8888, validate 1–65535; không cho đích 0.0.0.0/:: hoặc URL sai định dạng.
- local fallback: không có; config remote hoặc chọn remote không mở server local ngay cả khi thất bại.
- connection UI: timeout TCP/DNS 10 giây, ACK tối đa 12 giây; thông báo tiếng Việt, nút thử lại, không vào lobby trước ACK/snapshot. Không lưu lịch sử endpoint gần đây (tùy chọn chưa triển khai).

PLAYER PRESENCE:
- initial snapshot: PASS; người mới nhận danh sách hiện tại trong LoginResponse trước mở lobby.
- join: PASS; broadcast snapshot, không liệt kê bản thân làm đối thủ; tên trùng bị từ chối.
- leave: PASS; đóng app xóa đúng session; về sảnh sau trận thêm lại, lobby không giữ ghost. Snapshot gửi tới các session đã xác thực.

INVITE:
- routing: PASS; chọn tên thật trong list → ChallengeRequest tới session mục tiêu.
- room creation: PASS; chấp nhận tạo một room, Challenger=X, người nhận=O, cả hai mở board và nhận cùng kết quả. Không cho tạo ván mới nếu một người đã rời room.

NETWORK TESTS:
- Same PC: PASS — hai EXE publish và hai EXE từ bộ cài, một server ẩn.
- LAN: NOT EXECUTED — chưa có hai thiết bị vật lý trong LAN.
- Non-loopback: PASS — TCP gameplay qua `192.168.2.12`; EXE đã cài chọn Remote qua `192.168.2.12:49722`, nhìn thấy người chơi local hai chiều. Cổng khác 8888 được chọn riêng cho test để tránh chiếm cổng người dùng.
- Tailscale: NOT EXECUTED — chưa cài Tailscale/chưa có tailnet hoặc thiết bị thứ hai.
- VPS: NOT EXECUTED — chưa có endpoint/quyền truy cập VPS.
- Public IP: NOT EXECUTED — chưa có public endpoint/port-forward.
- Physical two different networks: NOT EXECUTED — không tuyên bố localhost hay IP card mạng của cùng máy là hai mạng thật. VMware báo 0 VM đang chạy.

GAME TESTS:
- Login: PASS — ACK thực, DNS sai, cổng đóng, remote không phản hồi và không fallback.
- Player list: PASS — snapshot/join/leave/duplicate/return-to-lobby.
- Invite: PASS — UI của hai EXE thật.
- Accept: PASS — UI tạo room/board hai phía.
- Moves: PASS — 9 nước click qua Windows UI Automation, count đồng bộ ở cả hai EXE; thêm TCP tests.
- Turn: PASS — X/O luân phiên, server đổi lượt; timeout thật 30 giây.
- Win: PASS — kết quả thắng/thua, đóng/mở/minimize/restore result; không còn lỗi form visible/ShowDialog.
- Draw: PASS — đề nghị, từ chối, chấp nhận qua TCP; UI result test.
- Disconnect: PASS — xóa tên ở sảnh; pause trong trận và xử thua sau 60 giây thật.
- Reconnect: PASS — TCP khôi phục bằng token và giữ thời gian trận; UI phục hồi trạng thái.

DATABASE DOWN TEST:
- PASS — các suite dùng SQL cố ý không truy cập được `127.0.0.1,1`, không đụng database phát triển. Đăng nhập, danh sách, trận thắng/hòa, rematch/reconnect vẫn chạy. Không khẳng định lưu lịch sử thành công khi SQL down.

FLICKER:
- root cause: panel thống kê trong suốt bị repaint theo timer; label custom còn để lớp mặc định vẽ trùng.
- fix: buffered/opaque stats, custom label paint một lần, invalidate đúng vùng. Test countdown không repaint panel thống kê không liên quan PASS. Đã kiểm tra ảnh UI tại DPI120 (125%); chưa kiểm tra native 100%/150% hoặc màn hình vật lý khác.

FILES MODIFIED:
- Client: LoginForm.cs/Designer, LobbyForm.cs, Network/NetworkClient.cs, CaroApplicationContext.cs, ServerConfiguration.cs, GameBoardForm.cs (accessible cell identifiers); giữ các sửa branding/result/progress/flicker từ đợt trước.
- Server: Core/TcpServerManager.cs, Managers/SessionManager.cs, Managers/LobbyManager.cs, Models/PlayerSession.cs; Shared/Enums/ErrorCode.cs.
- Installer: CaroInstaller.iss (shortcut default), Test-Installer.ps1, README.md, CARO_RELEASE_REPORT.md.
- Tests: ProgressSystemTests/NetworkChecks.cs, LoginFlowChecks.cs; ReleaseSmokeTests/Program.cs, csproj và helper UI.
- Các file người dùng đã xóa/thay đổi trước phiên được giữ nguyên; không sửa trailing whitespace có sẵn trong CaroServer/test_game.py.

FILES CREATED:
- CaroClient/Network/ConnectionDiagnostics.cs — thông báo lỗi và log endpoint/mode/snapshot không ghi token.
- Test/ReleaseSmokeTests/ReleaseUi.cs, GlobalUsings.cs — Windows UI Automation cho EXE thật.
- Installer/NETWORK_GUIDE.md, TAILSCALE_TEST.md, NETWORK_RELEASE_REPORT.md.
- Các tài nguyên branding, splash, publish profiles, progress controls và test suites của đợt trước vẫn được sử dụng.

BUILD:
- PASS — Release solution, 0 warnings, 0 errors; `artifacts/network-solution-build.log`.
- Result suite 5/5 PASS: `artifacts/network-result-final.log`.
- Progress/TCP/UI suite PASS: `artifacts/network-progress-final.log`.
- Published full suite PASS: `artifacts/network-release-final.log`.
- Installed EXE + remote UI + uninstall PASS: `artifacts/network-installer-final.log`.
- `git diff --check` phần sửa đạt; toàn workspace còn whitespace ở test_game.py có sẵn từ trước.

PUBLISH:
- PASS — self-contained Windows x64 client/server.
- Path: `artifacts/publish/Caro/`.

INSTALLER:
- PASS — Inno Setup compile; cài vào thư mục riêng, Desktop mặc định/Start Menu/icon/registry, hai EXE, remote UI; gỡ sạch binary/registry/server của bản thử.
- Shortcut Desktop/Start Menu đã có được sao lưu và khôi phục nguyên byte, kiểm tra SHA256; giữ thiết lập cá nhân.
- Path: `artifacts/installer/CaroSetup.exe`.

INSTALLER FILE:
- Build time: 2026-09-23 22:59:15 +07:00 (timestamp file).
- Size: 65,024,428 bytes (62.01 MiB).
- SHA256: `16358CF5D4A3CBB78137BDBFAD87BD58602F366F0E75AC7E770F4A8F08157BD7`.

CODE SIGNING:
- Not Signed — chưa có chứng thư phát hành của chủ dự án. Chữ ký của bộ biên dịch Inno không phải chữ ký CaroSetup.exe.

MANUAL ACTION STILL REQUIRED:
- Cài bản mới trên hai máy; chạy test LAN/hai mạng thật. Với Tailscale cần người dùng đăng nhập/cho phép hai thiết bị và nhập IP thực tế của máy host.
- Chỉ cấu hình rule firewall/router khi cần, đúng executable và TCP8888; không có thay đổi firewall/router/tài khoản hay mua hạ tầng trong phiên.
- Cấu hình SQL nếu cần lưu lịch sử; ký EXE/installer nếu chủ dự án có chứng thư. Native DPI100%/150% chưa kiểm tra.

RECOMMENDED REAL TWO-NETWORK METHOD:
- Tailscale. Các lựa chọn VPS/Public IP được mô tả trong NETWORK_GUIDE.md; checklist/bằng chứng hai mạng trong TAILSCALE_TEST.md.

EXACT STEPS FOR ME:
1. Mở `artifacts/installer/CaroSetup.exe` và cài trên cả hai máy; giữ chọn Desktop shortcut.
2. Nếu cùng máy: mở hai Caro, chọn local, nhập hai tên và mời nhau.
3. Nếu hai mạng: cài/đăng nhập Tailscale trên cả hai thiết bị, cho phép liên lạc trong tailnet và lấy IP Tailscale thực tế của A.
4. A mở Caro → Chơi trên máy này → tên A → VÀO GAME để server chạy.
5. B mở Caro → Kết nối máy chủ → IP Tailscale của A → 8888 → tên B khác A → VÀO GAME. B không chọn local.
6. Xác nhận hai danh sách, chọn đối thủ → mời → nhận lời → đánh luân phiên. Thử thắng/hòa, về sảnh, mất mạng/kết nối lại theo TAILSCALE_TEST.md.
7. Nếu lỗi, ghi thông báo và lấy log Caro hai phía; kiểm tra server/firewall/quyền tailnet, không đổi B về localhost.
