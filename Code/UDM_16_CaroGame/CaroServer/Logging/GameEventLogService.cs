using System.Text.Json;

namespace CaroServer.Logging
{
    // Ghi event theo dạng JSON Lines để dễ đọc, tìm kiếm và phân tích khi debug.
    public sealed class GameEventLogService
    {
        private readonly object _sync = new();
        private readonly string _logFilePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false
        };

        public GameEventLogService(string? logDirectory = null)
        {
            var directory = logDirectory ?? Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(directory);
            _logFilePath = Path.Combine(directory, "game-events.jsonl");
        }

        public string LogFilePath => _logFilePath;

        public void Log(
            string roomId,
            string eventType,
            string playerId = "",
            int x = -1,
            int y = -1,
            int playerSymbol = 0,
            bool isValid = true,
            string result = "",
            string message = "")
        {
            var entry = new GameEventLog
            {
                TimestampUtc = DateTime.UtcNow,
                RoomId = roomId,
                EventType = eventType,
                PlayerId = playerId,
                X = x,
                Y = y,
                PlayerSymbol = playerSymbol,
                IsValid = isValid,
                Result = result,
                Message = message
            };

            var json = JsonSerializer.Serialize(entry, _jsonOptions);
            lock (_sync)
            {
                File.AppendAllText(_logFilePath, json + Environment.NewLine);
            }

            Console.WriteLine($"[GameEvent] {entry.TimestampUtc:O} [{eventType}] Room={roomId} Player={playerId} {message}");
        }
    }
}
