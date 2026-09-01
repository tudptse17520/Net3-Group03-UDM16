using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class LoginForm : Form
    {
        public string PlayerName { get; private set; } = string.Empty;
        public string ServerIp { get; private set; } = string.Empty;
        public int ServerPort { get; private set; }

        public LoginForm()
        {
            InitializeComponent();

            this.DoubleBuffered = true;

            // Vẽ bo góc và viền gỗ phong cách cổ điển cho nút
            BtnConnect.Paint += BtnConnect_Paint;

            /* NẾU CÓ ẢNH NỀN VÂN GỖ (wood_bg.jpg), HÃY BỎ COMMENT DÒNG DƯỚI:
            try 
            {
                this.BackgroundImage = Image.FromFile(@"Resources\wood_bg.jpg");
                this.BackgroundImageLayout = ImageLayout.Stretch;
            }
            catch { }
            */
        }

        private void BtnConnect_Paint(object? sender, PaintEventArgs e)
        {
            int borderRadius = 10;
            Rectangle rect = new Rectangle(0, 0, BtnConnect.Width - 1, BtnConnect.Height - 1);

            using (GraphicsPath path = GetRoundedPath(rect, borderRadius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Tô màu nền gỗ cho nút bấm
                using (SolidBrush brush = new SolidBrush(BtnConnect.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Vẽ viền vàng đồng giả kim loại quanh nút
                using (Pen pen = new Pen(ColorTranslator.FromHtml("#E8C37B"), 2))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                // Vẽ chữ vàng nổi bật giữa nút
                TextRenderer.DrawText(
                    e.Graphics,
                    BtnConnect.Text,
                    BtnConnect.Font,
                    rect,
                    BtnConnect.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );

                BtnConnect.Region = new Region(path);
            }
        }

        private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private async void BtnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNickname.Text))
            {
                MessageBox.Show("Vui lòng nhập Nickname!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TxtNickname.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtServerIp.Text))
            {
                MessageBox.Show("Vui lòng nhập Server IP!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TxtServerIp.Focus();
                return;
            }

            if (!int.TryParse(TxtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                MessageBox.Show("Port không hợp lệ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TxtPort.Focus();
                return;
            }

            PlayerName = TxtNickname.Text.Trim();
            ServerIp = TxtServerIp.Text.Trim();
            ServerPort = port;

            BtnConnect.Enabled = false;
            BtnConnect.Text = "ĐANG KẾT NỐI...";

            bool success = await CaroClient.Network.NetworkClient.Instance.ConnectAsync(ServerIp, ServerPort);
            if (!success)
            {
                MessageBox.Show($"Không thể kết nối đến Server {ServerIp}:{ServerPort}. Vui lòng kiểm tra lại Server!", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BtnConnect.Enabled = true;
                BtnConnect.Text = "KẾT NỐI";
                return;
            }

            // Gửi thông điệp LoginRequest chứa Nickname
            await CaroClient.Network.NetworkClient.Instance.SendLoginAsync(PlayerName);

            LobbyForm lobby = new LobbyForm(PlayerName);
            this.Hide();
            lobby.ShowDialog();
            this.Close();
        }
    }
}