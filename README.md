# UDM_16 - Game Caro trực tuyến

## Thành viên

| STT | MSSV | Họ và tên | Vai trò |
|---:|---|---|---|
| 1 | 074203003699 | Đoàn Phạm Thanh Tú | |
| 2 | 087206011726 | Trần Chí Trung | |
| 3 | 001206004489 | Trần Đức Trung | |
| 4 | 051206007634 | Võ Văn Quang Trung | |
| 5 | 080306012223 | Huỳnh Phương Ý Vy | |
| 6 | 052205013900 | Nguyễn Chí Toàn | |

## Giới thiệu

Game Caro nhiều người chơi qua Server, hỗ trợ phòng đấu và đồng bộ trạng thái trận đấu.

## Kiến trúc hệ thống

- Mô hình: Client-Server
- Protocol:
- Port mặc định:
- Cấu trúc message:

## Yêu cầu môi trường

- Hệ điều hành:
- Ngôn ngữ và phiên bản: C# .NET 10 WinForms
- Công cụ hoặc dependency:

## Cài đặt

Mô tả các bước cài đặt dependency và cấu hình môi trường.

## Hướng dẫn chạy

### Server

```text
Lệnh hoặc các bước chạy Server
```

### Client

```text
Lệnh hoặc các bước chạy Client
```

## Cấu hình

Mô tả cách thay đổi IP, port và các tham số mạng. Không ghi password hoặc secret vào repository.

## Chức năng

- [ ] Client kết nối với Server và xem danh sách người chơi đang online.
- [ ] Gửi, chấp nhận hoặc từ chối lời mời thách đấu.
- [ ] Quản lý nhiều trận đấu đồng thời và trạng thái phòng riêng biệt.
- [ ] Kiểm tra tính hợp lệ của nước đi và xác định kết quả thắng/thua/hòa.
- [ ] Giới hạn thời gian lượt đi; xử lý theo luật khi hết giờ.
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
