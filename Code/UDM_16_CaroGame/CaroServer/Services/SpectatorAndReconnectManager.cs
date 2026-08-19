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
            return new GameStateDto(
                roomId, pX, pO, board, turn, status, remainingTime, yourRole
            );
        }

        // Tạo Snapshot toàn bộ trận đấu cho Khán giả vừa join phòng
        public SpectatorStateSnapshotDto BuildSpectatorSnapshot(
            string roomId, PlayerDto pX, PlayerDto pO, int spectatorCount, 
            int[][] board, PlayerSymbol turn, GameStatus status, int remainingTime)
        {
            return new SpectatorStateSnapshotDto(
                roomId, pX, pO, spectatorCount, board, turn, status, remainingTime
            );
        }
    }
}