using System;
using System.Threading.Tasks;
using CaroServer.Core;
using CaroServer.Managers;
using CaroServer.Data;
using CaroServer.Repositories;

namespace CaroServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== UDM_16 CARO SERVER ===");
            
            // Khởi tạo Database qua EF Core
            var dbContext = new CaroDbContext();
            try
            {
                await dbContext.Database.EnsureCreatedAsync();
                Console.WriteLine("[DB] Database connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DB] Connection failed: {ex.Message}");
                Console.WriteLine("[DB] Running without database persistence.");
            }

            var matchRepo = new MatchHistoryRepository(dbContext);

            // Khởi tạo các manager và dịch vụ mạng
            var sessionManager = new SessionManager();
            var roomManager = new RoomManager();
            var lobbyManager = new LobbyManager();
            var eventBroadcaster = new EventBroadcaster(sessionManager, roomManager);
            var tcpServer = new TcpServerManager(sessionManager, roomManager, lobbyManager, matchRepo, eventBroadcaster);

            // Bắt đầu lắng nghe TCP bất đồng bộ
            Task serverTask = tcpServer.StartListeningAsync();

            Console.WriteLine("Press Enter to stop the Server...");
            Console.ReadLine();

            // Dọn dẹp trước khi tắt
            tcpServer.Stop();
        }
    }
}
