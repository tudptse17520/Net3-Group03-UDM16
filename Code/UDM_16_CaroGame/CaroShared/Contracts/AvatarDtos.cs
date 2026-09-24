namespace CaroShared.Contracts
{
    public class AvatarUpdateRequest
    {
        public string Base64Image { get; set; } = string.Empty;
    }

    public class AvatarUpdateResponse
    {
        public bool Success { get; set; }
        public int AvatarVersion { get; set; }
        public string? Message { get; set; }
    }

    public class AvatarRemoveRequest
    {
    }

    public class AvatarRemoveResponse
    {
        public bool Success { get; set; }
        public int AvatarVersion { get; set; }
        public string? Message { get; set; }
    }

    public class AvatarRequest
    {
        public string PlayerId { get; set; } = string.Empty;
    }

    public class AvatarDataEvent
    {
        public string PlayerId { get; set; } = string.Empty;
        public int AvatarVersion { get; set; }
        public string Base64Image { get; set; } = string.Empty;
    }

    public class AvatarChangedEvent
    {
        public string PlayerId { get; set; } = string.Empty;
        public int AvatarVersion { get; set; }
        public bool HasAvatar { get; set; }
    }
}
