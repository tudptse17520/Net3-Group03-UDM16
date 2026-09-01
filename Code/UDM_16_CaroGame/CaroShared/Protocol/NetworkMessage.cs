using System;
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
    }
}
