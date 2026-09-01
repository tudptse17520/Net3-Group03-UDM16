using CaroShared.Constants;
using CaroShared.Contracts;

namespace CaroServer.Spectator
{
    // Store tối giản cho server hiện tại. Có thể cập nhật state từ game engine sau này.
    public sealed class InMemoryGameRoomStore : IGameRoomStore
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, SpectatorStateSnapshotDto> _rooms =
            new(StringComparer.OrdinalIgnoreCase);

        public void Upsert(string roomId, RoomDto room, GameSessionDto session)
        {
            if (string.IsNullOrWhiteSpace(roomId))
                throw new ArgumentException("RoomId không được rỗng.", nameof(roomId));

            var normalizedRoomId = roomId.Trim();
            var board = CloneBoard(session.Board);
            var safeSession = session with { Board = board };
            var safeRoom = room with { RoomId = normalizedRoomId };

            lock (_sync)
            {
                _rooms[normalizedRoomId] = new SpectatorStateSnapshotDto
                {
                    Room = safeRoom,
                    Session = safeSession
                };
            }
        }

        public bool TryGetSnapshot(string roomId, out SpectatorStateSnapshotDto snapshot)
        {
            snapshot = null!;
            if (string.IsNullOrWhiteSpace(roomId))
                return false;

            lock (_sync)
            {
                if (!_rooms.TryGetValue(roomId.Trim(), out var value))
                    return false;

                // Không trả reference tới board nội bộ.
                snapshot = value with
                {
                    Room = value.Room,
                    Session = value.Session is null
                        ? null
                        : value.Session with { Board = CloneBoard(value.Session.Board) }
                };
                return true;
            }
        }

        private static int[][] CloneBoard(int[][] board)
        {
            var source = board ?? [];
            if (source.Length == 0)
            {
                var empty = new int[GameConstants.BoardSize][];
                for (var i = 0; i < GameConstants.BoardSize; i++)
                    empty[i] = new int[GameConstants.BoardSize];
                return empty;
            }

            return source.Select(row => row?.ToArray() ?? []).ToArray();
        }
    }
}
