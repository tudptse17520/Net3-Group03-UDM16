using System.Net;
using CaroServer.Spectator;
using CaroShared.Constants;
using CaroShared.Contracts;

namespace CaroServer
{
    internal class Program
    {
        private const int Port = 5000;

        static async Task Main(string[] args)
        {
            var roomStore = new InMemoryGameRoomStore();

            // State mẫu để kiểm tra JoinSpectatorRequest.
            // Khi ghép với game engine thật, thay bằng state của phòng đang chơi.
            roomStore.Upsert(
                "ROOM-001",
                new RoomDto
                {
                    RoomId = "ROOM-001",
                    PlayerX = "PlayerX",
                    PlayerO = "PlayerO",
                    SpectatorCount = 0
                },
                new GameSessionDto
                {
                    Board = CreateEmptyBoard(),
                    CurrentTurn = 1,
                    Status = "Playing",
                    RemainingTimeSeconds = GameConstants.TurnTimeoutSeconds
                });

            var service = new SpectatorService(roomStore);
            var handler = new SpectatorConnectionHandler(service);
            var server = new SpectatorServer(IPAddress.Any, Port, handler);

            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                shutdown.Cancel();
            };

            Console.WriteLine("CaroServer - Dev 5 spectator endpoint");
            Console.WriteLine("Nhấn Ctrl+C để dừng server.");
            await server.RunAsync(shutdown.Token);
        }

        private static int[][] CreateEmptyBoard()
        {
            var board = new int[GameConstants.BoardSize][];
            for (var row = 0; row < GameConstants.BoardSize; row++)
                board[row] = new int[GameConstants.BoardSize];
            return board;
        }
    }
}
