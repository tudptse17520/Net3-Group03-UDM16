using System.Text.Json.Serialization;

namespace CaroShared.Contracts;

public static class RoomCodes
{
    public static string? Normalize(string? input)
    {
        string value = (input ?? "").Trim().Replace(" ", "").ToUpperInvariant();
        if (value.StartsWith("ROOM-")) value = value[5..];
        return value.Length == 6 && value.All(c => c is >= '0' and <= '9') ? "ROOM-" + value : null;
    }
    public static string Display(string id) => Normalize(id) is string normalized ? "ROOM - " + normalized[5..] : "Phòng";
}

public record RoomAccessRequest
{
    public string RoomId { get; init; } = "";
    public bool IsLocked { get; init; }
}
public record RoomPresenceEvent
{
    public RoomDto Room { get; init; } = new();
    public string? JoinedPlayerName { get; init; }
    public string? LeftPlayerName { get; init; }
}
public record ChatRequest
{
    public string RoomId { get; init; } = "";
    public string Text { get; init; } = "";
}
public record ChatEvent
{
    public Guid MessageId { get; init; }
    public string RoomId { get; init; } = "";
    public string SenderName { get; init; } = "";
    public string Text { get; init; } = "";
}
public record NewGameOfferDto
{
    public string RoomId { get; init; } = "";
    public Guid MatchIdentity { get; init; }
    public Guid OfferIdentity { get; init; }
    public string RequesterName { get; init; } = "";
    public DateTime ExpiresAtUtc { get; init; }
    public bool Accepted { get; init; }
    public string Message { get; init; } = "";
}
public record NewGameResponseRequest
{
    public string RoomId { get; init; } = "";
    public Guid OfferIdentity { get; init; }
    public bool Accept { get; init; }
}
