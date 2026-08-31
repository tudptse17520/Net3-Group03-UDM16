using System;
using CaroShared.Constants;

namespace CaroServer.Game
{
    // Kết quả trả về sau mỗi nước đi
    public class MoveResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        // 1 = X, 2 = O
        public int Player { get; set; }
        public bool IsGameOver { get; set; }
        // 0 = chưa có, 1 = X thắng, 2 = O thắng
        public int WinnerSymbol { get; set; }
        public bool IsDraw { get; set; }
        // 0 nếu game đã xong
        public int NextTurn { get; set; }
    }

    // Xử lý toàn bộ logic của một ván Caro, độc lập với tầng Network
    public class CaroEngine
    {
        // 0: trống, 1: X, 2: O
        public int[][] Board { get; private set; }
        public int CurrentTurn { get; private set; }
        public string Status { get; private set; }
        public int MoveCount { get; private set; }
        public int WinnerSymbol { get; private set; }

        // Đảm bảo mỗi nước đi được xử lý trọn vẹn trước nước tiếp theo
        private readonly object _lockObj = new();

        // Event cho bên ngoài đăng ký theo dõi diễn biến trận đấu
        public event Action<int, int, int>? OnMoveMade;       // (x, y, player)
        public event Action<int, bool>? OnGameOver;            // (winnerSymbol, isDraw)
        public event Action<int, int, string>? OnInvalidMove;  // (x, y, lý do)

        public CaroEngine()
        {
            Board = new int[GameConstants.BoardSize][];
            for (int i = 0; i < GameConstants.BoardSize; i++)
            {
                Board[i] = new int[GameConstants.BoardSize];
            }

            CurrentTurn = 1;
            Status = "Playing";
            MoveCount = 0;
            WinnerSymbol = 0;
        }

        // Xử lý một nước đi, trả về kết quả để broadcast cho các Client
        public MoveResult MakeMove(int x, int y, int player)
        {
            lock (_lockObj)
            {
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

                Board[x][y] = player;
                MoveCount++;

                bool isWin = CheckWin(x, y, player);

                if (isWin)
                {
                    Status = "Finished";
                    WinnerSymbol = player;

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

                // Đủ 225 nước mà chưa ai thắng thì hòa
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

                CurrentTurn = (player == 1) ? 2 : 1;

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

        // Kiểm tra 4 hướng: ngang, dọc và hai đường chéo
        private bool CheckWin(int x, int y, int player)
        {
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

        // Kiểm tra chuỗi quân liên tiếp theo một hướng và luật chặn hai đầu
        private bool CheckDirection(int x, int y, int dx, int dy, int player)
        {
            int countForward = CountInDirection(x, y, dx, dy, player);
            int countBackward = CountInDirection(x, y, -dx, -dy, player);
            int total = countForward + countBackward + 1;

            if (total < 5)
            {
                return false;
            }

            // Hơn 5 quân liên tiếp thì luôn thắng
            if (total > 5)
            {
                return true;
            }

            // Đúng 5 quân: kiểm tra hai đầu chuỗi có bị chặn không
            int headX = x - (countBackward + 1) * dx;
            int headY = y - (countBackward + 1) * dy;
            int tailX = x + (countForward + 1) * dx;
            int tailY = y + (countForward + 1) * dy;

            bool headBlocked = IsBlocked(headX, headY, player);
            bool tailBlocked = IsBlocked(tailX, tailY, player);

            // Cả hai đầu bị chặn thì không tính thắng
            if (headBlocked && tailBlocked)
            {
                return false;
            }

            return true;
        }

        // Đếm quân liên tiếp cùng loại theo một chiều, không tính ô gốc
        private int CountInDirection(int x, int y, int dx, int dy, int player)
        {
            int count = 0;
            int nx = x + dx;
            int ny = y + dy;

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

        // Bị chặn nếu nằm ngoài bàn cờ hoặc là quân đối thủ
        private bool IsBlocked(int x, int y, int player)
        {
            if (x < 0 || x >= GameConstants.BoardSize ||
                y < 0 || y >= GameConstants.BoardSize)
            {
                return true;
            }

            if (Board[x][y] == 0)
            {
                return false;
            }

            return Board[x][y] != player;
        }
    }
}
