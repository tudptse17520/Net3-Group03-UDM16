using System;
using System.Threading.Tasks;
using CaroServer.Core;
using CaroServer.Managers;

namespace CaroServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== UDM_16 CARO SERVER ===");
            
            // Khởi tạo các manager
            var sessionManager = new SessionManager();
            var tcpServer = new TcpServerManager(sessionManager);

            // Bắt đầu lắng nghe TCP bất đồng bộ
            Task serverTask = tcpServer.StartListeningAsync();

            Console.WriteLine("Bam Enter de tat Server...");
            Console.ReadLine();

            // Dọn dẹp trước khi tắt
            tcpServer.Stop();
        }
    }
}
