# BÁO CÁO KIỂM TOÁN TỔNG THỂ DỰ ÁN CARO ONLINE (GROUP 03 - UDM16)
**Dành cho: Nhóm Phát triển (Dev 1 - Dev 6)**
**Sprint kiểm toán:** 1 - 5

Dựa trên việc kiểm tra chéo 7 tài liệu Google Sheets, 3 báo cáo từ các lập trình viên (Dev 1, Dev 3, Dev 4) và hiện trạng thực tế của toàn bộ các nhánh trên kho lưu trữ Git, dưới đây là kết quả kiểm toán (Read-Only Audit) toàn diện.

---

## 1. TỔNG HỢP LỖI HỆ THỐNG (MASTER ISSUE TABLE)

> [!CAUTION]
> **TÌNH TRẠNG NGHIÊM TRỌNG:** Toàn bộ hệ thống hiện đang bị ngưng trệ ở lớp Network. Các tính năng từ Sprint 3, 4, 5 sẽ không thể hoạt động ổn định cho đến khi các lỗi Chí mạng (Critical) ở lớp Socket/TCP được Merge vào `develop` và đồng bộ xuống tất cả các nhánh con.

| ID | Mức độ | Nhánh / Thành phần bị ảnh hưởng | Mô tả lỗi / Vấn đề | Đề xuất khắc phục |
| :--- | :--- | :--- | :--- | :--- |
| **SYS-01** | 🔴 CRITICAL | Toàn bộ nhánh rẽ từ `develop` | **Ngắt Socket khi Đăng nhập:** Hàm `RemoveSession` trong `SessionManager.cs` mặc định gọi `session.Dispose()`, khiến kết nối của Client bị ngắt ngay khi đổi từ ID tạm sang Nickname thật. | Thêm tham số `dispose = false` vào `RemoveSession` khi đổi ID tại `TcpServerManager`. |
| **SYS-02** | 🔴 CRITICAL | Toàn bộ nhánh rẽ từ `develop` | **Tắc nghẽn gói tin TCP:** Hàm `PlayerSession.SendMessageAsync` thiếu `await Stream.FlushAsync()`, khiến thông điệp nằm lại trong bộ đệm và không tới được Client. | Bổ sung `await Stream.FlushAsync()` vào luồng gửi tin. |
| **SYS-03** | 🔴 CRITICAL | `develop`, `main` và các nhánh con | **Lỗi Parse JSON (Enum):** Client gửi Enum dạng chuỗi nhưng Server mặc định parse dạng số, gây crash `JsonException`. | Khởi tạo `JsonSerializerOptions` chứa `JsonStringEnumConverter` trên toàn bộ Server. |
| **UI-01** | 🟠 HIGH | `feature/client-lobby-ui` | **Hiển thị Lobby (Race Condition):** Phía Client không chủ động yêu cầu `PlayerListRequest` và không cache kịp thời danh sách lúc form chưa nạp xong. | Đăng ký Load Event, thêm nút Refresh gọi `SendGetPlayerListAsync()`, cập nhật bằng `BeginInvoke`. |
| **INT-01** | 🟡 MEDIUM | `feature/match-history-ui` | **Chưa tích hợp thật Lịch sử đấu:** Hiện đang dùng Mock Data. Yêu cầu update `CaroShared` và thêm logic vào `CaroServer` nhưng chưa ai xử lý (Backend chưa merge). | Dev Backend cần tạo `MatchHistoryRequest/Response` và viết hàm truy vấn Database. |
| **INT-02** | 🟡 MEDIUM | Tất cả | **Phân mảnh mã nguồn:** Fix cho SYS-01, 02, 03 hiện đang nằm rải rác trên `feature/server-challenge` và chưa được Merge chính thức về `develop`. | Yêu cầu tạo PR khẩn cấp từ `feature/server-challenge` vào `develop` và rebase toàn bộ nhánh. |

---

## 2. MA TRẬN TÍCH HỢP (INTEGRATION MATRICES)

### A. Ma trận đồng bộ Nhánh (Branch Synchronization Matrix)

