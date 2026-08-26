using System.Text.Json; 

namespace CaroShared.Protocol
{
    public class MessageSerializer
    {
        private const char MessageDelimiter = '\n';

        private readonly JsonSerializerOptions _options;

        public MessageSerializer()
        {
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // Object -> JSON + \n
        public string Serialize<T>(T message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string json = JsonSerializer.Serialize(message, _options);

            return json + MessageDelimiter;
        }

        // JSON -> Object
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException(
                    "JSON message cannot be empty.",
                    nameof(json));

            return JsonSerializer.Deserialize<T>(
                json.Trim(),
                _options
            ) ?? throw new InvalidOperationException(
                "Cannot deserialize message.");
        }

        // Tách các message theo \n
        public List<string> SplitMessages(string buffer)
        {
            if (string.IsNullOrEmpty(buffer))
                return new List<string>();

            return buffer
                .Split(MessageDelimiter)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();
        }
    }
}
