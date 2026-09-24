# Kiểm tra Caro trên hai mạng bằng Tailscale

**Trạng thái: NOT EXECUTED trên hai mạng vật lý.** Máy phát triển chưa cài Tailscale; không có endpoint hay máy thứ hai được cung cấp. Không hard-code IP Tailscale ví dụ trong ứng dụng.

## Chuẩn bị

1. Cài CaroSetup.exe mới nhất trên hai máy Windows x64.
2. Cài Tailscale từ [nguồn chính thức](https://tailscale.com/download/windows), đăng nhập cả hai máy vào cùng tailnet hoặc thiết lập chia sẻ/quyền truy cập phù hợp. Không gửi mật khẩu/token cho người khác.
3. A dùng mạng nhà, B dùng mạng khác, chẳng hạn hotspot điện thoại. Ghi rõ hai mạng để phân biệt với test cùng LAN.
4. Xác nhận hai máy online trong Tailscale. Lấy **IP Tailscale thực tế hoặc MagicDNS của A** theo [hướng dẫn kết nối](https://tailscale.com/docs/how-to/connect-to-devices). Không dùng IP Wi-Fi của A, IP của B hoặc loopback làm địa chỉ remote.

## Thực hiện

1. A mở **Caro** → **Chơi trên máy này** → tên A → **VÀO GAME**. Server ẩn bắt đầu lắng nghe TCP8888.
2. B mở **Caro** → **Kết nối máy chủ** → IP Tailscale thực tế của A → cổng `8888` → tên khác A → **VÀO GAME**.
3. Nếu chính sách tailnet hạn chế, quản trị viên cho phép B truy cập A:TCP8888. Nếu Windows firewall chặn, quản trị viên tạo rule inbound cho **đúng CaroServer.exe**, TCP8888 và IP Tailscale của B; không tắt toàn bộ firewall. Xem [tài liệu firewall](https://tailscale.com/docs/integrations/firewalls).
4. A thấy B và B thấy A. A có thể hiện endpoint loopback, B hiện IP Tailscale: hai địa chỉ dẫn tới cùng server A.
5. A chọn B → mời → B xác nhận. Đánh luân phiên; kiểm tra bàn cờ, lượt và đếm nước hai bên.
6. Chơi đến thắng; thử ván mới và hòa. Sau **VỀ SẢNH**, cả hai phải lại thấy nhau và gửi lời mời mới được.
7. B ngắt mạng ngắn hơn 60 giây rồi kết nối lại; kiểm tra khôi phục trận. Thử ngắt quá hạn để kiểm tra xử thua. Đóng B ở sảnh; A phải xóa B khỏi danh sách.
8. Ghi giờ, host/cổng, mode, thông báo và thu log `%LOCALAPPDATA%/Caro/Logs/` hai phía. Không đổi sang localhost ở B để che lỗi.

## Đọc kết quả

- **Không tìm thấy địa chỉ:** kiểm tra IP/MagicDNS và Tailscale đang kết nối.
- **Chưa nhận kết nối:** A đã bấm vào game ở local chưa, cổng đúng chưa, server còn chạy không.
- **Chưa phản hồi:** kiểm tra tailnet, quyền truy cập và rule Windows trên A. Quản trị viên có thể dùng `Test-NetConnection <IP thực tế của A> -Port 8888` sau khi thay toàn bộ phần trong dấu ngoặc.
- **Tên đang được sử dụng:** đổi tên; không ghi đè phiên đang chơi.
- **Hai sảnh không thấy nhau:** kiểm tra B chọn remote về A, không để cả hai chọn local. Người đang trong trận không nằm trong danh sách sẵn sàng.

Tailscale dùng đường trực tiếp hoặc relay tùy NAT; cả hai có thể vận chuyển TCP của game. Không chỉnh code theo loại đường đi. Xem [device connectivity](https://tailscale.com/docs/reference/device-connectivity).

## Biên bản để đánh dấu PASS thật

| Mục | Ghi nhận thực tế |
|---|---|
| Giờ, SHA256 bộ cài hai máy | Chưa thực hiện |
| Mạng vật lý A / B | Chưa thực hiện |
| IP Tailscale A, cổng | Chưa thực hiện |
| Login, danh sách hai chiều | Chưa thực hiện |
| Mời, chấp nhận, tạo phòng | Chưa thực hiện |
| Nước đi, lượt, thắng/hòa | Chưa thực hiện |
| Mất mạng, reconnect, về sảnh | Chưa thực hiện |
| Video hoặc log hai phía | Chưa thực hiện |

Chỉ đổi trạng thái đầu tài liệu thành PASS sau khi thực hiện trên hai thiết bị ở hai mạng vật lý khác nhau.
