using System;
using CaroShared.Constants;

namespace CaroServer.Game
{
    // Kết quả trả về sau mỗi nước đi
    // Dev 1 dùng class này để đóng gói MoveMadeEvent hoặc GameOverEvent gửi về Client
    public class MoveResult
    {
        // Nước đi có hợp lệ không
        public bool IsValid { get; set; }

        // Nếu không hợp lệ thì lỗi gì
        public string? ErrorMessage { get; set; }

        // Tọa độ vừa đánh
        public int X { get; set; }
        public int Y { get; set; }

        // Ai đánh (1 = X, 2 = O)
        public int Player { get; set; }

        // Ván đã kết thúc chưa
        public bool IsGameOver { get; set; }

        // Người thắng (0 = chưa có, 1 = X thắng, 2 = O thắng)
        public int WinnerSymbol { get; set; }

        // Hòa hay không
        public bool IsDraw { get; set; }

        // Lượt tiếp theo (1 hoặc 2, bằng 0 nếu game đã xong)
        public int NextTurn { get; set; }
    }

    // Bộ não logic của trò chơi Caro
    // Class này hoàn toàn độc lập, không dính dáng gì đến TCP hay Network
    // Chỉ nhận dữ liệu đầu vào (tọa độ, người chơi) và trả ra kết quả
    public class CaroEngine
    {
        // Bàn cờ 15x15 (0: trống, 1: X, 2: O)
        public int[][] Board { get; private set; }

        // Lượt hiện tại (1 = X đánh trước, 2 = O)
        public int CurrentTurn { get; private set; }

        // Trạng thái ván đấu ("Playing" hoặc "Finished")
        public string Status { get; private set; }

        // Đếm tổng số nước đã đánh (dùng để xét hòa: đủ 225 là hòa)
        public int MoveCount { get; private set; }

        // Người thắng (0 = chưa có, 1 = X, 2 = O)
        public int WinnerSymbol { get; private set; }

        // Khóa chống spam click (2 request đánh cùng lúc)
        // Khác với ConcurrentDictionary ở LobbyManager (Sprint 2):
        // - LobbyManager: mỗi thao tác là đơn lẻ → ConcurrentDictionary đủ
        // - CaroEngine: một nước đi = chuỗi nhiều bước phải chạy nguyên tử
        //   (kiểm tra lượt → đánh dấu → xét thắng → đổi lượt)
        //   → Cần lock để buộc cả chuỗi chạy xong trước khi request tiếp vào
        private readonly object _lockObj = new();

        // === EVENT HOOKS cho Dev 5 gắn code ghi Log ===
        // Dev 5 chỉ cần đăng ký event từ bên ngoài, không cần mở file này
        // Ví dụ: engine.OnMoveMade += (x, y, player) => MatchLogger.LogMove(...)
        public event Action<int, int, int>? OnMoveMade;       // (x, y, player)
        public event Action<int, bool>? OnGameOver;            // (winnerSymbol, isDraw)
        public event Action<int, int, string>? OnInvalidMove;  // (x, y, lý do)

        public CaroEngine()
        {
            // Khởi tạo bàn cờ 15x15 toàn số 0 (trống)
            Board = new int[GameConstants.BoardSize][];
            for (int i = 0; i < GameConstants.BoardSize; i++)
            {
                Board[i] = new int[GameConstants.BoardSize];
            }

            // X luôn đánh trước theo luật
            CurrentTurn = 1;
            Status = "Playing";
            MoveCount = 0;
            WinnerSymbol = 0;
        }

