using CaroShared.Contracts;
using CaroShared.Enums;

namespace CaroServer.Services
{
    public class SpectatorAndReconnectManager
    {
        // Tạo Snapshot khôi phục cho người chơi Reconnect trong cửa sổ 60s
        public GameStateDto BuildReconnectState(
            string roomId, PlayerDto pX, PlayerDto pO, int[][] board, 
            PlayerSymbol turn, GameStatus status, int remainingTime, PlayerSymbol yourRole)
        {
            return new GameStateDto
            {
                Room = new RoomDto
                {
                    RoomId = roomId,
                    Player1 = pX?.Nickname ?? string.Empty,
                    Player2 = pO?.Nickname ?? string.Empty
                },
                Session = new GameSessionDto
                {
                    Board = board,
                    CurrentTurn = (int)turn,
                    Status = status.ToString(),
                    RemainingTimeSeconds = remainingTime
                }
            };
        }

        // Tạo Snapshot toàn bộ trận đấu cho Khán giả vừa join phòng
        public SpectatorStateSnapshotDto BuildSpectatorSnapshot(
            string roomId, PlayerDto pX, PlayerDto pO, int spectatorCount, 
            int[][] board, PlayerSymbol turn, GameStatus status, int remainingTime)
        {
            return new SpectatorStateSnapshotDto
            {
                Room = new RoomDto
                {
                    RoomId = roomId,
                    Player1 = pX?.Nickname ?? string.Empty,
                    Player2 = pO?.Nickname ?? string.Empty
                },
                Session = new GameSessionDto
                {
                    Board = board,
                    CurrentTurn = (int)turn,
                    Status = status.ToString(),
                    RemainingTimeSeconds = remainingTime
                }
            };
        }
    }
}