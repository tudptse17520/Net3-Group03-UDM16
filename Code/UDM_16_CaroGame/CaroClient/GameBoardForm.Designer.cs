using System.Drawing;
using System.Windows.Forms;

namespace CaroClient
{
    partial class GameBoardForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pnlWoodFrame = new WoodBoardFramePanel();
            pnlBoardContainer = new Panel();
            lblAppTitle = new Label();
            lblTime = new Label();
            lblTimeCount = new Label();

            // Action Buttons
            btnSurrender = new PillButton();
            btnOfferDraw = new PillButton();
            btnNewGame = new PillButton();
            btnExitMatch = new PillButton();

            // Player 1 Card & Controls
            pnlPlayer1 = new Soft3DPanel();
            picAvatarPlayer1 = new PictureBox();
            lblPlayer1Name = new Label();
            lblPlayer1Id = new Label();
            lblPlayer1TimerPill = new Label();
            pnlPlayer1Stats = new Panel();
            Piece1 = new Label();
            picPlayer1Piece = new PictureBox();
            lblPlayer1MoveCountLabel = new Label();
            lblPlayer1MoveCount = new Label();
            pnlPlayer1Turn = new Label();
            prgPlayer1Timer = new ProgressBar();

            // Player 2 Card & Controls
            pnlPlayer2 = new Soft3DPanel();
            picAvatarPlayer2 = new PictureBox();
            lblPlayer2Name = new Label();
            lblPlayer2Id = new Label();
            lblPlayer2TimerPill = new Label();
            pnlPlayer2Stats = new Panel();
            Piece2 = new Label();
            picPlayer2Piece = new PictureBox();
            lblPlayer2MoveCountLabel = new Label();
            lblPlayer2MoveCount = new Label();
            pnlPlayer2Turn = new Label();
            prgPlayer2Timer = new ProgressBar();

