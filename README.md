# UDM_16 - Game Caro trực tuyến

## Thành viên

| STT | MSSV | Họ và tên | Vai trò |
|---:|---|---|---|
| 1 | 074203003699 | Đoàn Phạm Thanh Tú | Leader / Core Server & Network |
| 2 | 001206004489 | Trần Đức Trung | Room & Game Logic (Server) |
| 3 | 087206011726 | Trần Chí Trung | Client Lobby & Challenge UI |
| 4 | 051206007634 | Võ Văn Quang Trung | Client GameBoard & Spectator UI |
| 5 | 080306012223 | Huỳnh Phương Ý Vy | Protocol, Reconnect & Spectator Server |
| 6 | 052205013900 | Nguyễn Chí Toàn | Database, Match History & QA |

## Giới thiệu

Game Caro nhiều người chơi qua Server, hỗ trợ phòng đấu và đồng bộ trạng thái trận đấu.

## Kiến trúc hệ thống

- Mô hình: Client-Server
- Protocol: TCP Sockets (thông qua `TcpListener` và `TcpClient`)
- Port mặc định: 8888 (cấu hình trong `NetworkConstants.cs`)
- Cấu trúc message: JSON bọc trong "Network Envelope" (gồm `MessageType`, `RequestId`, và `Payload`). Cắt luồng TCP bằng ký tự `\n`.

## Yêu cầu môi trường

- Hệ điều hành: Windows 10/11
- Ngôn ngữ và phiên bản: C# .NET 10 WinForms
- Công cụ hoặc dependency: 
  - Visual Studio 2026 (thiết kế UI WinForms) và Visual Studio Code
  - SQL Server
  - Entity Framework Core (EF Core)

## Cài đặt

1. Cài đặt .NET 10 SDK và Visual Studio 2026.
2. Cài đặt SQL Server.
3. Clone repository về máy.
4. Chạy lệnh Entity Framework Core (`Update-Database`) trong dự án Server để tạo database từ Code-First.

## Hướng dẫn chạy

### Server

```text
1. Mở UDM_16_CaroGame.sln.
2. Cấu hình Connection String trong project CaroServer.
3. Set CaroServer làm Startup Project và chạy (F5).
```

### Client

```text
1. Set CaroClient làm Startup Project.
2. Chạy nhiều instance (Ctrl + F5) để mô phỏng nhiều người chơi.
3. Kết nối với Server thông qua IP/Port 8888.
```

## Cấu hình

- **Network**: IP và Port được định nghĩa tại `NetworkConstants.cs` hoặc trong file cấu hình.
- **Database**: Cấu hình Connection String trong project Server. Không commit Connection String có chứa mật khẩu thật lên repository.

## Chức năng

- [ ] Client kết nối với Server và xem danh sách người chơi đang online.
- [ ] Gửi, chấp nhận hoặc từ chối lời mời thách đấu.
- [ ] Quản lý nhiều trận đấu đồng thời và trạng thái phòng riêng biệt.
- [ ] Kiểm tra tính hợp lệ của nước đi và xác định kết quả thắng/thua/hòa.
- [ ] Giới hạn thời gian lượt đi; xử lý thua luôn (Timeout Loss) khi hết giờ. Server tự động khóa ván, gán trạng thái Finished và broadcast GameOver.
- [ ] Reconnect (kết nối lại) để tiếp tục trận đấu trong thời gian cho phép.
- [ ] Lưu lịch sử và kết quả các trận đấu.
- [ ] Tham gia phòng với vai trò khán giả (nhận đầy đủ trạng thái lúc vào và cập nhật realtime).
- [ ] Phân biệt vai trò: Khán giả chỉ xem, không tác động đến trận đấu, có thể rời phòng tự do.

## Kiểm thử

- Functional test:
- Test dữ liệu không hợp lệ:
- Test mất kết nối:
- Stress test:
- Performance test:

Bằng chứng kiểm thử lưu tại `Extra/`.

## Demo

- Video: [Public hoặc Unlisted URL]
- Slide: `PPTX/`
- Báo cáo: `DOCX/`

## Giới hạn

Liệt kê chức năng chưa hỗ trợ và giới hạn hiện tại của sản phẩm.
