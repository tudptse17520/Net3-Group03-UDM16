using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroServer.Spectator
{
    // Adapter mạng: nhận NetworkMessage JSON theo từng dòng và trả snapshot.
    // Logic nghiệp vụ vẫn nằm ở SpectatorService để dễ test/integrate.
    public sealed class SpectatorConnectionHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly SpectatorService _service;

        public SpectatorConnectionHandler(SpectatorService service)
        {
            _service = service;
        }

        public async Task HandleAsync(TcpClient client, CancellationToken cancellationToken = default)
        {
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
            {
                AutoFlush = true
            };

            while (!cancellationToken.IsCancellationRequested && client.Connected)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                    break;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                NetworkMessage? message;
                try
                {
                    message = JsonSerializer.Deserialize<NetworkMessage>(line, JsonOptions);
                }
                catch (JsonException)
                {
                    await WriteAsync(writer, new NetworkMessage(
                        MessageType.SpectatorError,
                        new { code = "INVALID_JSON", message = "Gói tin không hợp lệ." }), cancellationToken);
                    continue;
                }

                if (message?.Type != MessageType.JoinSpectatorRequest)
                {
                    await WriteAsync(writer, new NetworkMessage(
                        MessageType.SpectatorError,
                        new { code = "UNSUPPORTED_MESSAGE", message = "Server chỉ xử lý JoinSpectatorRequest trên endpoint này." },
                        message?.RequestId), cancellationToken);
                    continue;
                }

                JoinSpectatorRequest? request;
                try
                {
                    request = DeserializePayload<JoinSpectatorRequest>(message.Payload);
                }
                catch (JsonException)
                {
                    await WriteAsync(writer, new NetworkMessage(
                        MessageType.SpectatorError,
                        new { code = "INVALID_PAYLOAD", message = "Payload JoinSpectatorRequest không hợp lệ." },
                        message.RequestId), cancellationToken);
                    continue;
                }

                var result = _service.Join(request);
                if (result.Code == SpectatorJoinResultCode.Joined)
                {
                    await WriteAsync(writer, new NetworkMessage(
                        MessageType.SpectatorStateSnapshot,
                        result.Snapshot,
                        message.RequestId), cancellationToken);
                }
                else
                {
                    await WriteAsync(writer, new NetworkMessage(
                        MessageType.SpectatorError,
                        new { code = result.Code.ToString(), message = result.Error },
                        message.RequestId), cancellationToken);
                }
            }
        }

        private static T? DeserializePayload<T>(object? payload)
        {
            if (payload is null)
                return default;

            if (payload is JsonElement element)
                return element.Deserialize<T>(JsonOptions);

            return JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(payload, JsonOptions), JsonOptions);
        }

        private static Task WriteAsync(StreamWriter writer, NetworkMessage message, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);
            return writer.WriteLineAsync(json.AsMemory(), cancellationToken).AsTask();
        }
    }
}
