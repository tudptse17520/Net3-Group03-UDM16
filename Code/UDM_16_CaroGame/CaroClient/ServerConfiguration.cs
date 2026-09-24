using System.Net;
using System.Text.Json;
using CaroShared.Constants;

namespace CaroClient;

public sealed record ServerConfiguration
{
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = NetworkConstants.DefaultPort;
    public bool AutoStartLocalServer { get; init; } = true;
    public bool IsLocal => IsLoopbackHost(Host);
    public static bool IsLoopbackHost(string? host) => string.Equals(host?.Trim(), "localhost", StringComparison.OrdinalIgnoreCase)
        || (IPAddress.TryParse(host?.Trim().Trim('[', ']'), out var address) && IPAddress.IsLoopback(address));
    public bool UsesLocalServer => IsLocal && AutoStartLocalServer;
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Host) || Port is < 1 or > 65535)
            throw new InvalidDataException("Vui lòng nhập địa chỉ máy chủ và cổng từ 1 đến 65535.");
        if (IPAddress.TryParse(Host.Trim('[', ']'), out var address) && (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)))
            throw new InvalidDataException("Hãy nhập địa chỉ của máy chủ. Không thể kết nối tới 0.0.0.0 hoặc ::.");
        if (Uri.CheckHostName(Host.Trim('[', ']')) == UriHostNameType.Unknown)
            throw new InvalidDataException("Địa chỉ máy chủ cần là IP hoặc tên miền, không kèm http:// hay số cổng.");
    }
    public static async Task<ServerConfiguration> LoadAsync(CancellationToken token)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "server-config.json");
        if (!File.Exists(path)) return new();
        using var stream = File.OpenRead(path);
        var config = await JsonSerializer.DeserializeAsync<ServerConfiguration>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, token) ?? new();
        config.Validate();
        return config with { Host = config.Host.Trim() };
    }
}
