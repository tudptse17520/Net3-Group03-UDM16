using System.Text;

namespace CaroShared.Protocol
{
    
    public sealed class MessageFrameDecoder
    {
        private readonly StringBuilder _buffer = new();

       
        public IReadOnlyList<string> Decode(string incomingData)
        {
            if (string.IsNullOrEmpty(incomingData))
            {
                return Array.Empty<string>();
            }

            _buffer.Append(incomingData);

            var messages = new List<string>();

            while (true)
            {
                var delimiterIndex = _buffer.ToString().IndexOf('\n');

                if (delimiterIndex < 0)
                {
                    break;
                }

                var message = _buffer
                    .ToString(0, delimiterIndex)
                    .Trim();

                _buffer.Remove(0, delimiterIndex + 1);

                if (message.Length > 0)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }

        
        public string GetRemainingBuffer()
        {
            return _buffer.ToString();
        }

        public void Clear()
        {
            _buffer.Clear();
        }
    }
}
