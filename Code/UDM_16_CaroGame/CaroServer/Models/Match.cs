namespace CaroServer.Models
{
    public class Match
    {
        public long Id { get; set; }

        public long PlayerXId { get; set; }

        public long PlayerOId { get; set; }

        public long? WinnerId { get; set; }

        public string Result { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime EndedAt { get; set; }
    }
}