using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CaroShared.Contracts;
using CaroShared.Enums;

namespace CaroShared.Protocol
{
    // Gói tin chung dùng để truyền dữ liệu giữa Client và Server
    public class NetworkMessage
    {
        // Loại message
        public MessageType Type { get; set; }

        // ID dùng để liên kết Request và Response
        public string RequestId { get; set; }

        // Dữ liệu của message
        public object? Payload { get; set; }

        public NetworkMessage() 
        {
            RequestId = Guid.NewGuid().ToString();
        }

        public NetworkMessage(MessageType type, object? payload, string? requestId = null)
        {
            Type = type;
            Payload = payload;
            // Nếu không có RequestId thì tạo ID mới
            RequestId = string.IsNullOrEmpty(requestId) ? Guid.NewGuid().ToString() : requestId;
        }

        // Phương thức hỗ trợ Deserialize Payload an toàn (khi Payload là JsonElement hoặc Object)
        public T? GetPayload<T>()
        {
            if (Payload == null) return default;
            if (Payload is T target) return target;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            options.Converters.Add(new JsonStringEnumConverter());

            try
            {
                if (Payload is JsonElement element)
                {
                    if (typeof(T) == typeof(PlayerListResponse))
                    {
                        if (element.ValueKind == JsonValueKind.Object)
                        {
                            var res = JsonSerializer.Deserialize<PlayerListResponse>(element.GetRawText(), options);
                            return (T?)(object?)res;
                        }
                        if (element.ValueKind == JsonValueKind.Array)
                        {
                            var names = JsonSerializer.Deserialize<List<string>>(element.GetRawText(), options) ?? new();
                            var res = new PlayerListResponse { PlayerNames = names };
                            return (T?)(object?)res;
                        }
                    }

                    return JsonSerializer.Deserialize<T>(element.GetRawText(), options);
                }

                string json = JsonSerializer.Serialize(Payload, options);
                return JsonSerializer.Deserialize<T>(json, options);
            }
            catch
            {
                return default;
            }
        }
    }
}
