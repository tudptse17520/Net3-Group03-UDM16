using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CaroShared.Protocol;
using CaroShared.Constants;

namespace CaroServer.Models
{
    // Đại diện cho một phiên kết nối của người chơi
    public class PlayerSession : IDisposable
    {
        public string PlayerId { get; set; }

        // Token dùng để xác thực khi Reconnect.
        public string SessionToken { get; private set; }

        // Phòng đang chơi (null nếu ở sảnh chờ)
        public string? CurrentRoomId { get; set; }

        public TcpClient Client { get; private set; }
        public NetworkStream Stream { get; private set; }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public PlayerSession(TcpClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Stream = client.GetStream();

            string shortId = Guid.NewGuid().ToString("N").Substring(0, 6);
            PlayerId = $"Player_{shortId}";
            SessionToken = Guid.NewGuid().ToString("N");
        }

        // Gắn socket mới cho session sau khi Reconnect thành công.
        public void RestoreConnection(TcpClient client, string playerId, string sessionToken, string? roomId)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            Stream = client.GetStream();
            PlayerId = playerId;
            SessionToken = sessionToken;
            CurrentRoomId = roomId;
        }

        public async Task SendMessageAsync(NetworkMessage message)
        {
            try
            {
                string json = JsonSerializer.Serialize(message, JsonOptions);
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json + NetworkConstants.MessageDelimiter);
                await Stream.WriteAsync(data, 0, data.Length);
                await Stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlayerSession] Error sending message to {PlayerId}: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            try { Stream?.Close(); } catch { }
            try { Stream?.Dispose(); } catch { }
            try { Client?.Close(); } catch { }
            try { Client?.Dispose(); } catch { }
        }
    }
}
