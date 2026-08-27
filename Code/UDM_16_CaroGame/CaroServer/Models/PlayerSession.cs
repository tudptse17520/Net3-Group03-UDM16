using System;
using System.Net.Sockets;

namespace CaroServer.Models
{
    // Đại diện cho một phiên kết nối của người chơi
    public class PlayerSession : IDisposable
    {
        // ID của người chơi
        public string PlayerId { get; private set; }

        // Tên hiển thị (Nickname) của người chơi
        public string Nickname { get; set; } = string.Empty;

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

        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        // Gửi thông điệp tới Client
        public async System.Threading.Tasks.Task SendAsync(CaroShared.Protocol.NetworkMessage message)
        {
            try
            {
                if (Client.Connected && Stream.CanWrite)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(message, JsonOptions) + CaroShared.Constants.NetworkConstants.MessageDelimiter;
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    await Stream.WriteAsync(bytes, 0, bytes.Length);
                    await Stream.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerSession] Lỗi gửi dữ liệu tới {PlayerId}: {ex.Message}");
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
