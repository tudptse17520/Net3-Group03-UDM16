# Chơi Caro trên cùng máy, LAN và Internet

## Vì sao hai máy không thấy nhau?

`127.0.0.1`, `localhost`, `::1` chỉ máy đang chạy ứng dụng. Nếu cả hai máy chọn local, mỗi máy có server riêng và danh sách riêng. Cài cùng bộ cài không tạo một máy chủ Internet chung. Hai người cần kết nối **cùng một server đang chạy**, bằng hai tên khác nhau.

Người ngồi tại máy chủ có thể dùng loopback, người từ xa dùng IP LAN/Tailscale: hai địa chỉ khác nhau vẫn hợp lệ nếu đều dẫn tới server đó. Người đang trong trận không hiện trong danh sách đối thủ sẵn sàng. Sau **VỀ SẢNH**, tên người chơi được thêm lại.

## Cùng máy

Mở hai Caro → chọn **Chơi trên máy này** → nhập hai tên → **VÀO GAME**. Hai cửa sổ dùng một server ẩn theo cổng. Đóng một cửa sổ không dừng server của cửa sổ kia.

## Hai máy cùng LAN/Wi-Fi

1. Máy A mở Caro, chọn local, đăng nhập để server TCP8888 chạy.
2. Trên A, vào **Settings → Network & internet → Wi-Fi/Ethernet → Properties → IPv4 address**. Chọn card thực, không chọn VMware/loopback.
3. B chọn **Kết nối máy chủ**, nhập IPv4 của A, cổng `8888`, tên khác A. Không nhập `0.0.0.0`: đây là địa chỉ server lắng nghe, không phải đích kết nối.
4. Nếu firewall chặn, quản trị viên chỉ cho phép đúng `Server/CaroServer.exe`, inbound TCP8888 và phạm vi mạng cần dùng. Không tắt toàn bộ firewall. Wi-Fi khách/AP isolation có thể ngăn thiết bị liên lạc; dùng mạng cho phép hai máy truy cập nhau.
5. Kiểm tra dòng **Máy chủ:** ở đáy sảnh; thử mời, nhận lời và nước đánh trên cả hai bên.

Remote không mở local server và không chuyển về localhost khi thất bại. Lỗi DNS, cổng chưa mở và quá thời gian được hiện bằng tiếng Việt. Kết nối có giới hạn 10 giây, chờ ACK đăng nhập tối đa 12 giây. Chỉ mở sảnh sau ACK cùng snapshot.

## Hai mạng Internet — ưu tiên Tailscale

Làm theo [TAILSCALE_TEST.md](TAILSCALE_TEST.md). Dùng IP Tailscale/MagicDNS **thực tế** của A; không dùng IP ví dụ. Hai máy phải cùng tailnet hoặc được chia sẻ/cho phép truy cập phù hợp. Tailscale có thể truyền qua NAT bằng kết nối trực tiếp hoặc relay; không cần tự mở cổng router theo phương án này. Tham khảo [kết nối thiết bị](https://tailscale.com/docs/how-to/connect-to-devices), [cơ chế kết nối](https://tailscale.com/docs/reference/device-connectivity), [firewall](https://tailscale.com/docs/integrations/firewalls).

Phiên bàn giao chưa có Tailscale, VPS hay máy thứ hai ở mạng khác. Kiểm tra qua IP card mạng ngay trên máy phát triển không phải bằng chứng hai mạng vật lý đã hoạt động.

## VPS Windows

Quản trị viên chép thư mục `Server/` lên VPS Windows và chạy `CaroServer.exe 8888` như tiến trình được giám sát. Cho phép TCP8888 inbound tại firewall Windows và security group của nhà cung cấp. Cả hai client chọn remote và nhập cùng IP/DNS thực tế của VPS. Chưa mua hay triển khai VPS trong phiên này.

## Public IP và chuyển tiếp cổng

Máy A chạy server với IP LAN ổn định. Quản trị viên router chuyển TCP8888 tới IPv4 LAN của A; Windows cho phép đúng executable/cổng. Người ở mạng ngoài dùng public IP/DNS của router. IP LAN không dùng trực tiếp qua Internet.

Nếu có CGNAT/double NAT, chỉ tạo rule trên router nhà có thể không đủ; ưu tiên Tailscale hoặc hỏi nhà mạng về public IP. Chưa thay cấu hình router/firewall. Giao thức hiện có là TCP/JSON, chưa bổ sung TLS; bản demo ưu tiên Tailscale thay vì mở trực tiếp dịch vụ ra Internet.

## Database và hỗ trợ

SQL không hoạt động không chặn đăng nhập/chơi. Muốn lưu lịch sử bền vững, quản trị viên cấu hình `Server/appsettings.json` tới SQL Server hoạt động. Không tự cài SQL.

Khi lỗi, ghi giờ, chế độ, host/cổng và thông báo rồi lấy log `%LOCALAPPDATA%/Caro/Logs/` ở cả hai máy. Client ghi mode/DNS/endpoint/kết quả/snapshot, không ghi session token. Không đổi B về localhost để che lỗi remote.
