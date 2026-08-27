namespace CaroServer.Logging
{
    public static class MatchLogger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "match_events.log");

        public static void Log(string roomId, string action, string details)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Room {roomId}] [{action}] {details}";
            Console.WriteLine(logLine);

            try
            {
                File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
            }
            catch
            {
                
            }
        }
    }
}