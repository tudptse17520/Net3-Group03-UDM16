using System;
using System.Text;
using System.Text.Json;

namespace CaroShared.Protocol
{
    public sealed class MessageSerializer
    {
        private readonly JsonSerializerOptions _options;

        public MessageSerializer(JsonSerializerOptions? options = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        public string Serialize(NetworkMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);
            return JsonSerializer.Serialize(message, _options) + "\n";
        }

        public NetworkMessage Deserialize(string frame)
        {
            if (string.IsNullOrWhiteSpace(frame))
            {
                throw new ArgumentException("Message frame cannot be empty.", nameof(frame));
            }

            var message = JsonSerializer.Deserialize<NetworkMessage>(frame.Trim(), _options);

            return message ?? throw new JsonException("Cannot deserialize NetworkMessage.");
        }

        public string SerializePayload<T>(T payload)
        {
            ArgumentNullException.ThrowIfNull(payload);
            return JsonSerializer.Serialize(payload, _options);
        }

        public T DeserializePayload<T>(NetworkMessage message)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (message.Payload is not JsonElement element)
            {
                throw new JsonException("NetworkMessage.Payload does not contain a JSON object.");
            }

            var payload = element.Deserialize<T>(_options);

            return payload ?? throw new JsonException("Cannot deserialize message payload.");
        }
    }
}
