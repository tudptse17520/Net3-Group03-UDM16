using System.Text;
namespace CaroShared.Protocol
{
    public class MessageFrameDecoder
    {
        private readonly StringBuilder _buffer = new();

        public List<string> Decode(string incomingData)
        {
            if (string.IsNullOrEmpty(incomingData))
                return new List<string>();

            _buffer.Append(incomingData);

            List<string> messages = new();

            while (true)
            {
                int delimiterIndex = _buffer
                    .ToString()
                    .IndexOf('\n');

                if (delimiterIndex < 0)
                    break;

                string message = _buffer
                    .ToString(0, delimiterIndex)
                    .Trim();

                _buffer.Remove(0, delimiterIndex + 1);

                if (!string.IsNullOrWhiteSpace(message))
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
