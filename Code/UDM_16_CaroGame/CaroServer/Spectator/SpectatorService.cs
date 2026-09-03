using CaroShared.Contracts;

namespace CaroServer.Spectator
{
    // Dev 5 phụ trách logic JoinSpectatorRequest và tạo snapshot ban đầu.
    public sealed class SpectatorService
    {
        private readonly IGameRoomStore _roomStore;

        public SpectatorService(IGameRoomStore roomStore)
        {
            _roomStore = roomStore ?? throw new ArgumentNullException(nameof(roomStore));
        }

        public SpectatorJoinResult Join(JoinSpectatorRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.RoomId))
            {
                return new(
                    SpectatorJoinResultCode.InvalidRequest,
                    Error: "RoomId là bắt buộc.");
            }

            if (!_roomStore.TryGetSnapshot(request.RoomId, out var snapshot))
            {
                return new(
                    SpectatorJoinResultCode.RoomNotFound,
                    Error: $"Không tìm thấy phòng '{request.RoomId.Trim()}'.");
            }

            if (snapshot.Room is null || snapshot.Session is null)
            {
                return new(
                    SpectatorJoinResultCode.GameStateUnavailable,
                    Error: "Trạng thái trận đấu hiện chưa sẵn sàng.");
            }

            Console.WriteLine(
                $"[{DateTimeOffset.Now:O}] Spectator '{request.SpectatorName.Trim()}' joined room '{snapshot.Room.RoomId}'.");

            return new(SpectatorJoinResultCode.Joined, snapshot);
        }
    }
}
