# WALKTHROUGH: BỔ SUNG CAROSHARED & CAROSERVER VÀ ĐỒNG BỘ DEVELOP

## Tổng quan kết quả thực hiện
Đã hoàn thành 100% các hạng mục và đồng bộ toàn diện lên GitHub `origin/develop` tại commit [`ff9b68c`](https://github.com/tudptse17520/Net3-Group03-UDM16/commit/ff9b68c):
1. **Bổ sung đầy đủ Enums & Contracts trong `CaroShared`**:
   - [`MessageType.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Enums/MessageType.cs): Bổ sung `SurrenderRequest`, `NewGameRequest`, `NewGameEvent`, `JoinSpectatorRequest`, `JoinSpectatorResponse`.
   - [`JoinSpectatorRequest.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/JoinSpectatorRequest.cs): DTO chứa `RoomId` của phòng muốn xem.
   - [`JoinSpectatorResponse.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/JoinSpectatorResponse.cs): Hỗ trợ cả `IsSuccess` và getter `Success` để tương thích ngược hoàn hảo với mọi nhánh Client.
   - [`SurrenderRequest.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/SurrenderRequest.cs): DTO chứa `RoomId` và `PlayerId` của người đầu hàng.
   - [`NewGameRequest.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/NewGameRequest.cs): DTO chứa `RoomId` và `PlayerId` yêu cầu ván mới.
   - [`NewGameEventDto.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/NewGameEventDto.cs): Payload broadcast sự kiện ván mới cho Client.
   - **Quy chuẩn comment:** Tất cả comment trong các file DTO mới đều được viết theo định dạng `//` đơn giản, rõ ràng, phù hợp cho sinh viên.

2. **Cập nhật Backend `CaroServer`**:
   - [`Room.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Models/Room.cs): Thêm `ResetSession()` để khởi tạo ván cờ và timer mới.
   - [`RoomManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Managers/RoomManager.cs): Thêm `ResetRoom(roomId)` tự động reset session và khởi động lại `TurnTimer`.
   - [`TcpServerManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Core/TcpServerManager.cs):
     - `HandleSurrenderAsync`: Nhận yêu cầu đầu hàng, xác định đối thủ thắng, dừng timer, lưu DB và broadcast `GameOverEvent` với payload `MoveMadeEventDto` để Client hiển thị popup kết quả.
     - `HandleNewGameAsync`: Nhận yêu cầu ván mới, reset phòng qua `_roomManager.ResetRoom(roomId)`, và broadcast `NewGameEvent` cho cả 2 bên.
     - `HandleJoinSpectatorAsync`: Thêm khán giả vào phòng, tạo snapshot `SpectatorStateSnapshotDto` và trả về `JoinSpectatorResponse`.
     - Giữ Room mở sau khi ván kết thúc (không hủy phòng ngay) để 2 bên có thể bấm "Ván mới" (rematch). Khi người chơi thoát hoặc ngắt kết nối, phòng mới được giải phóng.

3. **Đồng bộ PR #42 (`feature/client-spectator`) & Gộp commit sạch sẽ**:
   - Nhánh `feature/client-spectator` đã được merge vào `develop` trên GitHub.
   - Đã đồng bộ `origin/develop` về, giải quyết xung đột sạch sẽ.
   - Đã gộp (amend/squash) commit chuẩn hóa style comment trực tiếp vào commit merge, xóa bỏ hoàn toàn vết tích commit lẻ tẻ và force-push commit sạch đẹp `ff9b68c` lên `origin/develop`.

---

## Chi tiết các thay đổi

### 1. `CaroShared`
| File | Loại thay đổi | Chi tiết |
|---|:---:|---|
| [`MessageType.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Enums/MessageType.cs) | MODIFY | Bổ sung các MessageType cho ván đấu và khán giả |
| [`JoinSpectatorRequest.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/JoinSpectatorRequest.cs) | NEW | DTO yêu cầu vào xem phòng (comment `//`) |
| [`JoinSpectatorResponse.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/JoinSpectatorResponse.cs) | NEW | DTO kết quả vào xem (kèm snapshot, hỗ trợ cả `IsSuccess` & `Success`, comment `//`) |
| [`SurrenderRequest.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/SurrenderRequest.cs) | NEW | DTO người chơi gửi yêu cầu đầu hàng (comment `//`) |
| [`NewGameRequest.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/NewGameRequest.cs) | NEW | DTO người chơi gửi yêu cầu bắt đầu ván mới (comment `//`) |
| [`NewGameEventDto.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroShared/Contracts/NewGameEventDto.cs) | NEW | Payload broadcast sự kiện ván mới cho Client (comment `//`) |

### 2. `CaroServer`
| File | Phương thức / Vị trí | Mục đích |
|---|---|---|
| [`Room.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Models/Room.cs) | `ResetSession()` | Hủy session cũ và tạo `GameSession` mới tinh cho phòng |
| [`RoomManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Managers/RoomManager.cs) | `ResetRoom(string roomId)` | Reset phòng và khởi động lại đếm giờ `TurnTimer` |
| [`TcpServerManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Core/TcpServerManager.cs) | `HandleSurrenderAsync` | Xử lý logic đầu hàng, lưu DB, broadcast `GameOverEvent` |
| [`TcpServerManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Core/TcpServerManager.cs) | `HandleNewGameAsync` | Xử lý yêu cầu ván mới và broadcast `NewGameEvent` |
| [`TcpServerManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Core/TcpServerManager.cs) | `HandleJoinSpectatorAsync` | Xử lý khán giả xin xem và gửi snapshot bàn cờ |
| [`TcpServerManager.cs`](file:///d:/UTH4/Net/Net3-Group03-UDM16/Code/UDM_16_CaroGame/CaroServer/Core/TcpServerManager.cs) | `OnRoomTimeout` & `HandleMakeMoveAsync` | Giữ Room mở sau khi ván kết thúc để người chơi rematch |

---

## Kết quả kiểm thử Build

Lệnh kiểm tra toàn bộ Solution:
```bash
dotnet build Code/UDM_16_CaroGame/UDM_16_CaroGame.slnx
```

**Kết quả:**
```
  Determining projects to restore...
  All projects are up-to-date for restore.
  CaroShared -> D:\UTH4\Net\Net3-Group03-UDM16\Code\UDM_16_CaroGame\CaroShared\bin\Debug\net10.0\CaroShared.dll
  CaroClient -> D:\UTH4\Net\Net3-Group03-UDM16\Code\UDM_16_CaroGame\CaroClient\bin\Debug\net10.0-windows\CaroClient.dll
  CaroServer -> D:\UTH4\Net\Net3-Group03-UDM16\Code\UDM_16_CaroGame\CaroServer\bin\Debug\net10.0\CaroServer.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Hướng dẫn các nhánh Client tiếp nhận

Mọi thay đổi đã được đẩy lên `origin/develop`:
1. **`feature/client-gameplay` (Dev 4)**:
   - Chạy `git pull origin develop`.
   - Tại nút Đầu hàng (`button1_Click`): Thay vì gửi tạm `MessageType.GameOverEvent, null`, gửi đúng `NetworkMessage(MessageType.SurrenderRequest, new SurrenderRequest { RoomId = _roomId, PlayerId = _networkClient.CurrentNickname })`.
2. **`feature/ui-gameboard` (Dev 6)**:
   - Chạy `git pull origin develop`.
   - Tại `button1_Click` (Đầu hàng): Gửi `SurrenderRequest`.
   - Tại `button3_Click` (Ván mới): Gửi `NewGameRequest`.
3. **`feature/client-spectator` (Dev 4)**:
   - PR #42 đã được tích hợp thành công vào `develop`! Khán giả có thể gửi `JoinSpectatorRequest` với `RoomId` và Server sẽ trả về `JoinSpectatorResponse` kèm snapshot bàn cờ chuẩn xác.
