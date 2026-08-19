using System.Text;
using System.Text.Json;

namespace CaroShared.Protocol
{
    public static class MessageSerializer
    {
        // Đóng gói: Thêm 4-byte Length Prefix vào đầu gói tin TCP để chống dính/xé gói
        public static byte[] Pack(NetworkMessage message)
        {
            string json = JsonSerializer.Serialize(message);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(json);
            byte[] lengthBytes = BitConverter.GetBytes(bodyBytes.Length);

            byte[] packet = new byte[4 + bodyBytes.Length];
            Buffer.BlockCopy(lengthBytes, 0, packet, 0, 4);
            Buffer.BlockCopy(bodyBytes, 0, packet, 4, bodyBytes.Length);

            return packet;
        }

        // Giải mã gói tin từ UTF8 JSON
        public static NetworkMessage? Unpack(byte[] bodyBytes)
        {
            string json = Encoding.UTF8.GetString(bodyBytes);
            return JsonSerializer.Deserialize<NetworkMessage>(json);
        }
    }
}