using System.Net.Sockets;
using System.Text;
using CaroShared.Constants;

namespace CaroClient.Network
{
    /// <summary>
    /// Bộ quản lý kết nối Socket phía Client.
    /// Đảm nhận việc kết nối, gửi/nhận dữ liệu và ngắt kết nối với Server.
    /// </summary>
    public class ClientSocketManager
    {
        // ===== Đối tượng cốt lõi quản lý mạng =====

        // Đối tượng TcpClient đảm nhận việc mở và duy trì kết nối TCP với máy chủ
        private TcpClient? _tcpClient;

        // Đối tượng NetworkStream dùng để đọc/ghi luồng dữ liệu hai chiều
        private NetworkStream? _stream;

        // Đối tượng CancellationTokenSource để hủy luồng lắng nghe khi đóng ứng dụng hoặc ngắt kết nối
        private CancellationTokenSource? _cts;

        // ===== Sự kiện (Events) để báo cáo về giao diện =====

        /// <summary>
        /// Sự kiện nhận dữ liệu: Bắn ra chuỗi thông điệp khi đọc thành công dữ liệu từ Server.
        /// </summary>
        public event Action<string>? OnMessageReceived;

        /// <summary>
        /// Sự kiện trạng thái kết nối: Thông báo trạng thái kết nối thành công (true) hoặc bị ngắt kết nối (false).
        /// </summary>
        public event Action<bool>? OnConnectionStatusChanged;

        // ===== Các phương thức xử lý chính =====

        /// <summary>
        /// Phương thức Kết nối bất đồng bộ:
        /// Nhận địa chỉ IP và Cổng (Port), tiến hành kết nối tới Server,
        /// khởi tạo luồng đọc dữ liệu ngầm và phát sự kiện báo thành công.
        /// </summary>
        /// <param name="ip">Địa chỉ IP của Server</param>
        /// <param name="port">Cổng (Port) của Server</param>
        public async Task ConnectAsync(string ip, int port)
        {
            try
            {
                // Tạo TcpClient mới và kết nối tới Server
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(ip, port);

                // Lấy NetworkStream từ TcpClient để đọc/ghi dữ liệu
                _stream = _tcpClient.GetStream();

                // Khởi tạo CancellationTokenSource để có thể hủy luồng lắng nghe sau này
                _cts = new CancellationTokenSource();

                // Phát sự kiện báo kết nối thành công
                OnConnectionStatusChanged?.Invoke(true);

                // Chạy luồng lắng nghe ngầm (không await để không block)
                _ = Task.Run(() => ListenAsync(_cts.Token));
            }
            catch (Exception)
            {
                // Kết nối thất bại → phát sự kiện báo mất kết nối
                OnConnectionStatusChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Phương thức Gửi thông điệp:
        /// Chuyển đổi chuỗi văn bản thành mảng byte (sử dụng chuẩn mã hóa UTF-8),
        /// thêm ký tự phân cách chuỗi (dấu xuống dòng \n) và ghi dữ liệu xuống luồng mạng.
        /// </summary>
        /// <param name="message">Chuỗi thông điệp cần gửi</param>
        public void SendMessage(string message)
        {
            try
            {
                if (_stream == null || !_tcpClient!.Connected)
                    return;

                // Thêm ký tự phân cách (MessageDelimiter = "\n") vào cuối chuỗi
                string data = message + NetworkConstants.MessageDelimiter;

                // Chuyển đổi chuỗi thành mảng byte sử dụng chuẩn UTF-8
                byte[] bytes = Encoding.UTF8.GetBytes(data);

                // Ghi dữ liệu xuống luồng mạng
                _stream.Write(bytes, 0, bytes.Length);
                _stream.Flush();
            }
            catch (Exception)
            {
                // Lỗi khi gửi → ngắt kết nối
                Disconnect();
            }
        }

        /// <summary>
        /// Phương thức Lắng nghe bất đồng bộ (private):
        /// Chạy vòng lặp đọc ngầm liên tục nhận byte dữ liệu từ Server,
        /// giải mã thành chuỗi văn bản, phân tách các gói tin dựa trên ký tự phân cách
        /// và kích hoạt sự kiện gửi dữ liệu về giao diện.
        /// </summary>
        /// <param name="token">Token để hủy vòng lặp lắng nghe</param>
        private async Task ListenAsync(CancellationToken token)
        {
            // Buffer để đọc dữ liệu từ stream
            byte[] buffer = new byte[4096];

            // StringBuilder để tích lũy dữ liệu nhận được (có thể nhận không đầy đủ 1 gói tin)
            StringBuilder sb = new StringBuilder();

            try
            {
                // Vòng lặp đọc liên tục cho đến khi bị hủy
                while (!token.IsCancellationRequested)
                {
                    // Đọc dữ liệu bất đồng bộ từ NetworkStream
                    int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, token);

                    // Nếu ReadAsync trả về 0 byte → Server đã đóng kết nối
                    if (bytesRead == 0)
                        break;

                    // Giải mã byte thành chuỗi UTF-8 và tích lũy vào StringBuilder
                    string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    sb.Append(chunk);

                    // Phân tách các gói tin dựa trên ký tự phân cách (\n)
                    string accumulated = sb.ToString();
                    string delimiter = NetworkConstants.MessageDelimiter;

                    int delimiterIndex;
                    while ((delimiterIndex = accumulated.IndexOf(delimiter)) >= 0)
                    {
                        // Trích xuất một gói tin hoàn chỉnh
                        string completeMessage = accumulated.Substring(0, delimiterIndex);
                        accumulated = accumulated.Substring(delimiterIndex + delimiter.Length);

                        // Kích hoạt sự kiện gửi dữ liệu về giao diện
                        if (!string.IsNullOrWhiteSpace(completeMessage))
                        {
                            OnMessageReceived?.Invoke(completeMessage);
                        }
                    }

                    // Cập nhật lại StringBuilder với phần dữ liệu chưa đầy đủ (chờ nhận tiếp)
                    sb.Clear();
                    sb.Append(accumulated);
                }
            }
            catch (OperationCanceledException)
            {
                // Bị hủy bởi CancellationToken → thoát bình thường
            }
            catch (Exception)
            {
                // Lỗi đọc dữ liệu (ví dụ: mất kết nối)
            }
            finally
            {
                // Khi thoát vòng lặp → gọi ngắt kết nối
                Disconnect();
            }
        }

        /// <summary>
        /// Phương thức Ngắt kết nối:
        /// Đóng luồng ghi/đọc, đóng kết nối Socket, giải phóng tài nguyên
        /// và phát sự kiện báo mất kết nối.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                // Hủy CancellationTokenSource để dừng luồng lắng nghe
                _cts?.Cancel();

                // Đóng NetworkStream (luồng đọc/ghi)
                _stream?.Close();

                // Đóng kết nối TcpClient
                _tcpClient?.Close();
            }
            catch (Exception)
            {
                // Bỏ qua lỗi khi đóng tài nguyên
            }
            finally
            {
                // Giải phóng tài nguyên
                _stream = null;
                _tcpClient = null;
                _cts = null;

                // Phát sự kiện báo mất kết nối
                OnConnectionStatusChanged?.Invoke(false);
            }
        }
    }
}
