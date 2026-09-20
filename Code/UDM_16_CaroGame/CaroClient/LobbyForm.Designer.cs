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

            cardPlayers = new Soft3DPanel();
            LblPlayers = new Label();
            pnlPlayersContainer = new RoundedInputPanel();
            LstPlayers = new ListBox();
            BtnChallenge = new PillButton();

            cardRooms = new Soft3DPanel();
            LblRoomList = new Label();
            pnlRoomsContainer = new RoundedInputPanel();
            LstRooms = new ListBox();

            cardActions = new Soft3DPanel();
            lblActionsHeader = new Label();
            LblRoomCode = new Label();
            pnlRoomCode = new RoundedInputPanel();
            TxtRoomCode = new TextBox();
            BtnJoinRoom = new CaroClient.PillButton();
            BtnRefresh = new CaroClient.PillButton();
            BtnMatchHistory = new CaroClient.PillButton();
            BtnPersonalization = new CaroClient.PillButton();
            BtnLogout = new CaroClient.PillButton();

            cardPlayers.SuspendLayout();
            pnlPlayersContainer.SuspendLayout();
            cardRooms.SuspendLayout();
            pnlRoomsContainer.SuspendLayout();
            cardActions.SuspendLayout();
            pnlRoomCode.SuspendLayout();
            SuspendLayout();

            // ==========================================
            // LblTitle
            // ==========================================
            LblTitle.BackColor = Color.Transparent;
            LblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblTitle.ForeColor = CaroTheme.TextDark;
            LblTitle.Location = new Point(0, 15);
            LblTitle.Name = "LblTitle";
            LblTitle.Size = new Size(880, 36);
            LblTitle.Text = "CARO ONLINE - SẢNH CHỜ";
            LblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // ==========================================
            // LblWelcome
            // ==========================================
            LblWelcome.AutoEllipsis = true;
            LblWelcome.BackColor = Color.Transparent;
            LblWelcome.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
            LblWelcome.ForeColor = CaroTheme.TextMuted;
            LblWelcome.Location = new Point(0, 50);
            LblWelcome.Name = "LblWelcome";
            LblWelcome.Size = new Size(880, 22);
            LblWelcome.Text = "Xin chào, Player!";
            LblWelcome.TextAlign = ContentAlignment.MiddleCenter;

            // ==========================================
            // cardPlayers (Soft3DPanel - Thẻ người chơi)
            // ==========================================
            cardPlayers.BackColor = Color.Transparent;
            cardPlayers.CornerRadius = 18;
            cardPlayers.Location = new Point(20, 80);
            cardPlayers.Name = "cardPlayers";
            cardPlayers.Size = new Size(260, 415);
            cardPlayers.Controls.Add(LblPlayers);
            cardPlayers.Controls.Add(pnlPlayersContainer);
            cardPlayers.Controls.Add(BtnChallenge);

            // LblPlayers
            LblPlayers.BackColor = Color.Transparent;
            LblPlayers.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            LblPlayers.ForeColor = CaroTheme.TextDark;
            LblPlayers.Location = new Point(16, 16);
            LblPlayers.Name = "LblPlayers";
            LblPlayers.Size = new Size(228, 24);
            LblPlayers.Text = "Người chơi online (0):";
            LblPlayers.TextAlign = ContentAlignment.MiddleLeft;

            // pnlPlayersContainer (Recessed container)
            pnlPlayersContainer.BackColor = Color.Transparent;
            pnlPlayersContainer.CornerRadius = 10;
            pnlPlayersContainer.Location = new Point(16, 46);
            pnlPlayersContainer.Name = "pnlPlayersContainer";
            pnlPlayersContainer.Padding = new Padding(4, 4, 4, 4);
            pnlPlayersContainer.Size = new Size(228, 305);
            pnlPlayersContainer.Controls.Add(LstPlayers);

            // LstPlayers
            LstPlayers.BackColor = Color.FromArgb(255, 253, 248);
            LstPlayers.BorderStyle = BorderStyle.None;
            LstPlayers.Dock = DockStyle.Fill;
            LstPlayers.Font = new Font("Segoe UI", 9.5F);
            LstPlayers.ForeColor = CaroTheme.TextDark;
            LstPlayers.FormattingEnabled = true;
            LstPlayers.ItemHeight = 36;
            LstPlayers.DrawMode = DrawMode.OwnerDrawFixed;
            LstPlayers.DrawItem += LstPlayers_DrawItem;
            LstPlayers.Name = "LstPlayers";

            // BtnChallenge
            BtnChallenge.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            BtnChallenge.Location = new Point(16, 361);
            BtnChallenge.Name = "BtnChallenge";
            BtnChallenge.Size = new Size(228, 38);
            BtnChallenge.Text = "THÁCH ĐẤU";
            BtnChallenge.Click += BtnChallenge_Click;

            // ==========================================
            // cardRooms (Soft3DPanel - Thẻ phòng chơi)
            // ==========================================
            cardRooms.BackColor = Color.Transparent;
            cardRooms.CornerRadius = 18;
            cardRooms.Location = new Point(295, 80);
            cardRooms.Name = "cardRooms";
            cardRooms.Size = new Size(335, 415);
            cardRooms.Controls.Add(LblRoomList);
            cardRooms.Controls.Add(pnlRoomsContainer);

            // LblRoomList
            LblRoomList.BackColor = Color.Transparent;
            LblRoomList.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            LblRoomList.ForeColor = CaroTheme.TextDark;
            LblRoomList.Location = new Point(16, 16);
            LblRoomList.Name = "LblRoomList";
            LblRoomList.Size = new Size(303, 24);
            LblRoomList.Text = "Phòng đang chơi (0):";
            LblRoomList.TextAlign = ContentAlignment.MiddleLeft;

            // pnlRoomsContainer (Recessed container)
            pnlRoomsContainer.BackColor = Color.Transparent;
            pnlRoomsContainer.CornerRadius = 10;
            pnlRoomsContainer.Location = new Point(16, 46);
            pnlRoomsContainer.Name = "pnlRoomsContainer";
            pnlRoomsContainer.Padding = new Padding(4, 4, 4, 4);
            pnlRoomsContainer.Size = new Size(303, 353);
            pnlRoomsContainer.Controls.Add(LstRooms);

            // LstRooms
            LstRooms.BackColor = Color.FromArgb(255, 253, 248);
            LstRooms.BorderStyle = BorderStyle.None;
            LstRooms.Dock = DockStyle.Fill;
            LstRooms.Font = new Font("Segoe UI", 9.5F);
            LstRooms.ForeColor = CaroTheme.TextDark;
            LstRooms.FormattingEnabled = true;
            LstRooms.ItemHeight = 20;
            LstRooms.Name = "LstRooms";

            // ==========================================
            // cardActions (Soft3DPanel - Thao tác)
            // ==========================================
            cardActions.BackColor = Color.Transparent;
            cardActions.CornerRadius = 18;
            cardActions.Location = new Point(645, 80);
            cardActions.Name = "cardActions";
            cardActions.Size = new Size(215, 415);
            cardActions.Controls.Add(lblActionsHeader);
            cardActions.Controls.Add(LblRoomCode);
            cardActions.Controls.Add(pnlRoomCode);
            cardActions.Controls.Add(BtnJoinRoom);
            cardActions.Controls.Add(BtnRefresh);
            cardActions.Controls.Add(BtnMatchHistory);
            cardActions.Controls.Add(BtnPersonalization);
            cardActions.Controls.Add(BtnLogout);

            // lblActionsHeader
            lblActionsHeader.BackColor = Color.Transparent;
            lblActionsHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblActionsHeader.ForeColor = CaroTheme.TextDark;
            lblActionsHeader.Location = new Point(16, 16);
            lblActionsHeader.Name = "lblActionsHeader";
            lblActionsHeader.Size = new Size(183, 24);
            lblActionsHeader.Text = "THAO TÁC";
            lblActionsHeader.TextAlign = ContentAlignment.MiddleLeft;

            // LblRoomCode
            LblRoomCode.BackColor = Color.Transparent;
            LblRoomCode.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            LblRoomCode.ForeColor = CaroTheme.TextMuted;
            LblRoomCode.Location = new Point(16, 48);
            LblRoomCode.Name = "LblRoomCode";
            LblRoomCode.Size = new Size(183, 20);
            LblRoomCode.Text = "Nhập mã phòng:";
            LblRoomCode.TextAlign = ContentAlignment.MiddleLeft;

            // pnlRoomCode
            pnlRoomCode.BackColor = Color.Transparent;
            pnlRoomCode.CornerRadius = 10;
            pnlRoomCode.Location = new Point(16, 70);
            pnlRoomCode.Name = "pnlRoomCode";
            pnlRoomCode.Padding = new Padding(8, 6, 8, 6);
            pnlRoomCode.Size = new Size(183, 34);
            pnlRoomCode.Controls.Add(TxtRoomCode);

            // TxtRoomCode
            TxtRoomCode.BackColor = Color.FromArgb(255, 253, 248);
            TxtRoomCode.BorderStyle = BorderStyle.None;
            TxtRoomCode.Dock = DockStyle.Fill;
            TxtRoomCode.Font = new Font("Segoe UI", 10F);
            TxtRoomCode.ForeColor = CaroTheme.TextDark;
            TxtRoomCode.Location = new Point(8, 6);
            TxtRoomCode.Name = "TxtRoomCode";
            TxtRoomCode.Size = new Size(167, 18);

            // BtnJoinRoom
            BtnJoinRoom.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnJoinRoom.Location = new Point(16, 115);
            BtnJoinRoom.Name = "BtnJoinRoom";
            BtnJoinRoom.Size = new Size(183, 38);
            BtnJoinRoom.Text = "VÀO PHÒNG";
            BtnJoinRoom.Click += BtnJoinRoom_Click;

            // BtnRefresh
            BtnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnRefresh.IsSecondary = true;
            BtnRefresh.Location = new Point(16, 165);
            BtnRefresh.Name = "BtnRefresh";
            BtnRefresh.Size = new Size(183, 38);
            BtnRefresh.Text = "LÀM MỚI";
            BtnRefresh.Click += BtnRefresh_Click;

            // BtnMatchHistory
            BtnMatchHistory.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnMatchHistory.Location = new Point(16, 215);
            BtnMatchHistory.Name = "BtnMatchHistory";
            BtnMatchHistory.Size = new Size(183, 38);
            BtnMatchHistory.Text = "LỊCH SỬ ĐẤU";
            BtnMatchHistory.Click += BtnMatchHistory_Click;

            // BtnPersonalization
            BtnPersonalization.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnPersonalization.Location = new Point(16, 265);
            BtnPersonalization.Name = "BtnPersonalization";
            BtnPersonalization.Size = new Size(183, 38);
            BtnPersonalization.Text = "CÁ NHÂN HOÁ";
            BtnPersonalization.Click += BtnPersonalization_Click;

            // BtnLogout
            BtnLogout.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnLogout.IsSecondary = true;
            BtnLogout.Location = new Point(16, 315);
            BtnLogout.Name = "BtnLogout";
            BtnLogout.Size = new Size(183, 38);
            BtnLogout.Text = "ĐĂNG XUẤT";
            BtnLogout.Click += BtnLogout_Click;

            // ==========================================
            // LobbyForm
            // ==========================================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = CaroTheme.Background;
            ClientSize = new Size(880, 520);
            Controls.Add(LblTitle);
            Controls.Add(LblWelcome);
            Controls.Add(cardPlayers);
            Controls.Add(cardRooms);
            Controls.Add(cardActions);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "LobbyForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sảnh Chờ - Game Caro";

            cardPlayers.ResumeLayout(false);
            pnlPlayersContainer.ResumeLayout(false);
            cardRooms.ResumeLayout(false);
            pnlRoomsContainer.ResumeLayout(false);
            cardActions.ResumeLayout(false);
            pnlRoomCode.ResumeLayout(false);
            pnlRoomCode.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Label LblTitle;
        private Label LblWelcome;

        private Soft3DPanel cardPlayers;
        private Label LblPlayers;
        private RoundedInputPanel pnlPlayersContainer;
        private ListBox LstPlayers;
        private PillButton BtnChallenge;

        private Soft3DPanel cardRooms;
        private Label LblRoomList;
        private RoundedInputPanel pnlRoomsContainer;
        private ListBox LstRooms;

        private Soft3DPanel cardActions;
        private Label lblActionsHeader;
        private Label LblRoomCode;
        private RoundedInputPanel pnlRoomCode;
        private TextBox TxtRoomCode;
        private PillButton BtnJoinRoom;
        private PillButton BtnRefresh;
        private PillButton BtnMatchHistory;
        private PillButton BtnPersonalization;
        private PillButton BtnLogout;
    }
}