        // Hàm xử lý chính: nhận nước đi từ người chơi
        // Trả về MoveResult chứa đủ thông tin để Dev 1 broadcast về Client
        // Bên trong lock xử lý Bước 3 → 6 theo đặc tả Sprint 3
        // (Bước 1 và 2 do RoomManager xử lý trước khi gọi vào đây)
        public MoveResult MakeMove(int x, int y, int player)
        {
            // lock toàn bộ chuỗi kiểm tra để chống đụng độ đa luồng
            // Kịch bản: 2 người cùng gửi request trong cùng 1 mili-giây
            // → Request thứ 2 phải xếp hàng chờ lock
            // → Khi vào được thì lượt đã đổi → tự động bị reject ở bước kiểm tra lượt
            lock (_lockObj)
            {
                // Bước 6 (kiểm tra sớm): Game đã kết thúc chưa?
                if (Status != "Playing")
                {
                    string reason = "Trận đấu đã kết thúc";
                    OnInvalidMove?.Invoke(x, y, reason);
                    return new MoveResult
                    {
                        IsValid = false,
                        ErrorMessage = reason,
                        X = x, Y = y, Player = player
                    };
                }

                // Bước 3: Có đúng lượt không?
                // Đây chính là chỗ chặn spam click hiệu quả nhất
                if (player != CurrentTurn)
                {
                    string reason = "Chưa tới lượt của bạn";
                    OnInvalidMove?.Invoke(x, y, reason);
                    return new MoveResult
                    {
                        IsValid = false,
                        ErrorMessage = reason,
                        X = x, Y = y, Player = player
                    };
                }

                // Bước 4: Tọa độ có hợp lệ?
                // Chặn tọa độ âm (-1, -3) hoặc vượt ngoài bàn cờ (15, 20)
                if (x < 0 || x >= GameConstants.BoardSize ||
                    y < 0 || y >= GameConstants.BoardSize)
                {
                    string reason = "Tọa độ nằm ngoài bàn cờ";
                    OnInvalidMove?.Invoke(x, y, reason);
                    return new MoveResult
                    {
                        IsValid = false,
                        ErrorMessage = reason,
                        X = x, Y = y, Player = player
                    };
                }

                // Bước 5: Ô đã có quân chưa?
                // Chặn đánh đè lên quân đã có (quân mình hoặc quân đối thủ)
                if (Board[x][y] != 0)
                {
                    string reason = "Ô này đã được đánh rồi";
                    OnInvalidMove?.Invoke(x, y, reason);
                    return new MoveResult
                    {
                        IsValid = false,
                        ErrorMessage = reason,
                        X = x, Y = y, Player = player
                    };
                }

                // === TẤT CẢ 6 BƯỚC ĐỀU PASS → NƯỚC ĐI HỢP LỆ ===

                // 1. Cập nhật bàn cờ
                Board[x][y] = player;
                MoveCount++;

                // 2. Kiểm tra thắng (thuật toán O(1) - chỉ duyệt từ ô vừa đánh)
                bool isWin = CheckWin(x, y, player);

                if (isWin)
                {
                    Status = "Finished";
                    WinnerSymbol = player;

                    // Phát sự kiện cho Dev 5 ghi log
                    OnMoveMade?.Invoke(x, y, player);
                    OnGameOver?.Invoke(player, false);

                    return new MoveResult
                    {
                        IsValid = true,
                        X = x, Y = y, Player = player,
                        IsGameOver = true,
                        WinnerSymbol = player,
                        IsDraw = false,
                        NextTurn = 0
                    };
                }

                // 3. Kiểm tra hòa (đủ 225 nước = 15x15 mà chưa ai thắng)
                if (MoveCount == GameConstants.BoardSize * GameConstants.BoardSize)
                {
                    Status = "Finished";
                    WinnerSymbol = 0;

                    OnMoveMade?.Invoke(x, y, player);
                    OnGameOver?.Invoke(0, true);

                    return new MoveResult
                    {
                        IsValid = true,
                        X = x, Y = y, Player = player,
                        IsGameOver = true,
                        WinnerSymbol = 0,
                        IsDraw = true,
                        NextTurn = 0
                    };
                }

                // 4. Đổi lượt (1 → 2 hoặc 2 → 1)
                CurrentTurn = (player == 1) ? 2 : 1;

                // 5. Phát sự kiện cho Dev 5 ghi log nước đi
                OnMoveMade?.Invoke(x, y, player);

                return new MoveResult
                {
                    IsValid = true,
                    X = x, Y = y, Player = player,
                    IsGameOver = false,
                    WinnerSymbol = 0,
                    IsDraw = false,
                    NextTurn = CurrentTurn
                };
            }
        }

