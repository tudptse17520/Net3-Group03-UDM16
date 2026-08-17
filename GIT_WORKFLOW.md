### Quy tắc Git

Nhóm sử dụng mô hình Git Flow với 2 nhánh chính:

**main**

└── Chỉ chứa phiên bản ổn định, đã được kiểm thử

**develop**

└── Nhánh tích hợp các tính năng đang phát triển

Mỗi task được phát triển trên một `feature branch`, tạo từ `develop`:

`feature/<ten-tinh-nang>`

Ví dụ:

```text
develop
   ├── feature/game-engine
   ├── feature/client-gameplay
   ├── feature/server-spectator
   └── feature/reconnect-logic
```

### Luồng phát triển

```text
develop
   ↓
Tạo feature branch
   ↓
Code + Dev Test
   ↓
Cập nhật code mới nhất từ develop
   ↓
Push feature branch
   ↓
Tester kiểm thử feature
   ↓
Test Passed
   ↓
Merge feature → develop
```

`develop` là nơi tích hợp các tính năng của nhóm. Khi cần phát hành phiên bản ổn định:

```text
develop
   ↓
Integration Test
   ↓
Merge → main
```

### Quy tắc khi cập nhật code

Trong quá trình phát triển, thành viên cần thường xuyên cập nhật `develop` vào feature branch để giảm conflict. Khi xảy ra conflict, **ưu tiên code từ** **`develop`** vì đây là nhánh tích hợp hiện tại của nhóm. Tinh thần này được giữ từ quy định của giảng viên về việc thường xuyên lấy code từ nhánh chuẩn để hạn chế conflict.

### Nguyên tắc chính

* **Một task → một feature branch.**
* **Một feature branch → một người phụ trách chính.**
* `develop` dùng để tích hợp và kiểm thử các tính năng.
* `main` chỉ nhận code đã ổn định và được kiểm thử.
* Không merge feature trực tiếp vào `main`.
* Chỉ merge feature vào `develop` sau khi Dev đã tự test và Tester xác nhận đạt yêu cầu.
* Các thay đổi trong `CaroShared` phải được thống nhất trước khi các thành viên tiếp tục phát triển tính năng liên quan.

