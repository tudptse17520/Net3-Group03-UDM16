namespace CaroServer.Models
{
    public class User
    {
        public long Id { get; set; }

        public string Nickname { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}