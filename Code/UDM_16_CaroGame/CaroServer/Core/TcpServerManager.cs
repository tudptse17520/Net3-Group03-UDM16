using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CaroServer.Managers;
using CaroServer.Models;
using CaroShared.Constants;

namespace CaroServer.Core
{
    // Quản lý Server và các kết nối TCP
    public class TcpServerManager
    {
        private readonly TcpListener _listener;
        private readonly SessionManager _sessionManager;
        private bool _isRunning;

        public TcpServerManager(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
            // Lắng nghe kết nối trên port mặc định
            _listener = new TcpListener(IPAddress.Any, NetworkConstants.DefaultPort);
        }

        // Bắt đầu lắng nghe
        public async Task StartListeningAsync()
        {
            _listener.Start();
            _isRunning = true;
            Console.WriteLine($"[TcpServer] Server started on port {NetworkConstants.DefaultPort}");

            try
            {
                while (_isRunning)
                {
                    // Chờ Client kết nối
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    Console.WriteLine($"[TcpServer] Client connected: {client.Client.RemoteEndPoint}");

                    // Khởi tạo session và lưu vào Manager
                    var session = new PlayerSession(client);
                    _sessionManager.AddSession(session);

                    // TODO: Bắt đầu vòng lặp đọc dữ liệu từ client này
                    // _ = ReceiveDataAsync(session); 
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"[TcpServer] Error while listening: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();
            Console.WriteLine("[TcpServer] Server stopped.");
        }
    }
}
