using System;
using System.ComponentModel; // Cần thiết cho DesignerSerializationVisibility
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    public partial class LobbyForm : Form
    {
        // Khắc phục lỗi WFO1000: Báo cho Designer bỏ qua Property này
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PlayerName { get; set; } = string.Empty;

        // Constructor mặc định cho Visual Studio Designer
        public LobbyForm()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        // Constructor chính nhận tên người chơi
        public LobbyForm(string playerName) : this()
        {
            PlayerName = playerName;
            LblWelcome.Text = $"Xin chào, {PlayerName}!";

            // Đăng ký sự kiện vẽ bo góc
            BtnJoinRoom.Paint += Button_Paint;
            BtnCreateRoom.Paint += Button_Paint;
            BtnRefresh.Paint += Button_Paint;
            BtnLogout.Paint += Button_Paint;
        }

        // Khắc phục cảnh báo CS8622: Thêm dấu ? cho object? sender
        private void Button_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;

            int borderRadius = 8;
            Rectangle rect = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);

            using (GraphicsPath path = GetRoundedPath(rect, borderRadius))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Tô màu nền
                using (SolidBrush brush = new SolidBrush(btn.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }

                // Vẽ viền vàng đồng cổ điển
                using (Pen pen = new Pen(ColorTranslator.FromHtml("#E8C37B"), 1.5f))
                {
                    e.Graphics.DrawPath(pen, path);
                }

                // Vẽ chữ giữa nút
                TextRenderer.DrawText(
                    e.Graphics,
                    btn.Text,
                    btn.Font,
                    rect,
                    btn.ForeColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );

                btn.Region = new Region(path);
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

        private void BtnJoinRoom_Click(object sender, EventArgs e)
        {
            string roomCode = TxtRoomCode.Text.Trim();

            if (LstRooms.SelectedItem != null && string.IsNullOrEmpty(roomCode))
            {
                roomCode = LstRooms.SelectedItem.ToString() ?? string.Empty;
            }

            if (string.IsNullOrEmpty(roomCode))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập mã phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show($"Đang tham gia phòng: {roomCode}", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCreateRoom_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Đang tạo phòng mới...", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LstRooms.Items.Clear();
            LstRooms.Items.Add("Phòng 101 (1/2)");
            LstRooms.Items.Add("Phòng 102 (Đang chơi)");
            LstRooms.Items.Add("Phòng 103 (1/2)");
            MessageBox.Show("Đã làm mới danh sách phòng!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (dialogResult == DialogResult.Yes)
            {
                this.Close();
            }
        }
    }
}