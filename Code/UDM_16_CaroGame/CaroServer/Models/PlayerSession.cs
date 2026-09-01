using System;
using System.Net.Sockets;

namespace CaroServer.Models
{
    // Đại diện cho một phiên kết nối của người chơi
    public class PlayerSession : IDisposable
    {
        // ID của người chơi
        public string PlayerId { get; private set; }

        // Token dùng để xác thực khi Reconnect
        public string SessionToken { get; private set; }

        public TcpClient Client { get; private set; }
        public NetworkStream Stream { get; private set; }

        public PlayerSession(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            
            // Khởi tạo ngẫu nhiên danh tính và Token khi mới kết nối
            string shortId = Guid.NewGuid().ToString("N").Substring(0, 6);
            PlayerId = $"Player_{shortId}";
            SessionToken = Guid.NewGuid().ToString();
        }

        // Đảm bảo đóng socket và giải phóng tài nguyên
        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Close();
                Stream.Dispose();
            }
            if (Client != null)
            {
                Client.Close();
                Client.Dispose();
            }
        }
    }
}
