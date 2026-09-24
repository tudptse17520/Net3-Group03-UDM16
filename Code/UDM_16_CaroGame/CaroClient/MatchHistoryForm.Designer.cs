using System.Drawing;
using System.Windows.Forms;

namespace CaroClient
{
    partial class MatchHistoryForm
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
            LblPlayerName = new Label();

            // Stat cards
            cardTotal = new Soft3DPanel();
            LblStatTotalTitle = new Label();
            LblStatTotalValue = new Label();

            cardWin = new Soft3DPanel();
            LblStatWinTitle = new Label();
            LblStatWinValue = new Label();

            cardDraw = new Soft3DPanel();
            LblStatDrawTitle = new Label();
            LblStatDrawValue = new Label();

            cardLoss = new Soft3DPanel();
            LblStatLossTitle = new Label();
            LblStatLossValue = new Label();

            cardTable = new Soft3DPanel();
            DgvMatchHistory = new DataGridView();
            BtnRefresh = new PillButton();
            BtnClose = new PillButton();

            cardTotal.SuspendLayout();
            cardWin.SuspendLayout();
            cardDraw.SuspendLayout();
            cardLoss.SuspendLayout();
            cardTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DgvMatchHistory).BeginInit();
            SuspendLayout();

            // ============================
            // LblTitle
            // ============================
            LblTitle.BackColor = Color.Transparent;
            LblTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblTitle.ForeColor = CaroTheme.TextDark;
            LblTitle.Location = new Point(0, 15);
            LblTitle.Name = "LblTitle";
            LblTitle.Size = new Size(760, 32);
            LblTitle.Text = "LỊCH SỬ TRẬN ĐẤU";
            LblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // LblPlayerName
            // ============================
            LblPlayerName.AutoEllipsis = true;
            LblPlayerName.BackColor = Color.Transparent;
            LblPlayerName.Font = new Font("Segoe UI", 10F, FontStyle.Italic);
            LblPlayerName.ForeColor = CaroTheme.TextMuted;
            LblPlayerName.Location = new Point(0, 48);
            LblPlayerName.Name = "LblPlayerName";
            LblPlayerName.Size = new Size(760, 22);
            LblPlayerName.Text = "Người chơi: --";
            LblPlayerName.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // cardTotal (Soft3DPanel)
            // ============================
            cardTotal.BackColor = Color.Transparent;
            cardTotal.CornerRadius = 14;
            cardTotal.Location = new Point(30, 75);
            cardTotal.Name = "cardTotal";
            cardTotal.Size = new Size(163, 75);
            cardTotal.Controls.Add(LblStatTotalTitle);
            cardTotal.Controls.Add(LblStatTotalValue);

            LblStatTotalTitle.BackColor = Color.Transparent;
            LblStatTotalTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            LblStatTotalTitle.ForeColor = CaroTheme.TextMuted;
            LblStatTotalTitle.Location = new Point(5, 10);
            LblStatTotalTitle.Name = "LblStatTotalTitle";
            LblStatTotalTitle.Size = new Size(153, 18);
            LblStatTotalTitle.Text = "TỔNG SỐ TRẬN";
            LblStatTotalTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatTotalValue.BackColor = Color.Transparent;
            LblStatTotalValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatTotalValue.ForeColor = CaroTheme.TextDark;
            LblStatTotalValue.Location = new Point(5, 30);
            LblStatTotalValue.Name = "LblStatTotalValue";
            LblStatTotalValue.Size = new Size(153, 34);
            LblStatTotalValue.Text = "0";
            LblStatTotalValue.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // cardWin (Soft3DPanel)
            // ============================
            cardWin.BackColor = Color.Transparent;
            cardWin.CornerRadius = 14;
            cardWin.Location = new Point(209, 75);
            cardWin.Name = "cardWin";
            cardWin.Size = new Size(163, 75);
            cardWin.Controls.Add(LblStatWinTitle);
            cardWin.Controls.Add(LblStatWinValue);

            LblStatWinTitle.BackColor = Color.Transparent;
            LblStatWinTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            LblStatWinTitle.ForeColor = CaroTheme.TextMuted;
            LblStatWinTitle.Location = new Point(5, 10);
            LblStatWinTitle.Name = "LblStatWinTitle";
            LblStatWinTitle.Size = new Size(153, 18);
            LblStatWinTitle.Text = "SỐ TRẬN THẮNG";
            LblStatWinTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatWinValue.BackColor = Color.Transparent;
            LblStatWinValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatWinValue.ForeColor = Color.FromArgb(85, 107, 63); // Muted Forest Green
            LblStatWinValue.Location = new Point(5, 30);
            LblStatWinValue.Name = "LblStatWinValue";
            LblStatWinValue.Size = new Size(153, 34);
            LblStatWinValue.Text = "0";
            LblStatWinValue.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // cardDraw (Soft3DPanel)
            // ============================
            cardDraw.BackColor = Color.Transparent;
            cardDraw.CornerRadius = 14;
            cardDraw.Location = new Point(388, 75);
            cardDraw.Name = "cardDraw";
            cardDraw.Size = new Size(163, 75);
            cardDraw.Controls.Add(LblStatDrawTitle);
            cardDraw.Controls.Add(LblStatDrawValue);

            LblStatDrawTitle.BackColor = Color.Transparent;
            LblStatDrawTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            LblStatDrawTitle.ForeColor = CaroTheme.TextMuted;
            LblStatDrawTitle.Location = new Point(5, 10);
            LblStatDrawTitle.Name = "LblStatDrawTitle";
            LblStatDrawTitle.Size = new Size(153, 18);
            LblStatDrawTitle.Text = "SỐ TRẬN HÒA";
            LblStatDrawTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatDrawValue.BackColor = Color.Transparent;
            LblStatDrawValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatDrawValue.ForeColor = Color.FromArgb(70, 130, 180); // Muted Blue for Draw
            LblStatDrawValue.Location = new Point(5, 30);
            LblStatDrawValue.Name = "LblStatDrawValue";
            LblStatDrawValue.Size = new Size(153, 34);
            LblStatDrawValue.Text = "0";
            LblStatDrawValue.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // cardLoss (Soft3DPanel)
            // ============================
            cardLoss.BackColor = Color.Transparent;
            cardLoss.CornerRadius = 14;
            cardLoss.Location = new Point(567, 75);
            cardLoss.Name = "cardLoss";
            cardLoss.Size = new Size(163, 75);
            cardLoss.Controls.Add(LblStatLossTitle);
            cardLoss.Controls.Add(LblStatLossValue);

            LblStatLossTitle.BackColor = Color.Transparent;
            LblStatLossTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            LblStatLossTitle.ForeColor = CaroTheme.TextMuted;
            LblStatLossTitle.Location = new Point(5, 10);
            LblStatLossTitle.Name = "LblStatLossTitle";
            LblStatLossTitle.Size = new Size(153, 18);
            LblStatLossTitle.Text = "SỐ TRẬN THUA";
            LblStatLossTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatLossValue.BackColor = Color.Transparent;
            LblStatLossValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatLossValue.ForeColor = Color.FromArgb(147, 76, 61); // Muted Brick Red
            LblStatLossValue.Location = new Point(5, 30);
            LblStatLossValue.Name = "LblStatLossValue";
            LblStatLossValue.Size = new Size(153, 34);
            LblStatLossValue.Text = "0";
            LblStatLossValue.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // cardTable (Soft3DPanel chứa DataGridView)
            // ============================
            cardTable.BackColor = Color.Transparent;
            cardTable.CornerRadius = 16;
            cardTable.Location = new Point(30, 160);
            cardTable.Name = "cardTable";
            cardTable.Size = new Size(700, 320);
            cardTable.Controls.Add(DgvMatchHistory);

            // ============================
            // DgvMatchHistory
            // ============================
            DgvMatchHistory.AllowUserToAddRows = false;
            DgvMatchHistory.AllowUserToDeleteRows = false;
            DgvMatchHistory.AllowUserToResizeRows = false;
            DgvMatchHistory.ReadOnly = true;
            DgvMatchHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            DgvMatchHistory.MultiSelect = false;
            DgvMatchHistory.RowHeadersVisible = false;
            DgvMatchHistory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            DgvMatchHistory.BorderStyle = BorderStyle.None;
            DgvMatchHistory.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Colors
            DgvMatchHistory.BackgroundColor = Color.FromArgb(255, 253, 248);
            DgvMatchHistory.GridColor = Color.FromArgb(223, 202, 176);
            DgvMatchHistory.DefaultCellStyle.BackColor = Color.FromArgb(255, 253, 248);
            DgvMatchHistory.DefaultCellStyle.ForeColor = CaroTheme.TextDark;
            DgvMatchHistory.DefaultCellStyle.SelectionBackColor = CaroTheme.WoodHighlight;
            DgvMatchHistory.DefaultCellStyle.SelectionForeColor = Color.White;
            DgvMatchHistory.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            DgvMatchHistory.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);

            DgvMatchHistory.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 238, 226);
            DgvMatchHistory.AlternatingRowsDefaultCellStyle.ForeColor = CaroTheme.TextDark;

            DgvMatchHistory.ColumnHeadersDefaultCellStyle.BackColor = CaroTheme.WoodFrame;
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.ForeColor = CaroTheme.ButtonText;
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = CaroTheme.WoodFrame;
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.SelectionForeColor = CaroTheme.ButtonText;
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DgvMatchHistory.ColumnHeadersHeight = 36;
            DgvMatchHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            DgvMatchHistory.EnableHeadersVisualStyles = false;

            DgvMatchHistory.RowTemplate.Height = 36;

            // Columns
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColIndex",
                HeaderText = "STT",
                Width = 50,
                MinimumWidth = 45,
                FillWeight = 8,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColOpponent",
                HeaderText = "Đối thủ",
                MinimumWidth = 130,
                FillWeight = 28
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColPiece",
                HeaderText = "Quân cờ",
                MinimumWidth = 65,
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColResult",
                HeaderText = "Kết quả",
                MinimumWidth = 85,
                FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColStartTime",
                HeaderText = "Thời gian",
                MinimumWidth = 135,
                FillWeight = 22,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColDuration",
                HeaderText = "Số nước",
                MinimumWidth = 80,
                FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            DgvMatchHistory.Location = new Point(14, 14);
            DgvMatchHistory.Name = "DgvMatchHistory";
            DgvMatchHistory.Size = new Size(672, 292);

            // ============================
            // BtnRefresh
            // ============================
            BtnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnRefresh.IsSecondary = true;
            BtnRefresh.Location = new Point(30, 495);
            BtnRefresh.Name = "BtnRefresh";
            BtnRefresh.Size = new Size(160, 38);
            BtnRefresh.Text = "LÀM MỚI";
            BtnRefresh.Click += BtnRefresh_Click;

            // ============================
            // BtnClose
            // ============================
            BtnClose.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnClose.IsDestructive = true;
            BtnClose.Location = new Point(570, 495);
            BtnClose.Name = "BtnClose";
            BtnClose.Size = new Size(160, 38);
            BtnClose.Text = "ĐÓNG";
            BtnClose.Click += BtnClose_Click;

            // ============================
            // MatchHistoryForm
            // ============================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = CaroTheme.Background;
            ClientSize = new Size(760, 550);
            Controls.Add(LblTitle);
            Controls.Add(LblPlayerName);
            Controls.Add(cardTotal);
            Controls.Add(cardWin);
            Controls.Add(cardDraw);
            Controls.Add(cardLoss);
            Controls.Add(cardTable);
            Controls.Add(BtnRefresh);
            Controls.Add(BtnClose);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MatchHistoryForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lịch Sử Trận Đấu - C A R O";

            cardTotal.ResumeLayout(false);
            cardWin.ResumeLayout(false);
            cardDraw.ResumeLayout(false);
            cardLoss.ResumeLayout(false);
            cardTable.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)DgvMatchHistory).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Label LblTitle;
        private Label LblPlayerName;
        private Soft3DPanel cardTotal;
        private Label LblStatTotalTitle;
        private Label LblStatTotalValue;
        private Soft3DPanel cardWin;
        private Label LblStatWinTitle;
        private Label LblStatWinValue;
        private Soft3DPanel cardDraw;
        private Label LblStatDrawTitle;
        private Label LblStatDrawValue;
        private Soft3DPanel cardLoss;
        private Label LblStatLossTitle;
        private Label LblStatLossValue;
        private Soft3DPanel cardTable;
        private DataGridView DgvMatchHistory;
        private PillButton BtnRefresh;
        private PillButton BtnClose;
    }
}
