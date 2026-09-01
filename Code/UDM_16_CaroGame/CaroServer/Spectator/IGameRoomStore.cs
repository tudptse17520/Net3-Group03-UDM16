using CaroShared.Contracts;

namespace CaroServer.Spectator
{
    // Nơi lưu trạng thái phòng. Dev 1/2 có thể thay implementation bằng game state thật.
    public interface IGameRoomStore
    {
        bool TryGetSnapshot(string roomId, out SpectatorStateSnapshotDto snapshot);
    }
}
