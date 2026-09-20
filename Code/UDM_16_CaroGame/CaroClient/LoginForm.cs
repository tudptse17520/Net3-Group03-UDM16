using System;
using System.Drawing;
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

            // Focus highlighting cho các ô nhập liệu
            TxtNickname.GotFocus += (s, e) => { pnlNickname.IsFocused = true; pnlNickname.Invalidate(); };
            TxtNickname.LostFocus += (s, e) => { pnlNickname.IsFocused = false; pnlNickname.Invalidate(); };

            TxtServerIp.GotFocus += (s, e) => { pnlServerIp.IsFocused = true; pnlServerIp.Invalidate(); };
            TxtServerIp.LostFocus += (s, e) => { pnlServerIp.IsFocused = false; pnlServerIp.Invalidate(); };

            TxtPort.GotFocus += (s, e) => { pnlPort.IsFocused = true; pnlPort.Invalidate(); };
            TxtPort.LostFocus += (s, e) => { pnlPort.IsFocused = false; pnlPort.Invalidate(); };
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var bgBrush = new SolidBrush(CaroTheme.Background);
            e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
        }

        private async void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNickname.Text))
            {
                CaroDialogForm.Show(this, "Vui lòng nhập Nickname của bạn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TxtNickname.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtServerIp.Text))
            {
                CaroDialogForm.Show(this, "Vui lòng nhập địa chỉ Server IP!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                TxtServerIp.Focus();
                return;
            }

            if (!int.TryParse(TxtPort.Text, out int port) || port <= 0 || port > 65535)
            {
                CaroDialogForm.Show(this, "Cổng kết nối (Port) không hợp lệ (1 - 65535)!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                CaroDialogForm.Show(this, $"Không thể kết nối đến Server {ServerIp}:{ServerPort}.\nVui lòng kiểm tra lại trạng thái máy chủ!", "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
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