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
        static int Main(string[] args)
        {
            int port = CaroShared.Constants.NetworkConstants.DefaultPort;
            if (args.Length > 0 && int.TryParse(args[0], out var parsed)) port = parsed;
            if (args.Contains("--stop") && OperatingSystem.IsWindows())
            {
                string configPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "server-config.json"));
                if (File.Exists(configPath))
                {
                    using var json = System.Text.Json.JsonDocument.Parse(File.ReadAllText(configPath));
                    if (json.RootElement.TryGetProperty("Port", out var configuredPort)) port = configuredPort.GetInt32();
                }
                if (EventWaitHandle.TryOpenExisting($"Local\\Caro.Stop.{port}", out var stop))
                    using (stop) stop.Set();
                return 0;
            }
            if (port is < 1 or > 65535) return 2;
            // Hold and release on this same thread; clients are deliberately multi-instance.
            using var singleton = new Mutex(false, $"Local\\Caro.Server.{port}");
            bool acquired;
            try { acquired = singleton.WaitOne(0); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) return 0;
            try { RunAsync(args, port).GetAwaiter().GetResult(); return 0; }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
            finally { singleton.ReleaseMutex(); }
        }

        static async Task RunAsync(string[] args, int port)
        {
            bool background = args.Contains("--background");
            using var log = background ? CreateLog(port) : null;
            if (log != null) { Console.SetOut(TextWriter.Synchronized(log)); Console.SetError(Console.Out); }
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
            async Task InitializeDatabaseAsync()
            {
                await using var dbContext = new CaroDbContext(optionsBuilder.Options);
                try { await dbContext.Database.EnsureCreatedAsync(); }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DB] Persistence unavailable: {ex.Message}");
                }
            }
            // Database availability must not block the lobby or local-server readiness.
            _ = InitializeDatabaseAsync();

            var matchRepo = new MatchHistoryRepository(optionsBuilder.Options);

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
            if (serverTask.IsFaulted) await serverTask;
            using var ready = OperatingSystem.IsWindows() ? new EventWaitHandle(false, EventResetMode.ManualReset, $"Local\\Caro.Ready.{port}") : null;
            using var stop = OperatingSystem.IsWindows() ? new EventWaitHandle(false, EventResetMode.ManualReset, $"Local\\Caro.Stop.{port}") : null;
            ready?.Set();
            try
            {
                if (background) await Task.WhenAny(serverTask, stop != null ? Task.Run(() => stop.WaitOne()) : Task.Delay(Timeout.Infinite));
                else
                {
                    Console.WriteLine("Press Enter to stop the Server...");
                    await Task.WhenAny(serverTask, Task.Run(() => Console.ReadLine()));
                }
            }
            finally
            {
                ready?.Reset();
                await heartbeat.DisposeAsync();
                tcpServer.Stop();
            }
        }

        static StreamWriter CreateLog(int port)
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Caro", "Logs");
            Directory.CreateDirectory(directory);
            return new StreamWriter(new FileStream(Path.Combine(directory, $"server-{port}.log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
        }
    }
}
