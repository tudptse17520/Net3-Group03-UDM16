using System.Drawing;
using System.Windows.Forms;

namespace CaroClient
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            LblTitle = new Label();
            LblNickname = new Label();
            TxtNickname = new TextBox();
            LblServerIp = new Label();
            TxtServerIp = new TextBox();
            LblPort = new Label();
            TxtPort = new TextBox();
            BtnConnect = new Button();
            SuspendLayout();

            // LblTitle (Tiêu đề màu vàng đồng cổ điển)
            LblTitle.BackColor = Color.Transparent;
            LblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            LblTitle.ForeColor = ColorTranslator.FromHtml("#E8C37B"); // Vàng đồng cổ
            LblTitle.Location = new Point(0, 25);
            LblTitle.Name = "LblTitle";
            LblTitle.Size = new Size(420, 35);
            LblTitle.Text = "ĐĂNG NHẬP GAME CARO";
            LblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // LblNickname
            LblNickname.BackColor = Color.Transparent;
            LblNickname.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblNickname.ForeColor = ColorTranslator.FromHtml("#F5E6CA"); // Màu kem cổ
            LblNickname.Location = new Point(45, 85);
            LblNickname.Name = "LblNickname";
            LblNickname.Size = new Size(90, 25);
            LblNickname.Text = "Nickname:";
            LblNickname.TextAlign = ContentAlignment.MiddleRight;

            // TxtNickname
            TxtNickname.BackColor = ColorTranslator.FromHtml("#FFFDF8");
            TxtNickname.BorderStyle = BorderStyle.FixedSingle;
            TxtNickname.Font = new Font("Segoe UI", 9.5F);
            TxtNickname.ForeColor = ColorTranslator.FromHtml("#3B2314");
            TxtNickname.Location = new Point(145, 85);
            TxtNickname.Name = "TxtNickname";
            TxtNickname.Size = new Size(220, 24);

            // LblServerIp
            LblServerIp.BackColor = Color.Transparent;
            LblServerIp.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblServerIp.ForeColor = ColorTranslator.FromHtml("#F5E6CA");
            LblServerIp.Location = new Point(45, 130);
            LblServerIp.Name = "LblServerIp";
            LblServerIp.Size = new Size(90, 25);
            LblServerIp.Text = "Server IP:";
            LblServerIp.TextAlign = ContentAlignment.MiddleRight;

            // TxtServerIp
            TxtServerIp.BackColor = ColorTranslator.FromHtml("#FFFDF8");
            TxtServerIp.BorderStyle = BorderStyle.FixedSingle;
            TxtServerIp.Font = new Font("Segoe UI", 9.5F);
            TxtServerIp.ForeColor = ColorTranslator.FromHtml("#3B2314");
            TxtServerIp.Location = new Point(145, 130);
            TxtServerIp.Name = "TxtServerIp";
            TxtServerIp.Size = new Size(220, 24);
            TxtServerIp.Text = "127.0.0.1";

            // LblPort
            LblPort.BackColor = Color.Transparent;
            LblPort.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblPort.ForeColor = ColorTranslator.FromHtml("#F5E6CA");
            LblPort.Location = new Point(45, 175);
            LblPort.Name = "LblPort";
            LblPort.Size = new Size(90, 25);
            LblPort.Text = "Port:";
            LblPort.TextAlign = ContentAlignment.MiddleRight;

            // TxtPort
            TxtPort.BackColor = ColorTranslator.FromHtml("#FFFDF8");
            TxtPort.BorderStyle = BorderStyle.FixedSingle;
            TxtPort.Font = new Font("Segoe UI", 9.5F);
            TxtPort.ForeColor = ColorTranslator.FromHtml("#3B2314");
            TxtPort.Location = new Point(145, 175);
            TxtPort.Name = "TxtPort";
            TxtPort.Size = new Size(220, 24);
            TxtPort.Text = "8888";

            // BtnConnect (Màu gỗ đậm viền vàng đồng)
            BtnConnect.BackColor = ColorTranslator.FromHtml("#5C3A21"); // Nâu gỗ
            BtnConnect.Cursor = Cursors.Hand;
            BtnConnect.FlatAppearance.BorderSize = 0;
            BtnConnect.FlatStyle = FlatStyle.Flat;
            BtnConnect.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            BtnConnect.ForeColor = ColorTranslator.FromHtml("#FFE8A3"); // Chữ vàng ánh kim
            BtnConnect.Location = new Point(145, 230);
            BtnConnect.Name = "BtnConnect";
            BtnConnect.Size = new Size(220, 40);
            BtnConnect.Text = "KẾT NỐI";
            BtnConnect.UseVisualStyleBackColor = false;
            BtnConnect.Click += BtnConnect_Click;

            // LoginForm (Nền nâu sẫm cổ xưa)
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = ColorTranslator.FromHtml("#3B2314"); // Nâu đậm vân gỗ
            ClientSize = new Size(420, 300);
            Controls.Add(LblTitle);
            Controls.Add(LblNickname);
            Controls.Add(TxtNickname);
            Controls.Add(LblServerIp);
            Controls.Add(TxtServerIp);
            Controls.Add(LblPort);
            Controls.Add(TxtPort);
            Controls.Add(BtnConnect);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Đăng Nhập Game Caro";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label LblTitle;
        private Label LblNickname;
        private TextBox TxtNickname;
        private Label LblServerIp;
        private TextBox TxtServerIp;
        private Label LblPort;
        private TextBox TxtPort;
        private Button BtnConnect;
    }
}