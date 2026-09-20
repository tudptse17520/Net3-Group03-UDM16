namespace CaroShared.Contracts
{
    public class PlayerInfoDto
    {
        public string PlayerName { get; set; } = string.Empty;
        public bool HasAvatar { get; set; }
        public int AvatarVersion { get; set; }
    }
}
