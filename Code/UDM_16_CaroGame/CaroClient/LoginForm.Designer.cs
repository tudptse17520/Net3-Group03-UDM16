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
            LblAppTitle = new Label();
            LblSubtitle = new Label();
            pnlCard = new Soft3DPanel();
            LblNickname = new Label();
            pnlNickname = new RoundedInputPanel();
            TxtNickname = new TextBox();
            LblServerIp = new Label();
            pnlServerIp = new RoundedInputPanel();
            TxtServerIp = new TextBox();
            LblPort = new Label();
            pnlPort = new RoundedInputPanel();
            TxtPort = new TextBox();
            BtnConnect = new PillButton();
            pnlCard.SuspendLayout();
            pnlNickname.SuspendLayout();
            pnlServerIp.SuspendLayout();
            pnlPort.SuspendLayout();
            SuspendLayout();

            // ══════════════════════════════════════════════════════════════════
            //  HEADER (APP TITLE & SUBTITLE)
            // ══════════════════════════════════════════════════════════════════
            LblAppTitle.BackColor = Color.Transparent;
            LblAppTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            LblAppTitle.ForeColor = CaroTheme.TextDark;
            LblAppTitle.Location = new Point(0, 18);
            LblAppTitle.Name = "LblAppTitle";
            LblAppTitle.Size = new Size(480, 38);
            LblAppTitle.Text = "CARO ONLINE";
            LblAppTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblSubtitle.BackColor = Color.Transparent;
            LblSubtitle.Font = new Font("Segoe UI", 10.5F, FontStyle.Italic);
            LblSubtitle.ForeColor = CaroTheme.TextMuted;
            LblSubtitle.Location = new Point(0, 54);
            LblSubtitle.Name = "LblSubtitle";
            LblSubtitle.Size = new Size(480, 22);
            LblSubtitle.Text = "ĐĂNG NHẬP & KẾT NỐI";
            LblSubtitle.TextAlign = ContentAlignment.MiddleCenter;

            // ══════════════════════════════════════════════════════════════════
            //  CENTRAL SOFT 3D CARD
            // ══════════════════════════════════════════════════════════════════
            pnlCard.CornerRadius = 18;
            pnlCard.Location = new Point(40, 86);
            pnlCard.Name = "pnlCard";
            pnlCard.Size = new Size(400, 280);
            pnlCard.TabIndex = 0;
            pnlCard.Controls.Add(LblNickname);
            pnlCard.Controls.Add(pnlNickname);
            pnlCard.Controls.Add(LblServerIp);
            pnlCard.Controls.Add(pnlServerIp);
            pnlCard.Controls.Add(LblPort);
            pnlCard.Controls.Add(pnlPort);
            pnlCard.Controls.Add(BtnConnect);

            // LblNickname
            LblNickname.AutoSize = true;
            LblNickname.BackColor = Color.Transparent;
            LblNickname.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblNickname.ForeColor = CaroTheme.TextDark;
            LblNickname.Location = new Point(25, 20);
            LblNickname.Name = "LblNickname";
            LblNickname.Size = new Size(74, 17);
            LblNickname.Text = "Nickname:";

            // pnlNickname (Rounded Input Container)
            pnlNickname.CornerRadius = 10;
            pnlNickname.Location = new Point(25, 42);
            pnlNickname.Name = "pnlNickname";
            pnlNickname.Size = new Size(350, 36);
            pnlNickname.TabIndex = 1;
            pnlNickname.Controls.Add(TxtNickname);

            // TxtNickname
            TxtNickname.BackColor = Color.FromArgb(255, 253, 248);
            TxtNickname.BorderStyle = BorderStyle.None;
            TxtNickname.Font = new Font("Segoe UI", 10.5F);
            TxtNickname.ForeColor = CaroTheme.TextDark;
            TxtNickname.Location = new Point(10, 8);
            TxtNickname.Name = "TxtNickname";
            TxtNickname.Size = new Size(330, 19);
            TxtNickname.TabIndex = 0;

            // LblServerIp
            LblServerIp.AutoSize = true;
            LblServerIp.BackColor = Color.Transparent;
            LblServerIp.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblServerIp.ForeColor = CaroTheme.TextDark;
            LblServerIp.Location = new Point(25, 90);
            LblServerIp.Name = "LblServerIp";
            LblServerIp.Size = new Size(67, 17);
            LblServerIp.Text = "Server IP:";

            // pnlServerIp
            pnlServerIp.CornerRadius = 10;
            pnlServerIp.Location = new Point(25, 112);
            pnlServerIp.Name = "pnlServerIp";
            pnlServerIp.Size = new Size(220, 36);
            pnlServerIp.TabIndex = 2;
            pnlServerIp.Controls.Add(TxtServerIp);

            // TxtServerIp
            TxtServerIp.BackColor = Color.FromArgb(255, 253, 248);
            TxtServerIp.BorderStyle = BorderStyle.None;
            TxtServerIp.Font = new Font("Segoe UI", 10.5F);
            TxtServerIp.ForeColor = CaroTheme.TextDark;
            TxtServerIp.Location = new Point(10, 8);
            TxtServerIp.Name = "TxtServerIp";
            TxtServerIp.Size = new Size(200, 19);
            TxtServerIp.TabIndex = 0;
            TxtServerIp.Text = "127.0.0.1";

            // LblPort
            LblPort.AutoSize = true;
            LblPort.BackColor = Color.Transparent;
            LblPort.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblPort.ForeColor = CaroTheme.TextDark;
            LblPort.Location = new Point(260, 90);
            LblPort.Name = "LblPort";
            LblPort.Size = new Size(38, 17);
            LblPort.Text = "Port:";

            // pnlPort
            pnlPort.CornerRadius = 10;
            pnlPort.Location = new Point(260, 112);
            pnlPort.Name = "pnlPort";
            pnlPort.Size = new Size(115, 36);
            pnlPort.TabIndex = 3;
            pnlPort.Controls.Add(TxtPort);

            // TxtPort
            TxtPort.BackColor = Color.FromArgb(255, 253, 248);
            TxtPort.BorderStyle = BorderStyle.None;
            TxtPort.Font = new Font("Segoe UI", 10.5F);
            TxtPort.ForeColor = CaroTheme.TextDark;
            TxtPort.Location = new Point(10, 8);
            TxtPort.Name = "TxtPort";
            TxtPort.Size = new Size(95, 19);
            TxtPort.TabIndex = 0;
            TxtPort.Text = "8888";

            // BtnConnect (Pill Button)
            BtnConnect.BackColor = CaroTheme.Background;
            BtnConnect.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            BtnConnect.GlyphIcon = "⚡";
            BtnConnect.Location = new Point(75, 185);
            BtnConnect.Name = "BtnConnect";
            BtnConnect.Size = new Size(250, 44);
            BtnConnect.TabIndex = 4;
            BtnConnect.Text = "KẾT NỐI";
            BtnConnect.Click += BtnConnect_Click;

            // ══════════════════════════════════════════════════════════════════
            //  FORM CONFIGURATION
            // ══════════════════════════════════════════════════════════════════
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AcceptButton = BtnConnect;
            BackColor = CaroTheme.Background;
            ClientSize = new Size(480, 400);
            Controls.Add(LblAppTitle);
            Controls.Add(LblSubtitle);
            Controls.Add(pnlCard);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Đăng Nhập Game Caro";
            pnlPort.ResumeLayout(false);
            pnlPort.PerformLayout();
            pnlServerIp.ResumeLayout(false);
            pnlServerIp.PerformLayout();
            pnlNickname.ResumeLayout(false);
            pnlNickname.PerformLayout();
            pnlCard.ResumeLayout(false);
            pnlCard.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label LblAppTitle;
        private Label LblSubtitle;
        private Soft3DPanel pnlCard;
        private Label LblNickname;
        private RoundedInputPanel pnlNickname;
        private TextBox TxtNickname;
        private Label LblServerIp;
        private RoundedInputPanel pnlServerIp;
        private TextBox TxtServerIp;
        private Label LblPort;
        private RoundedInputPanel pnlPort;
        private TextBox TxtPort;
        private PillButton BtnConnect;
    }
}