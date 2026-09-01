using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using CaroShared.Protocol;
using CaroShared.Constants;

namespace CaroServer.Models
{
    // Đại diện cho một phiên kết nối của người chơi
    public class PlayerSession : IDisposable
    {
        // ID của người chơi
        public string PlayerId { get; set; }

        // Token dùng để xác thực khi Reconnect
        public string SessionToken { get; private set; }

        // Phòng đang chơi (null nếu ở sảnh chờ)
        public string? CurrentRoomId { get; set; }

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

        // Gửi tin nhắn qua luồng Stream
        public async Task SendMessageAsync(NetworkMessage message)
        {
            try
            {
                string json = JsonSerializer.Serialize(message);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json + NetworkConstants.MessageDelimiter);
                await Stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerSession] Error sending message to {PlayerId}: {ex.Message}");
            }
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
