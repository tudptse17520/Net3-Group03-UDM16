using System.Net.Sockets;
using System.Text;
using CaroShared.Constants;

namespace CaroClient.Network
{
    /// <summary>
    /// Bộ quản lý kết nối Socket phía Client.
    /// </summary>
    public class ClientSocketManager
    {
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public event Action<string>? OnMessageReceived;
        public event Action<bool>? OnConnectionStatusChanged;

        /// <summary>
        /// Kết nối bất đồng bộ tới Server.
        /// </summary>
        /// <param name="ip">Địa chỉ IP của Server</param>
        /// <param name="port">Cổng (Port) của Server</param>
        public async Task ConnectAsync(string ip, int port)
        {
            try
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);

                _stream = _tcpClient.GetStream();
                _cts = new CancellationTokenSource();

                OnConnectionStatusChanged?.Invoke(true);

                _ = Task.Run(() => ListenAsync(_cts.Token));
            }
            catch (Exception)
            {
                OnConnectionStatusChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Gửi thông điệp tới Server.
        /// </summary>
        /// <param name="message">Chuỗi thông điệp cần gửi</param>
        public void SendMessage(string message)
        {
            try
            {
                if (_stream == null || !_tcpClient!.Connected)
                    return;

                string data = message + NetworkConstants.MessageDelimiter;
                byte[] bytes = Encoding.UTF8.GetBytes(data);

                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            StringBuilder sb = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, token);

                    if (bytesRead == 0)
                        break;

                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(chunk);

                    string accumulated = sb.ToString();
                    string delimiter = NetworkConstants.MessageDelimiter;

                    int delimiterIndex;
                    while ((delimiterIndex = accumulated.IndexOf(delimiter)) >= 0)
                    {
                        string completeMessage = accumulated.Substring(0, delimiterIndex);
                        accumulated = accumulated.Substring(delimiterIndex + delimiter.Length);

                        if (!string.IsNullOrWhiteSpace(completeMessage))
                        {
                            OnMessageReceived?.Invoke(completeMessage);
                        }
                    }

                    sb.Clear();
                    sb.Append(accumulated);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// Ngắt kết nối với Server và giải phóng tài nguyên.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _cts?.Cancel();
                _stream?.Close();
                _tcpClient?.Close();
            }
            catch (Exception)
            {
            }
            finally
            {
                _stream = null;
                _tcpClient = null;
                _cts = null;

                OnConnectionStatusChanged?.Invoke(false);
            }
        }
    }
}