        // Kiểm tra thắng: duyệt 4 hướng từ ô (x, y) vừa đánh
        // Không duyệt lại toàn bộ 225 ô → O(1)
        private bool CheckWin(int x, int y, int player)
        {
            // 4 hướng cần kiểm tra:
            //   Ngang (—)     : dx=0, dy=1
            //   Dọc (|)       : dx=1, dy=0
            //   Chéo xuôi (\) : dx=1, dy=1
            //   Chéo ngược (/): dx=1, dy=-1
            int[][] directions = new int[][]
            {
                new int[] { 0, 1 },
                new int[] { 1, 0 },
                new int[] { 1, 1 },
                new int[] { 1, -1 }
            };

            foreach (var dir in directions)
            {
                if (CheckDirection(x, y, dir[0], dir[1], player))
                {
                    return true;
                }
            }

            return false;
        }

        // Đếm quân liên tiếp theo 1 hướng và kiểm tra luật chặn 2 đầu
        private bool CheckDirection(int x, int y, int dx, int dy, int player)
        {
            // Đếm quân liên tiếp theo chiều thuận (+dx, +dy)
            int countForward = CountInDirection(x, y, dx, dy, player);

            // Đếm quân liên tiếp theo chiều ngược (-dx, -dy)
            int countBackward = CountInDirection(x, y, -dx, -dy, player);

            // Tổng = thuận + ngược + 1 (tính cả ô vừa đánh)
            int total = countForward + countBackward + 1;

            // Chưa đủ 5 quân → chắc chắn chưa thắng
            if (total < 5)
            {
                return false;
            }

            // Hơn 5 quân liên tiếp → luôn thắng (không thể chặn 2 đầu được)
            if (total > 5)
            {
                return true;
            }

            // === ĐÚNG 5 QUÂN → Áp dụng luật chặn 2 đầu (Caro Việt Nam) ===
            // Tìm tọa độ 2 ô ở 2 đầu chuỗi (ô ngay trước đầu và ngay sau cuối)
            int headX = x - (countBackward + 1) * dx;
            int headY = y - (countBackward + 1) * dy;
            int tailX = x + (countForward + 1) * dx;
            int tailY = y + (countForward + 1) * dy;

            // Kiểm tra từng đầu có bị chặn không
            bool headBlocked = IsBlocked(headX, headY, player);
            bool tailBlocked = IsBlocked(tailX, tailY, player);

            // Cả 2 đầu đều bị chặn → KHÔNG tính thắng
            // Ví dụ: [X] [O][O][O][O][O] [X] → O không thắng
            if (headBlocked && tailBlocked)
            {
                return false;
            }

            // Ít nhất 1 đầu mở → THẮNG
            // Ví dụ: [ ] [O][O][O][O][O] [X] → O thắng (đầu trái mở)
            return true;
        }

        // Đếm số quân liên tiếp cùng loại theo 1 chiều từ (x, y)
        // Không tính ô (x, y) vì nó đã được tính ở ngoài (+1)
        private int CountInDirection(int x, int y, int dx, int dy, int player)
        {
            int count = 0;
            int nx = x + dx;
            int ny = y + dy;

            // Duyệt theo hướng cho đến khi gặp quân khác hoặc ra ngoài bàn cờ
            // Tối đa 14 bước (cạnh bàn cờ) → vẫn là O(1) vì kích thước bàn cố định
            while (nx >= 0 && nx < GameConstants.BoardSize &&
                   ny >= 0 && ny < GameConstants.BoardSize &&
                   Board[nx][ny] == player)
            {
                count++;
                nx += dx;
                ny += dy;
            }

            return count;
        }

        // Kiểm tra 1 đầu chuỗi có bị chặn không
        // Bị chặn = nằm ngoài bàn cờ HOẶC là quân đối thủ
        private bool IsBlocked(int x, int y, int player)
        {
            // Ngoài bàn cờ = bị chặn bởi biên
            if (x < 0 || x >= GameConstants.BoardSize ||
                y < 0 || y >= GameConstants.BoardSize)
            {
                return true;
            }

            // Ô trống = không bị chặn (đầu mở)
            if (Board[x][y] == 0)
            {
                return false;
            }

            // Quân đối thủ = bị chặn
            return Board[x][y] != player;
        }
    }
}
