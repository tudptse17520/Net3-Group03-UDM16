namespace CaroShared.Contracts
{
    public class PlayerDto
    {
        public string Username { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;

        public PlayerDto() { }

        public PlayerDto(string username, string nickname)
        {
            Username = username;
            Nickname = nickname;
        }
    }
}
