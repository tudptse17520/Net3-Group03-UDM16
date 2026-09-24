using System.Net.Sockets;
using CaroShared.Enums;

namespace CaroClient.Network;

public sealed class ServerResponseException(ErrorCode code, string message) : IOException(message)
{
    public ErrorCode Code { get; } = code;
}

public static class ConnectionDiagnostics
{
    public static string Explain(Exception error, string endpoint) => error switch
    {
        ServerResponseException { Code: ErrorCode.UsernameInUse } => "Tên người chơi này đang được sử dụng. Hãy chọn tên khác.",
        SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain } => "Không tìm thấy địa chỉ máy chủ. Hãy kiểm tra tên miền.",
        SocketException { SocketErrorCode: SocketError.ConnectionRefused } => $"Máy chủ {endpoint} chưa nhận kết nối. Kiểm tra máy chủ đã mở và cổng đã đúng.",
        SocketException { SocketErrorCode: SocketError.TimedOut } or TimeoutException or OperationCanceledException => $"Máy chủ {endpoint} chưa phản hồi. Kiểm tra địa chỉ, mạng và quyền truy cập rồi thử lại.",
        InvalidDataException => error.Message,
        FileNotFoundException => "Chưa tìm thấy máy chủ đi kèm. Hãy cài lại Caro hoặc chọn kết nối máy chủ khác.",
        _ => $"Không thể kết nối đến {endpoint}. Vui lòng kiểm tra mạng rồi thử lại."
    };
}

public static class ClientLog
{
    private static readonly object Gate = new();
    public static void Write(string message)
    {
        try
        {
            lock (Gate)
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Caro", "Logs");
                Directory.CreateDirectory(folder);
                File.AppendAllText(Path.Combine(folder, $"caro-{Environment.ProcessId}.log"),
                    $"{DateTime.UtcNow:O} {message.Replace('\r', ' ').Replace('\n', ' ')}{Environment.NewLine}");
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
