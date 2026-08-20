using System.Drawing;
using System.Windows.Forms;

namespace CaroClient
{
    partial class LobbyForm
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
            LblWelcome = new Label();
            LstRooms = new ListBox();
            LblRoomList = new Label();
            LblRoomCode = new Label();
            TxtRoomCode = new TextBox();
            BtnJoinRoom = new Button();
            BtnCreateRoom = new Button();
            BtnRefresh = new Button();
            BtnLogout = new Button();
            SuspendLayout();

            // LblTitle
            LblTitle.BackColor = Color.Transparent;
            LblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblTitle.ForeColor = ColorTranslator.FromHtml("#E8C37B");
            LblTitle.Location = new Point(0, 15);
            LblTitle.Name = "LblTitle";
            LblTitle.Size = new Size(580, 40);
            LblTitle.Text = "SẢNH CHỜ GAME CARO";
            LblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // LblWelcome
            LblWelcome.BackColor = Color.Transparent;
            LblWelcome.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
            LblWelcome.ForeColor = ColorTranslator.FromHtml("#F5E6CA");
            LblWelcome.Location = new Point(0, 55);
            LblWelcome.Name = "LblWelcome";
            LblWelcome.Size = new Size(580, 25);
            LblWelcome.Text = "Xin chào, Player!";
            LblWelcome.TextAlign = ContentAlignment.MiddleCenter;

            // LblRoomList
            LblRoomList.AutoSize = true;
            LblRoomList.BackColor = Color.Transparent;
            LblRoomList.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            LblRoomList.ForeColor = ColorTranslator.FromHtml("#E8C37B");
            LblRoomList.Location = new Point(30, 95);
            LblRoomList.Name = "LblRoomList";
            LblRoomList.Size = new Size(153, 19);
            LblRoomList.Text = "Danh sách phòng chờ:";

            // LstRooms
            LstRooms.BackColor = ColorTranslator.FromHtml("#2A190E");
            LstRooms.BorderStyle = BorderStyle.FixedSingle;
            LstRooms.Font = new Font("Segoe UI", 10F);
            LstRooms.ForeColor = ColorTranslator.FromHtml("#FFFDF8");
            LstRooms.FormattingEnabled = true;
            LstRooms.ItemHeight = 17;
            LstRooms.Location = new Point(30, 120);
            LstRooms.Name = "LstRooms";
            LstRooms.Size = new Size(320, 223);

            // LblRoomCode
            LblRoomCode.AutoSize = true;
            LblRoomCode.BackColor = Color.Transparent;
            LblRoomCode.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            LblRoomCode.ForeColor = ColorTranslator.FromHtml("#F5E6CA");
            LblRoomCode.Location = new Point(370, 95);
            LblRoomCode.Name = "LblRoomCode";
            LblRoomCode.Size = new Size(106, 17);
            LblRoomCode.Text = "Nhập mã phòng:";

            // TxtRoomCode
            TxtRoomCode.BackColor = ColorTranslator.FromHtml("#FFFDF8");
            TxtRoomCode.BorderStyle = BorderStyle.FixedSingle;
            TxtRoomCode.Font = new Font("Segoe UI", 10F);
            TxtRoomCode.ForeColor = ColorTranslator.FromHtml("#3B2314");
            TxtRoomCode.Location = new Point(370, 120);
            TxtRoomCode.Name = "TxtRoomCode";
            TxtRoomCode.Size = new Size(180, 25);

            // BtnJoinRoom
            BtnJoinRoom.BackColor = ColorTranslator.FromHtml("#5C3A21");
            BtnJoinRoom.Cursor = Cursors.Hand;
            BtnJoinRoom.FlatAppearance.BorderSize = 0;
            BtnJoinRoom.FlatStyle = FlatStyle.Flat;
            BtnJoinRoom.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnJoinRoom.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
            BtnJoinRoom.Location = new Point(370, 155);
            BtnJoinRoom.Name = "BtnJoinRoom";
            BtnJoinRoom.Size = new Size(180, 35);
            BtnJoinRoom.Text = "VÀO PHÒNG";
            BtnJoinRoom.UseVisualStyleBackColor = false;
            BtnJoinRoom.Click += BtnJoinRoom_Click;

            // BtnCreateRoom
            BtnCreateRoom.BackColor = ColorTranslator.FromHtml("#5C3A21");
            BtnCreateRoom.Cursor = Cursors.Hand;
            BtnCreateRoom.FlatAppearance.BorderSize = 0;
            BtnCreateRoom.FlatStyle = FlatStyle.Flat;
            BtnCreateRoom.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnCreateRoom.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
            BtnCreateRoom.Location = new Point(370, 205);
            BtnCreateRoom.Name = "BtnCreateRoom";
            BtnCreateRoom.Size = new Size(180, 35);
            BtnCreateRoom.Text = "TẠO PHÒNG MỚI";
            BtnCreateRoom.UseVisualStyleBackColor = false;
            BtnCreateRoom.Click += BtnCreateRoom_Click;

            // BtnRefresh
            BtnRefresh.BackColor = ColorTranslator.FromHtml("#5C3A21");
            BtnRefresh.Cursor = Cursors.Hand;
            BtnRefresh.FlatAppearance.BorderSize = 0;
            BtnRefresh.FlatStyle = FlatStyle.Flat;
            BtnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnRefresh.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
            BtnRefresh.Location = new Point(370, 255);
            BtnRefresh.Name = "BtnRefresh";
            BtnRefresh.Size = new Size(180, 35);
            BtnRefresh.Text = "LÀM MỚI";
            BtnRefresh.UseVisualStyleBackColor = false;
            BtnRefresh.Click += BtnRefresh_Click;

            // BtnLogout
            BtnLogout.BackColor = ColorTranslator.FromHtml("#8B261D");
            BtnLogout.Cursor = Cursors.Hand;
            BtnLogout.FlatAppearance.BorderSize = 0;
            BtnLogout.FlatStyle = FlatStyle.Flat;
            BtnLogout.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnLogout.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
            BtnLogout.Location = new Point(370, 305);
            BtnLogout.Name = "BtnLogout";
            BtnLogout.Size = new Size(180, 35);
            BtnLogout.Text = "ĐĂNG XUẤT";
            BtnLogout.UseVisualStyleBackColor = false;
            BtnLogout.Click += BtnLogout_Click;

            // LobbyForm
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = ColorTranslator.FromHtml("#3B2314");
            ClientSize = new Size(580, 380);
            Controls.Add(LblTitle);
            Controls.Add(LblWelcome);
            Controls.Add(LblRoomList);
            Controls.Add(LstRooms);
            Controls.Add(LblRoomCode);
            Controls.Add(TxtRoomCode);
            Controls.Add(BtnJoinRoom);
            Controls.Add(BtnCreateRoom);
            Controls.Add(BtnRefresh);
            Controls.Add(BtnLogout);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "LobbyForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sảnh Chờ - Game Caro";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label LblTitle;
        private Label LblWelcome;
        private Label LblRoomList;
        private ListBox LstRooms;
        private Label LblRoomCode;
        private TextBox TxtRoomCode;
        private Button BtnJoinRoom;
        private Button BtnCreateRoom;
        private Button BtnRefresh;
        private Button BtnLogout;
    }
}