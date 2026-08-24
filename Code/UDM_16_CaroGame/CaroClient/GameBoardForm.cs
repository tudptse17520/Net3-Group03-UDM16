using System.Text.Json;
using CaroClient.Network;
using CaroShared.Constants;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroClient
{
    public partial class GameBoardForm : Form
    {
        // ===== Biến đại diện cho ClientSocketManager =====
        private readonly ClientSocketManager _socketManager;

        public GameBoardForm()
        {
            InitializeComponent();

            // Khởi tạo ClientSocketManager
            _socketManager = new ClientSocketManager();

            // Đăng ký nhận sự kiện kết nối và sự kiện nhận thông điệp
            _socketManager.OnConnectionStatusChanged += SocketManager_OnConnectionStatusChanged;
            _socketManager.OnMessageReceived += SocketManager_OnMessageReceived;
        }

        // ===== Xử lý kết nối khi mở Form =====

        /// <summary>
        /// Khi giao diện vừa khởi chạy, gọi phương thức kết nối bất đồng bộ tới Server
        /// theo IP và Port cấu hình sẵn.
        /// </summary>
        private async void GameBoardForm_Load(object sender, EventArgs e)
        {
            await _socketManager.ConnectAsync("127.0.0.1", NetworkConstants.DefaultPort);
        }

        // ===== Xử lý sự kiện trạng thái kết nối (Đồng bộ luồng UI) =====

        /// <summary>
        /// Cập nhật tiêu đề Form dựa trên kết quả trả về.
        /// Sử dụng Invoke để chuyển dữ liệu từ luồng mạng về luồng UI an toàn.
        /// </summary>
        private void SocketManager_OnConnectionStatusChanged(bool isConnected)
        {
            if (InvokeRequired)
            {
                Invoke(() => SocketManager_OnConnectionStatusChanged(isConnected));
                return;
            }

            // Cập nhật tiêu đề Form hoặc thanh trạng thái
            Text = isConnected
                ? "Caro Game - Đã kết nối"
                : "Caro Game - Mất kết nối";
        }

        // ===== Xử lý dữ liệu nhận từ Server (Đồng bộ luồng UI) =====

        /// <summary>
        /// Trong phương thức hứng sự kiện nhận thông điệp:
        /// - Kiểm tra xem mã đang chạy có ở luồng giao diện (UI Thread) hay không.
        /// - Sử dụng cơ chế gọi ủy quyền (Invoke) để chuyển dữ liệu từ luồng đọc mạng
        ///   ngầm về luồng UI an toàn, tránh lỗi tranh chấp luồng (Cross-thread).
        /// </summary>
        private void SocketManager_OnMessageReceived(string rawMessage)
        {
            if (InvokeRequired)
            {
                BeginInvoke(() => SocketManager_OnMessageReceived(rawMessage));
                return;
            }

            try
            {
                // Deserialize JSON thành NetworkMessage
                var message = JsonSerializer.Deserialize<NetworkMessage>(rawMessage);
                if (message == null) return;

                // Kiểm tra loại thông điệp: nước đi từ đối thủ
                if (message.Type == MessageType.MoveMadeEvent && message.Payload != null)
                {
                    // Trích xuất tọa độ nước đi từ Payload
                    var moveData = JsonSerializer.Deserialize<MakeMoveRequest>(
                        message.Payload.ToString()!);

                    if (moveData != null)
                    {
                        int row = moveData.Y;
                        int col = moveData.X;

                        // Xử lý nước đi nhận được từ đối thủ
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi parse message (tạm hiển thị trên title)
                Text = $"Caro Game - Lỗi: {ex.Message}";
            }
        }

        // ===== Xử lý đóng ứng dụng =====

        /// <summary>
        /// Đăng ký sự kiện đóng Form để chủ động gọi phương thức ngắt kết nối Socket,
        /// tránh tình trạng treo luồng ngầm hoặc để lại kết nối rác trên Server.
        /// </summary>
        private void GameBoardForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _socketManager.Disconnect();
        }
    }
}
