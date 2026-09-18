using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
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
            
            // Đọc cấu hình appsettings
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            string connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            // Khởi tạo CSDL qua EF Core
            var optionsBuilder = new DbContextOptionsBuilder<CaroDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            var dbContext = new CaroDbContext(optionsBuilder.Options);
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

            // Khởi tạo các Manager & Service
            var sessionManager = new SessionManager();
            var roomManager = new RoomManager();
            var lobbyManager = new LobbyManager();
            var eventBroadcaster = new EventBroadcaster(sessionManager, roomManager);
            var tcpServer = new TcpServerManager(sessionManager, roomManager, lobbyManager, matchRepo, eventBroadcaster);

            // Bắt đầu Server lắng nghe TCP
            Task serverTask = tcpServer.StartListeningAsync();

            Console.WriteLine("Press Enter to stop the Server...");
            Console.ReadLine();

            // Dọn dẹp trước khi tắt
            tcpServer.Stop();
        }
    }
}