| Lập trình viên | Nhánh chức năng | Tình trạng Code | Tích hợp vào Develop? | Tích hợp vào Main? | Trạng thái tích hợp |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Dev 1** | `feature/server-challenge` | Chứa bản vá lỗi mạng cực kỳ quan trọng | Đang chờ PR / Chưa đồng bộ | Chưa | ⚠️ **Cần Merge Khẩn Cấp** |
| **Dev 2** | `feature/error-handling` | Clean, Pass Local Runtime Test | Chưa | Chưa | ⏳ Chờ Rebase sau khi vá mạng |
| **Dev 3** | `feature/ui-lobby` | Đã nhận diện lỗi và có report đề xuất | Cũ / Lỗi mạng | Chưa | ❌ Dính bẫy lỗi Socket |
| **Dev 4** | `feature/match-history-ui` | UI hoàn chỉnh, đang dùng Mock Data | Chưa | Chưa | ⏸️ Bị block bởi Backend API |
| **Dev 5** | `feature/protocol-serializer` | Có serializer cơ bản | Đã merge `develop` | Có | ⚠️ Thiếu StringEnumConverter |
| **Dev 6** | `test/mvp-integration` | Test Harness cơ bản | Chưa | Chưa | ⏸️ Bị block do TCP Crash |

### B. Ma trận Cấu trúc Chia sẻ (CaroShared DTO Matrix)

| Request / Response | Sprint | Trạng thái Client | Trạng thái Server | Trạng thái CaroShared |
| :--- | :--- | :--- | :--- | :--- |
| `LoginRequest/Response` | Sprint 2 | Hoàn thành | Hoàn thành (Lỗi Enum) | Đã có |
| `PlayerListRequest/Response` | Sprint 2 | Đề xuất thêm mới | Đề xuất thêm mới | **Thiếu** |
| `MakeMoveRequest` | Sprint 3 | Hoàn thành UI | Xong GameEngine | Đã có |
| `JoinSpectatorRequest` | Sprint 4 | Đang làm | Đang làm | Đã có |
| `MatchHistoryRequest/Response` | Sprint 5 | Chờ API | Chưa làm | **Thiếu** |

---

## 3. KẾT LUẬN & ĐÁNH GIÁ TỔNG QUAN (FINAL PROJECT VERDICT)

> [!WARNING]
> **VERDICT: BLOCKED - CẦN GIẢI QUYẾT XUNG ĐỘT TÍCH HỢP**

Hệ thống Caro Online của nhóm hiện tại đã phát triển rất nhiều tính năng đến tận Sprint 5 (Lịch sử đấu, Xem khán giả, Bắt lỗi, Khôi phục kết nối). Tuy nhiên, **Nền móng Network (Sprint 1 & 2) đang bị nứt gãy**. 

**Quy trình giải quyết bắt buộc:**

1. **Đóng băng tính năng mới (Feature Freeze):** Các Dev (4, 5, 6) tạm dừng commit các tính năng nhánh nhánh ngoài lề.
2. **Hội quân tại `develop`:** Trưởng nhóm (Dev 1) phải review và Merge ngay lập tức bản fix từ `feature/server-challenge` (của Report Dev 3 và Dev 1) vào `develop`. Bản fix này bao gồm:
   - Sửa `Dispose()` trong `RemoveSession`.
   - Bổ sung `FlushAsync()`.
   - Cài đặt `JsonStringEnumConverter`.
3. **Đồng bộ toàn đội (Rebase/Merge):** Sau khi `develop` đã ổn định mạng, TẤT CẢ các Dev từ 1 đến 6 phải chạy:
   ```bash
   git fetch origin
   git merge origin/develop
   ```
4. **Xử lý Mock Data:** Backend (Dev 1 / Dev 2) cập nhật `CaroShared` cho tính năng Lịch sử trận đấu (`feature/match-history-ui`) để Dev 4 có thể gỡ Mock Data.
5. **Integration Test toàn diện:** Mới có thể tiến hành test luồng GameEngine (Thách đấu -> Chơi cờ -> Thắng/Thua) một cách trơn tru.

Dự án có cấu trúc tốt, phân chia công việc rõ ràng qua Google Sheets và Report, nhưng đang mắc lỗi điển hình của làm việc nhóm song song: **Không tích hợp sớm (Continuous Integration) các bản vá lõi**. Sửa xong lớp Network, tiến độ các Sprint 3, 4, 5 sẽ tự động được thông suốt.
