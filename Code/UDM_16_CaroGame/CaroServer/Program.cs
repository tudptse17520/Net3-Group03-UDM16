using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using CaroServer.Core;
using CaroServer.Managers;
using CaroServer.Data;
using CaroServer.Repositories;
using CaroServer.Heartbeat;
using CaroShared.Protocol;

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

            int port = CaroShared.Constants.NetworkConstants.DefaultPort;
            if (args.Length > 0 && int.TryParse(args[0], out int parsedPort))
            {
                port = parsedPort;
            }

            // Khởi tạo các Manager & Service
            var sessionManager = new SessionManager();
            var roomManager = new RoomManager();
            var lobbyManager = new LobbyManager();
            var eventBroadcaster = new EventBroadcaster(sessionManager, roomManager);
            var tcpServer = new TcpServerManager(sessionManager, roomManager, lobbyManager, matchRepo, eventBroadcaster, port);

            // Khởi tạo HeartbeatManager để phát hiện client zombie/mất kết nối
            var heartbeat = new HeartbeatManager(
                pingInterval: TimeSpan.FromSeconds(15),
                timeout: TimeSpan.FromSeconds(45),
                sendMessageAsync: async (clientId, msg) =>
                {
                    var session = sessionManager.GetSession(clientId);
                    if (session != null)
                        await session.SendMessageAsync(msg);
                },
                disconnectAsync: async (clientId) =>
                {
                    var session = sessionManager.GetSession(clientId);
                    if (session != null)
                    {
                        Console.WriteLine($"[Heartbeat] Client {clientId} timed out, disconnecting...");
                        session.Dispose();
                    }
                    await Task.CompletedTask;
                }
            );
            tcpServer.SetHeartbeatManager(heartbeat);
            heartbeat.Start();
            Console.WriteLine("[Heartbeat] HeartbeatManager started (Ping=15s, Timeout=45s).");

            // Bắt đầu Server lắng nghe TCP
            Task serverTask = tcpServer.StartListeningAsync();

            Console.WriteLine("Press Enter to stop the Server...");
            Console.ReadLine();

            // Dọn dẹp trước khi tắt
            await heartbeat.DisposeAsync();
            tcpServer.Stop();
        }
    }
}
