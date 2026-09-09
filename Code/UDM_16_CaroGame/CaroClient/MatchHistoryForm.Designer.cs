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
            PnlStats = new Panel();
            LblStatTotalTitle = new Label();
            LblStatTotalValue = new Label();
            LblStatWinTitle = new Label();
            LblStatWinValue = new Label();
            LblStatLossTitle = new Label();
            LblStatLossValue = new Label();

            DgvMatchHistory = new DataGridView();
            BtnRefresh = new Button();
            BtnClose = new Button();

            ((System.ComponentModel.ISupportInitialize)DgvMatchHistory).BeginInit();
            SuspendLayout();

            // ============================
            // LblTitle
            // ============================
            LblTitle.BackColor = Color.Transparent;
            LblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            LblTitle.ForeColor = ColorTranslator.FromHtml("#E8C37B");
            LblTitle.Location = new Point(0, 12);
            LblTitle.Name = "LblTitle";
            LblTitle.Size = new Size(700, 35);
            LblTitle.Text = "LỊCH SỬ TRẬN ĐẤU";
            LblTitle.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // LblPlayerName
            // ============================
            LblPlayerName.BackColor = Color.Transparent;
            LblPlayerName.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            LblPlayerName.ForeColor = ColorTranslator.FromHtml("#F5E6CA");
            LblPlayerName.Location = new Point(0, 47);
            LblPlayerName.Name = "LblPlayerName";
            LblPlayerName.Size = new Size(700, 20);
            LblPlayerName.Text = "Người chơi: --";
            LblPlayerName.TextAlign = ContentAlignment.MiddleCenter;

            // ============================
            // PnlStats (chứa 3 stat cards)
            // ============================
            PnlStats.BackColor = Color.Transparent;
            PnlStats.Location = new Point(20, 75);
            PnlStats.Name = "PnlStats";
            PnlStats.Size = new Size(660, 70);

            // --- Stat Card: Tổng trận ---
            var pnlTotal = new Panel();
            pnlTotal.BackColor = ColorTranslator.FromHtml("#2A190E");
            pnlTotal.Location = new Point(0, 0);
            pnlTotal.Size = new Size(210, 70);

            LblStatTotalTitle.BackColor = Color.Transparent;
            LblStatTotalTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            LblStatTotalTitle.ForeColor = ColorTranslator.FromHtml("#9CA3AF");
            LblStatTotalTitle.Location = new Point(0, 8);
            LblStatTotalTitle.Size = new Size(210, 18);
            LblStatTotalTitle.Text = "TỔNG SỐ TRẬN";
            LblStatTotalTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatTotalValue.BackColor = Color.Transparent;
            LblStatTotalValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatTotalValue.ForeColor = ColorTranslator.FromHtml("#FFFDF8");
            LblStatTotalValue.Location = new Point(0, 28);
            LblStatTotalValue.Size = new Size(210, 35);
            LblStatTotalValue.Text = "0";
            LblStatTotalValue.TextAlign = ContentAlignment.MiddleCenter;

            pnlTotal.Controls.Add(LblStatTotalTitle);
            pnlTotal.Controls.Add(LblStatTotalValue);

            // --- Stat Card: Số thắng ---
            var pnlWin = new Panel();
            pnlWin.BackColor = ColorTranslator.FromHtml("#2A190E");
            pnlWin.Location = new Point(225, 0);
            pnlWin.Size = new Size(210, 70);

            LblStatWinTitle.BackColor = Color.Transparent;
            LblStatWinTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            LblStatWinTitle.ForeColor = ColorTranslator.FromHtml("#9CA3AF");
            LblStatWinTitle.Location = new Point(0, 8);
            LblStatWinTitle.Size = new Size(210, 18);
            LblStatWinTitle.Text = "SỐ TRẬN THẮNG";
            LblStatWinTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatWinValue.BackColor = Color.Transparent;
            LblStatWinValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatWinValue.ForeColor = ColorTranslator.FromHtml("#10B981");
            LblStatWinValue.Location = new Point(0, 28);
            LblStatWinValue.Size = new Size(210, 35);
            LblStatWinValue.Text = "0";
            LblStatWinValue.TextAlign = ContentAlignment.MiddleCenter;

            pnlWin.Controls.Add(LblStatWinTitle);
            pnlWin.Controls.Add(LblStatWinValue);

            // --- Stat Card: Số thua ---
            var pnlLoss = new Panel();
            pnlLoss.BackColor = ColorTranslator.FromHtml("#2A190E");
            pnlLoss.Location = new Point(450, 0);
            pnlLoss.Size = new Size(210, 70);

            LblStatLossTitle.BackColor = Color.Transparent;
            LblStatLossTitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            LblStatLossTitle.ForeColor = ColorTranslator.FromHtml("#9CA3AF");
            LblStatLossTitle.Location = new Point(0, 8);
            LblStatLossTitle.Size = new Size(210, 18);
            LblStatLossTitle.Text = "SỐ TRẬN THUA";
            LblStatLossTitle.TextAlign = ContentAlignment.MiddleCenter;

            LblStatLossValue.BackColor = Color.Transparent;
            LblStatLossValue.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            LblStatLossValue.ForeColor = ColorTranslator.FromHtml("#EF4444");
            LblStatLossValue.Location = new Point(0, 28);
            LblStatLossValue.Size = new Size(210, 35);
            LblStatLossValue.Text = "0";
            LblStatLossValue.TextAlign = ContentAlignment.MiddleCenter;

            pnlLoss.Controls.Add(LblStatLossTitle);
            pnlLoss.Controls.Add(LblStatLossValue);

            PnlStats.Controls.Add(pnlTotal);
            PnlStats.Controls.Add(pnlWin);
            PnlStats.Controls.Add(pnlLoss);

            // ============================
            // DgvMatchHistory (DataGridView)
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
            DgvMatchHistory.BackgroundColor = ColorTranslator.FromHtml("#2A190E");
            DgvMatchHistory.GridColor = ColorTranslator.FromHtml("#5C3A21");
            DgvMatchHistory.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#3B2314");
            DgvMatchHistory.DefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FFFDF8");
            DgvMatchHistory.DefaultCellStyle.SelectionBackColor = ColorTranslator.FromHtml("#5C3A21");
            DgvMatchHistory.DefaultCellStyle.SelectionForeColor = ColorTranslator.FromHtml("#FFE8A3");
            DgvMatchHistory.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            DgvMatchHistory.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);

            DgvMatchHistory.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#2A190E");
            DgvMatchHistory.AlternatingRowsDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#FFFDF8");

            DgvMatchHistory.ColumnHeadersDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#5C3A21");
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.ForeColor = ColorTranslator.FromHtml("#E8C37B");
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            DgvMatchHistory.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DgvMatchHistory.ColumnHeadersHeight = 36;
            DgvMatchHistory.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            DgvMatchHistory.EnableHeadersVisualStyles = false;

            DgvMatchHistory.RowTemplate.Height = 38;

            // Columns
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColIndex",
                HeaderText = "STT",
                Width = 50,
                FillWeight = 8,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColOpponent",
                HeaderText = "Đối thủ",
                FillWeight = 28
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColPiece",
                HeaderText = "Quân cờ",
                FillWeight = 12,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColResult",
                HeaderText = "Kết quả",
                FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColStartTime",
                HeaderText = "Thời gian",
                FillWeight = 22,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            DgvMatchHistory.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ColDuration",
                HeaderText = "Thời lượng",
                FillWeight = 15,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            DgvMatchHistory.Location = new Point(20, 155);
            DgvMatchHistory.Name = "DgvMatchHistory";
            DgvMatchHistory.Size = new Size(660, 270);

            // ============================
            // BtnRefresh
            // ============================
            BtnRefresh.BackColor = ColorTranslator.FromHtml("#5C3A21");
            BtnRefresh.Cursor = Cursors.Hand;
            BtnRefresh.FlatAppearance.BorderSize = 0;
            BtnRefresh.FlatStyle = FlatStyle.Flat;
            BtnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnRefresh.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
            BtnRefresh.Location = new Point(20, 440);
            BtnRefresh.Name = "BtnRefresh";
            BtnRefresh.Size = new Size(140, 35);
            BtnRefresh.Text = "LÀM MỚI";
            BtnRefresh.UseVisualStyleBackColor = false;
            BtnRefresh.Click += BtnRefresh_Click;

            // ============================
            // BtnClose
            // ============================
            BtnClose.BackColor = ColorTranslator.FromHtml("#8B261D");
            BtnClose.Cursor = Cursors.Hand;
            BtnClose.FlatAppearance.BorderSize = 0;
            BtnClose.FlatStyle = FlatStyle.Flat;
            BtnClose.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            BtnClose.ForeColor = ColorTranslator.FromHtml("#FFE8A3");
            BtnClose.Location = new Point(540, 440);
            BtnClose.Name = "BtnClose";
            BtnClose.Size = new Size(140, 35);
            BtnClose.Text = "ĐÓNG";
            BtnClose.UseVisualStyleBackColor = false;
            BtnClose.Click += BtnClose_Click;

            // ============================
            // MatchHistoryForm
            // ============================
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = ColorTranslator.FromHtml("#3B2314");
            ClientSize = new Size(700, 495);
            Controls.Add(LblTitle);
            Controls.Add(LblPlayerName);
            Controls.Add(PnlStats);
            Controls.Add(DgvMatchHistory);
            Controls.Add(BtnRefresh);
            Controls.Add(BtnClose);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MatchHistoryForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Lịch Sử Trận Đấu - Game Caro";
            ((System.ComponentModel.ISupportInitialize)DgvMatchHistory).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label LblTitle;
        private Label LblPlayerName;
        private Panel PnlStats;
        private Label LblStatTotalTitle;
        private Label LblStatTotalValue;
        private Label LblStatWinTitle;
        private Label LblStatWinValue;
        private Label LblStatLossTitle;
        private Label LblStatLossValue;
        private DataGridView DgvMatchHistory;
        private Button BtnRefresh;
        private Button BtnClose;
    }
}