            pnlWoodFrame.SuspendLayout();
            pnlPlayer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer1).BeginInit();
            pnlPlayer1Stats.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer1Piece).BeginInit();
            pnlPlayer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer2).BeginInit();
            pnlPlayer2Stats.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer2Piece).BeginInit();
            SuspendLayout();

            // 
            // lblAppTitle (Header Title: CARO ONLINE - GỖ & CỔ ĐIỂN)
            // 
            lblAppTitle.AutoSize = false;
            lblAppTitle.UseMnemonic = false;
            lblAppTitle.Font = new Font("Segoe UI", 21F, FontStyle.Bold, GraphicsUnit.Point);
            lblAppTitle.ForeColor = CaroTheme.TextDark;
            lblAppTitle.Location = new Point(0, 10);
            lblAppTitle.Name = "lblAppTitle";
            lblAppTitle.Size = new Size(1360, 44);
            lblAppTitle.TabIndex = 18;
            lblAppTitle.Text = "CARO ONLINE - GỖ & CỔ ĐIỂN";
            lblAppTitle.TextAlign = ContentAlignment.MiddleCenter;

            // 
            // lblTime / lblTimeCount (Preserved controls, hidden / mapped to timer pills)
            // 
            lblTime.Location = new Point(-200, -200);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(50, 20);
            lblTime.TabIndex = 17;
            lblTime.Text = "TIME:";
            lblTime.Visible = false;
            lblTime.Click += label9_Click;

            lblTimeCount.Location = new Point(-200, -200);
            lblTimeCount.Name = "lblTimeCount";
            lblTimeCount.Size = new Size(60, 20);
            lblTimeCount.TabIndex = 4;
            lblTimeCount.Text = "00:30";
            lblTimeCount.Visible = false;
            lblTimeCount.Click += label1_Click;

            // 
            // pnlWoodFrame (Wooden Frame Container)
            // 
            pnlWoodFrame.Controls.Add(pnlBoardContainer);
            pnlWoodFrame.Location = new Point(340, 60);
            pnlWoodFrame.Name = "pnlWoodFrame";
            pnlWoodFrame.Size = new Size(680, 680);
            pnlWoodFrame.TabIndex = 1;

            // 
            // pnlBoardContainer (15x15 cell container)
            // 
            pnlBoardContainer.BackColor = CaroTheme.BoardSurface;
            pnlBoardContainer.Location = new Point(16, 16);
            pnlBoardContainer.Name = "pnlBoardContainer";
            pnlBoardContainer.Size = new Size(648, 648);
            pnlBoardContainer.TabIndex = 2;

            // 
            // btnSurrender ("ĐẦU HÀNG" Pill Button)
            // 
            btnSurrender.BackColor = CaroTheme.Background;
            btnSurrender.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            btnSurrender.GlyphIcon = "⚑";
            btnSurrender.IsSurrender = true;
            btnSurrender.Location = new Point(440, 746);
            btnSurrender.Name = "btnSurrender";
            btnSurrender.Size = new Size(180, 44);
            btnSurrender.TabIndex = 9;
            btnSurrender.Text = "ĐẦU HÀNG";
            btnSurrender.Click += button1_Click;

            // 
            // btnOfferDraw ("HÒA" Pill Button - Visual only, NO Click wired)
            // 
            btnOfferDraw.BackColor = CaroTheme.Background;
            btnOfferDraw.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            btnOfferDraw.GlyphIcon = "⚖";
            btnOfferDraw.Location = new Point(636, 746);
            btnOfferDraw.Name = "btnOfferDraw";
            btnOfferDraw.Size = new Size(130, 44);
            btnOfferDraw.TabIndex = 10;
            btnOfferDraw.Text = "HÒA";

            // 
            // btnNewGame ("VÁN MỚI" Pill Button)
            // 
            btnNewGame.BackColor = CaroTheme.Background;
            btnNewGame.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            btnNewGame.GlyphIcon = "⟲";
            btnNewGame.Location = new Point(782, 746);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.Size = new Size(180, 44);
            btnNewGame.TabIndex = 11;
            btnNewGame.Text = "VÁN MỚI";
            btnNewGame.Click += button3_Click;

            // 
            // btnExitMatch ("VỀ SẢNH" Pill Button)
            // 
            btnExitMatch.BackColor = CaroTheme.Background;
            btnExitMatch.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnExitMatch.GlyphIcon = "⮌";
            btnExitMatch.IsDestructive = true;
            btnExitMatch.Location = new Point(948, 746);
            btnExitMatch.Name = "btnExitMatch";
            btnExitMatch.Size = new Size(150, 44);
            btnExitMatch.TabIndex = 12;
            btnExitMatch.Text = "VỀ SẢNH";
            btnExitMatch.Click += btnExitMatch_Click;

            // ══════════════════════════════════════════════════════════════════
            //  PLAYER 1 CARD (LEFT)
            // ══════════════════════════════════════════════════════════════════
            pnlPlayer1.Controls.Add(picAvatarPlayer1);
            pnlPlayer1.Controls.Add(lblPlayer1Name);
            pnlPlayer1.Controls.Add(lblPlayer1Id);
            pnlPlayer1.Controls.Add(lblPlayer1TimerPill);
            pnlPlayer1.Controls.Add(pnlPlayer1Stats);
            pnlPlayer1.Controls.Add(pnlPlayer1Turn);
            pnlPlayer1.Controls.Add(prgPlayer1Timer);
            pnlPlayer1.Location = new Point(40, 60);
            pnlPlayer1.Name = "pnlPlayer1";
            pnlPlayer1.Size = new Size(260, 360);
            pnlPlayer1.TabIndex = 0;

            // picAvatarPlayer1 (Monogram initial avatar)
            picAvatarPlayer1.Location = new Point(18, 18);
            picAvatarPlayer1.Name = "picAvatarPlayer1";
            picAvatarPlayer1.Size = new Size(64, 64);
            picAvatarPlayer1.TabIndex = 1;
            picAvatarPlayer1.TabStop = false;
            picAvatarPlayer1.Click += pictureBox1_Click;

            // lblPlayer1Name
            lblPlayer1Name.AutoEllipsis = true;
            lblPlayer1Name.AutoSize = false;
            lblPlayer1Name.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            lblPlayer1Name.ForeColor = CaroTheme.TextDark;
            lblPlayer1Name.Location = new Point(90, 20);
            lblPlayer1Name.Name = "lblPlayer1Name";
            lblPlayer1Name.Size = new Size(155, 28);
            lblPlayer1Name.TabIndex = 9;
            lblPlayer1Name.Text = "Player 1";
            lblPlayer1Name.TextAlign = ContentAlignment.MiddleLeft;

            // lblPlayer1Id
            lblPlayer1Id.AutoEllipsis = true;
            lblPlayer1Id.AutoSize = false;
            lblPlayer1Id.Font = new Font("Segoe UI", 9.5F);
            lblPlayer1Id.ForeColor = CaroTheme.TextMuted;
            lblPlayer1Id.Location = new Point(90, 48);
            lblPlayer1Id.Name = "lblPlayer1Id";
            lblPlayer1Id.Size = new Size(155, 20);
            lblPlayer1Id.TabIndex = 8;
            lblPlayer1Id.Text = "ID: ---";
            lblPlayer1Id.TextAlign = ContentAlignment.MiddleLeft;
            lblPlayer1Id.Click += label2_Click;

            // lblPlayer1TimerPill (Turn timer pill in Player 1 card)
            lblPlayer1TimerPill.AutoSize = false;
            lblPlayer1TimerPill.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblPlayer1TimerPill.ForeColor = CaroTheme.TimerPillText;
            lblPlayer1TimerPill.Location = new Point(16, 92);
            lblPlayer1TimerPill.Name = "lblPlayer1TimerPill";
            lblPlayer1TimerPill.Size = new Size(228, 34);
            lblPlayer1TimerPill.TabIndex = 20;
            lblPlayer1TimerPill.Text = "⏱ 00:30";
            lblPlayer1TimerPill.TextAlign = ContentAlignment.MiddleCenter;

            // pnlPlayer1Stats (Recessed stats box)
            pnlPlayer1Stats.BackColor = Color.Transparent;
            pnlPlayer1Stats.Controls.Add(Piece1);
            pnlPlayer1Stats.Controls.Add(picPlayer1Piece);
            pnlPlayer1Stats.Controls.Add(lblPlayer1MoveCountLabel);
            pnlPlayer1Stats.Controls.Add(lblPlayer1MoveCount);
            pnlPlayer1Stats.Location = new Point(16, 138);
            pnlPlayer1Stats.Name = "pnlPlayer1Stats";
            pnlPlayer1Stats.Size = new Size(228, 120);
            pnlPlayer1Stats.TabIndex = 21;

            // Piece1
            Piece1.AutoSize = false;
            Piece1.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            Piece1.ForeColor = CaroTheme.TextDark;
            Piece1.Location = new Point(12, 16);
            Piece1.Name = "Piece1";
            Piece1.Size = new Size(160, 24);
            Piece1.TabIndex = 11;
            Piece1.Text = "Quân cờ: X (Đi trước)";
            Piece1.Click += label2_Click_1;

            // picPlayer1Piece
            picPlayer1Piece.Location = new Point(178, 12);
            picPlayer1Piece.Name = "picPlayer1Piece";
            picPlayer1Piece.Size = new Size(32, 32);
            picPlayer1Piece.TabIndex = 12;
            picPlayer1Piece.TabStop = false;
            picPlayer1Piece.Click += pictureBox1_Click_1;

            // lblPlayer1MoveCountLabel
            lblPlayer1MoveCountLabel.AutoSize = true;
            lblPlayer1MoveCountLabel.Font = new Font("Segoe UI", 10.5F);
            lblPlayer1MoveCountLabel.ForeColor = CaroTheme.TextMuted;
            lblPlayer1MoveCountLabel.Location = new Point(12, 64);
            lblPlayer1MoveCountLabel.Name = "lblPlayer1MoveCountLabel";
            lblPlayer1MoveCountLabel.Size = new Size(100, 20);
            lblPlayer1MoveCountLabel.TabIndex = 13;
            lblPlayer1MoveCountLabel.Text = "Số nước đã đi:";
            lblPlayer1MoveCountLabel.Click += label3_Click;

            // lblPlayer1MoveCount
            lblPlayer1MoveCount.AutoSize = true;
            lblPlayer1MoveCount.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblPlayer1MoveCount.ForeColor = CaroTheme.TextDark;
            lblPlayer1MoveCount.Location = new Point(178, 60);
            lblPlayer1MoveCount.Name = "lblPlayer1MoveCount";
            lblPlayer1MoveCount.Size = new Size(22, 25);
            lblPlayer1MoveCount.TabIndex = 14;
            lblPlayer1MoveCount.Text = "0";

            // pnlPlayer1Turn (Turn status badge)
            pnlPlayer1Turn.AutoSize = false;
            pnlPlayer1Turn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            pnlPlayer1Turn.ForeColor = CaroTheme.BadgeActiveText;
            pnlPlayer1Turn.Location = new Point(16, 276);
            pnlPlayer1Turn.Name = "pnlPlayer1Turn";
            pnlPlayer1Turn.Size = new Size(228, 42);
            pnlPlayer1Turn.TabIndex = 15;
            pnlPlayer1Turn.Text = "● LƯỢT CỦA BẠN";
            pnlPlayer1Turn.TextAlign = ContentAlignment.MiddleCenter;
            pnlPlayer1Turn.Click += label3_Click_1;

            // prgPlayer1Timer (Hidden compatibility control)
            prgPlayer1Timer.Location = new Point(16, 330);
            prgPlayer1Timer.Name = "prgPlayer1Timer";
            prgPlayer1Timer.Size = new Size(228, 10);
            prgPlayer1Timer.TabIndex = 0;
            prgPlayer1Timer.Visible = false;

            // ══════════════════════════════════════════════════════════════════
            //  PLAYER 2 CARD (RIGHT - OPPONENT)
            // ══════════════════════════════════════════════════════════════════
            pnlPlayer2.Controls.Add(picAvatarPlayer2);
            pnlPlayer2.Controls.Add(lblPlayer2Name);
            pnlPlayer2.Controls.Add(lblPlayer2Id);
            pnlPlayer2.Controls.Add(lblPlayer2TimerPill);
            pnlPlayer2.Controls.Add(pnlPlayer2Stats);
            pnlPlayer2.Controls.Add(pnlPlayer2Turn);
            pnlPlayer2.Controls.Add(prgPlayer2Timer);
            pnlPlayer2.Location = new Point(1060, 60);
            pnlPlayer2.Name = "pnlPlayer2";
            pnlPlayer2.Size = new Size(260, 360);
            pnlPlayer2.TabIndex = 16;

            // picAvatarPlayer2 (Monogram initial avatar)
            picAvatarPlayer2.Location = new Point(18, 18);
            picAvatarPlayer2.Name = "picAvatarPlayer2";
            picAvatarPlayer2.Size = new Size(64, 64);
            picAvatarPlayer2.TabIndex = 1;
            picAvatarPlayer2.TabStop = false;

            // lblPlayer2Name
            lblPlayer2Name.AutoEllipsis = true;
            lblPlayer2Name.AutoSize = false;
            lblPlayer2Name.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            lblPlayer2Name.ForeColor = CaroTheme.TextDark;
            lblPlayer2Name.Location = new Point(90, 20);
            lblPlayer2Name.Name = "lblPlayer2Name";
            lblPlayer2Name.Size = new Size(155, 28);
            lblPlayer2Name.TabIndex = 9;
            lblPlayer2Name.Text = "Player 2";
            lblPlayer2Name.TextAlign = ContentAlignment.MiddleLeft;

            // lblPlayer2Id
            lblPlayer2Id.AutoEllipsis = true;
            lblPlayer2Id.AutoSize = false;
            lblPlayer2Id.Font = new Font("Segoe UI", 9.5F);
            lblPlayer2Id.ForeColor = CaroTheme.TextMuted;
            lblPlayer2Id.Location = new Point(90, 48);
            lblPlayer2Id.Name = "lblPlayer2Id";
            lblPlayer2Id.Size = new Size(155, 20);
            lblPlayer2Id.TabIndex = 8;
            lblPlayer2Id.Text = "ID: ---";
            lblPlayer2Id.TextAlign = ContentAlignment.MiddleLeft;

            // lblPlayer2TimerPill (Turn timer pill in Player 2 card)
            lblPlayer2TimerPill.AutoSize = false;
            lblPlayer2TimerPill.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblPlayer2TimerPill.ForeColor = CaroTheme.TimerPillText;
            lblPlayer2TimerPill.Location = new Point(16, 92);
            lblPlayer2TimerPill.Name = "lblPlayer2TimerPill";
            lblPlayer2TimerPill.Size = new Size(228, 34);
            lblPlayer2TimerPill.TabIndex = 22;
            lblPlayer2TimerPill.Text = "⏱ 00:30";
            lblPlayer2TimerPill.TextAlign = ContentAlignment.MiddleCenter;

            // pnlPlayer2Stats (Recessed stats box)
            pnlPlayer2Stats.BackColor = Color.Transparent;
            pnlPlayer2Stats.Controls.Add(Piece2);
            pnlPlayer2Stats.Controls.Add(picPlayer2Piece);
            pnlPlayer2Stats.Controls.Add(lblPlayer2MoveCountLabel);
            pnlPlayer2Stats.Controls.Add(lblPlayer2MoveCount);
            pnlPlayer2Stats.Location = new Point(16, 138);
            pnlPlayer2Stats.Name = "pnlPlayer2Stats";
            pnlPlayer2Stats.Size = new Size(228, 120);
            pnlPlayer2Stats.TabIndex = 23;

            // Piece2
            Piece2.AutoSize = false;
            Piece2.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            Piece2.ForeColor = CaroTheme.TextDark;
            Piece2.Location = new Point(12, 16);
            Piece2.Name = "Piece2";
            Piece2.Size = new Size(160, 24);
            Piece2.TabIndex = 11;
            Piece2.Text = "Quân cờ: O";

            // picPlayer2Piece
            picPlayer2Piece.Location = new Point(178, 12);
            picPlayer2Piece.Name = "picPlayer2Piece";
            picPlayer2Piece.Size = new Size(32, 32);
            picPlayer2Piece.TabIndex = 12;
            picPlayer2Piece.TabStop = false;

            // lblPlayer2MoveCountLabel
            lblPlayer2MoveCountLabel.AutoSize = true;
            lblPlayer2MoveCountLabel.Font = new Font("Segoe UI", 10.5F);
            lblPlayer2MoveCountLabel.ForeColor = CaroTheme.TextMuted;
            lblPlayer2MoveCountLabel.Location = new Point(12, 64);
            lblPlayer2MoveCountLabel.Name = "lblPlayer2MoveCountLabel";
            lblPlayer2MoveCountLabel.Size = new Size(100, 20);
            lblPlayer2MoveCountLabel.TabIndex = 13;
            lblPlayer2MoveCountLabel.Text = "Số nước đã đi:";

            // lblPlayer2MoveCount
            lblPlayer2MoveCount.AutoSize = true;
            lblPlayer2MoveCount.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblPlayer2MoveCount.ForeColor = CaroTheme.TextDark;
            lblPlayer2MoveCount.Location = new Point(178, 60);
            lblPlayer2MoveCount.Name = "lblPlayer2MoveCount";
            lblPlayer2MoveCount.Size = new Size(22, 25);
            lblPlayer2MoveCount.TabIndex = 14;
            lblPlayer2MoveCount.Text = "0";

            // pnlPlayer2Turn (Turn status badge)
            pnlPlayer2Turn.AutoSize = false;
            pnlPlayer2Turn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            pnlPlayer2Turn.ForeColor = CaroTheme.BadgeInactiveText;
            pnlPlayer2Turn.Location = new Point(16, 276);
            pnlPlayer2Turn.Name = "pnlPlayer2Turn";
            pnlPlayer2Turn.Size = new Size(228, 42);
            pnlPlayer2Turn.TabIndex = 15;
            pnlPlayer2Turn.Text = "⏳ ĐANG CHỜ ĐỐI THỦ";
            pnlPlayer2Turn.TextAlign = ContentAlignment.MiddleCenter;

            // prgPlayer2Timer (Hidden compatibility control)
            prgPlayer2Timer.Location = new Point(16, 330);
            prgPlayer2Timer.Name = "prgPlayer2Timer";
            prgPlayer2Timer.Size = new Size(228, 10);
            prgPlayer2Timer.TabIndex = 0;
            prgPlayer2Timer.Visible = false;

            // ══════════════════════════════════════════════════════════════════
            //  FORM MAIN CONFIGURATION
            // ══════════════════════════════════════════════════════════════════
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = CaroTheme.Background;
            ClientSize = new Size(1360, 810);
            MinimumSize = new Size(1220, 810);
            Controls.Add(lblAppTitle);
            Controls.Add(pnlWoodFrame);
            Controls.Add(pnlPlayer1);
            Controls.Add(pnlPlayer2);
            Controls.Add(btnSurrender);
            Controls.Add(btnOfferDraw);
            Controls.Add(btnNewGame);
            Controls.Add(btnExitMatch);
            Controls.Add(lblTime);
            Controls.Add(lblTimeCount);
            Margin = new Padding(3, 2, 3, 2);
            Name = "GameBoardForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "CARO ONLINE - GỖ & CỔ ĐIỂN";

            pnlWoodFrame.ResumeLayout(false);
            pnlPlayer1.ResumeLayout(false);
            pnlPlayer1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer1).EndInit();
            pnlPlayer1Stats.ResumeLayout(false);
            pnlPlayer1Stats.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer1Piece).EndInit();
            pnlPlayer2.ResumeLayout(false);
            pnlPlayer2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer2).EndInit();
            pnlPlayer2Stats.ResumeLayout(false);
            pnlPlayer2Stats.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer2Piece).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        // Custom Panels
        private WoodBoardFramePanel pnlWoodFrame;
        private Panel pnlBoardContainer;

        // Header & Preserved Timer labels
        private Label lblAppTitle;
        private Label lblTime;
        private Label lblTimeCount;

        // Action Buttons
        private PillButton btnSurrender;
        private PillButton btnOfferDraw;
        private PillButton btnNewGame;
        private PillButton btnExitMatch;

        // Player 1
        private Soft3DPanel pnlPlayer1;
        private PictureBox picAvatarPlayer1;
        private Label lblPlayer1Name;
        private Label lblPlayer1Id;
        private Label lblPlayer1TimerPill;
        private Panel pnlPlayer1Stats;
        private Label Piece1;
        private PictureBox picPlayer1Piece;
        private Label lblPlayer1MoveCountLabel;
        private Label lblPlayer1MoveCount;
        private Label pnlPlayer1Turn;
        private ProgressBar prgPlayer1Timer;

        // Player 2
        private Soft3DPanel pnlPlayer2;
        private PictureBox picAvatarPlayer2;
        private Label lblPlayer2Name;
        private Label lblPlayer2Id;
        private Label lblPlayer2TimerPill;
        private Panel pnlPlayer2Stats;
        private Label Piece2;
        private PictureBox picPlayer2Piece;
        private Label lblPlayer2MoveCountLabel;
        private Label lblPlayer2MoveCount;
        private Label pnlPlayer2Turn;
        private ProgressBar prgPlayer2Timer;
    }
}