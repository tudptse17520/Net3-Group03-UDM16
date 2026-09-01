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
            pnlBoardContainer = new Panel();
            lblTimeCount = new Label();
            btnSurrender = new Button();
            btnOfferDraw = new Button();
            btnNewGame = new Button();
            prgPlayer1Timer = new ProgressBar();
            picAvatarPlayer1 = new PictureBox();
            lblPlayer1Id = new Label();
            lblPlayer1Name = new Label();
            pnlPlayer1 = new Panel();
            pnlPlayer1Turn = new Label();
            lblPlayer1MoveCount = new Label();
            lblPlayer1MoveCountLabel = new Label();
            picPlayer1Piece = new PictureBox();
            Piece1 = new Label();
            pnlPlayer2 = new Panel();
            pnlPlayer2Turn = new Label();
            lblPlayer2MoveCount = new Label();
            lblPlayer2MoveCountLabel = new Label();
            picPlayer2Piece = new PictureBox();
            Piece2 = new Label();
            lblPlayer2Name = new Label();
            lblPlayer2Id = new Label();
            picAvatarPlayer2 = new PictureBox();
            prgPlayer2Timer = new ProgressBar();
            lblTime = new Label();
            lblAppTitle = new Label();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer1).BeginInit();
            pnlPlayer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer1Piece).BeginInit();
            pnlPlayer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer2Piece).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer2).BeginInit();
            SuspendLayout();
            // 
            // pnlBoardContainer
            // 
            pnlBoardContainer.Location = new Point(261, 61);
            pnlBoardContainer.Name = "pnlBoardContainer";
            pnlBoardContainer.Size = new Size(878, 666);
            pnlBoardContainer.TabIndex = 2;
            // 
            // lblTimeCount
            // 
            lblTimeCount.AutoSize = true;
            lblTimeCount.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTimeCount.Location = new Point(1194, 9);
            lblTimeCount.Name = "lblTimeCount";
            lblTimeCount.Size = new Size(62, 30);
            lblTimeCount.TabIndex = 4;
            lblTimeCount.Text = "00:00";
            lblTimeCount.Click += label1_Click;
            // 
            // btnSurrender
            // 
            btnSurrender.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnSurrender.Location = new Point(1145, 500);
            btnSurrender.Name = "btnSurrender";
            btnSurrender.Size = new Size(230, 38);
            btnSurrender.TabIndex = 9;
            btnSurrender.Text = "ĐẦU HÀNG";
            btnSurrender.UseVisualStyleBackColor = true;
            btnSurrender.Click += button1_Click;
            // 
            // btnOfferDraw
            // 
            btnOfferDraw.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnOfferDraw.Location = new Point(1145, 560);
            btnOfferDraw.Name = "btnOfferDraw";
            btnOfferDraw.Size = new Size(230, 38);
            btnOfferDraw.TabIndex = 10;
            btnOfferDraw.Text = "HÒA";
            btnOfferDraw.UseVisualStyleBackColor = true;
            // 
            // btnNewGame
            // 
            btnNewGame.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnNewGame.Location = new Point(1145, 620);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.Size = new Size(230, 38);
            btnNewGame.TabIndex = 11;
            btnNewGame.Text = "VÁN MỚI";
            btnNewGame.UseVisualStyleBackColor = true;
            btnNewGame.Click += button3_Click;
            // 
            // prgPlayer1Timer
            // 
            prgPlayer1Timer.Location = new Point(15, 274);
            prgPlayer1Timer.Name = "prgPlayer1Timer";
            prgPlayer1Timer.Size = new Size(201, 23);
            prgPlayer1Timer.TabIndex = 0;
            // 
            // picAvatarPlayer1
            // 
            picAvatarPlayer1.Location = new Point(15, 17);
            picAvatarPlayer1.Name = "picAvatarPlayer1";
            picAvatarPlayer1.Size = new Size(83, 81);
            picAvatarPlayer1.TabIndex = 1;
            picAvatarPlayer1.TabStop = false;
            picAvatarPlayer1.Click += pictureBox1_Click;
            // 
            // lblPlayer1Id
            // 
            lblPlayer1Id.AutoSize = true;
            lblPlayer1Id.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPlayer1Id.Location = new Point(104, 18);
            lblPlayer1Id.Name = "lblPlayer1Id";
            lblPlayer1Id.Size = new Size(50, 21);
            lblPlayer1Id.TabIndex = 8;
            lblPlayer1Id.Text = "ID: ---";
            lblPlayer1Id.Click += label2_Click;
            // 
            // lblPlayer1Name
            // 
            lblPlayer1Name.AutoSize = true;
            lblPlayer1Name.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPlayer1Name.Location = new Point(104, 46);
            lblPlayer1Name.Name = "lblPlayer1Name";
            lblPlayer1Name.Size = new Size(54, 17);
            lblPlayer1Name.TabIndex = 9;
            lblPlayer1Name.Text = "Player 1";
            // 
            // pnlPlayer1
            // 
            pnlPlayer1.Controls.Add(pnlPlayer1Turn);
            pnlPlayer1.Controls.Add(lblPlayer1MoveCount);
            pnlPlayer1.Controls.Add(lblPlayer1MoveCountLabel);
            pnlPlayer1.Controls.Add(picPlayer1Piece);
            pnlPlayer1.Controls.Add(Piece1);
            pnlPlayer1.Controls.Add(lblPlayer1Name);
            pnlPlayer1.Controls.Add(lblPlayer1Id);
            pnlPlayer1.Controls.Add(picAvatarPlayer1);
            pnlPlayer1.Controls.Add(prgPlayer1Timer);
            pnlPlayer1.Location = new Point(25, 61);
            pnlPlayer1.Name = "pnlPlayer1";
            pnlPlayer1.Size = new Size(230, 310);
            pnlPlayer1.TabIndex = 0;
            // 
            // pnlPlayer1Turn
            // 
            pnlPlayer1Turn.AutoSize = true;
            pnlPlayer1Turn.Font = new Font("Segoe UI", 12F);
            pnlPlayer1Turn.Location = new Point(15, 246);
            pnlPlayer1Turn.Name = "pnlPlayer1Turn";
            pnlPlayer1Turn.Size = new Size(62, 21);
            pnlPlayer1Turn.TabIndex = 15;
            pnlPlayer1Turn.Text = "Lượt đi:";
            pnlPlayer1Turn.Click += label3_Click_1;
            // 
            // lblPlayer1MoveCount
            // 
            lblPlayer1MoveCount.AutoSize = true;
            lblPlayer1MoveCount.Font = new Font("Segoe UI", 12F);
            lblPlayer1MoveCount.Location = new Point(129, 145);
            lblPlayer1MoveCount.Name = "lblPlayer1MoveCount";
            lblPlayer1MoveCount.Size = new Size(19, 21);
            lblPlayer1MoveCount.TabIndex = 14;
            lblPlayer1MoveCount.Text = "0";
            // 
            // lblPlayer1MoveCountLabel
            // 
            lblPlayer1MoveCountLabel.AutoSize = true;
            lblPlayer1MoveCountLabel.Font = new Font("Segoe UI", 12F);
            lblPlayer1MoveCountLabel.Location = new Point(15, 145);
            lblPlayer1MoveCountLabel.Name = "lblPlayer1MoveCountLabel";
            lblPlayer1MoveCountLabel.Size = new Size(108, 21);
            lblPlayer1MoveCountLabel.TabIndex = 13;
            lblPlayer1MoveCountLabel.Text = "Số nước đã đi:";
            lblPlayer1MoveCountLabel.Click += label3_Click;
            // 
            // picPlayer1Piece
            // 
            picPlayer1Piece.Location = new Point(104, 107);
            picPlayer1Piece.Name = "picPlayer1Piece";
            picPlayer1Piece.Size = new Size(35, 35);
            picPlayer1Piece.TabIndex = 12;
            picPlayer1Piece.TabStop = false;
            picPlayer1Piece.Click += pictureBox1_Click_1;
            // 
            // Piece1
            // 
            Piece1.AutoSize = true;
            Piece1.Font = new Font("Segoe UI", 12F);
            Piece1.Location = new Point(15, 113);
            Piece1.Name = "Piece1";
            Piece1.Size = new Size(72, 21);
            Piece1.TabIndex = 11;
            Piece1.Text = "Quân cờ:";
            Piece1.Click += label2_Click_1;
            // 
            // pnlPlayer2
            // 
            pnlPlayer2.Controls.Add(pnlPlayer2Turn);
            pnlPlayer2.Controls.Add(lblPlayer2MoveCount);
            pnlPlayer2.Controls.Add(lblPlayer2MoveCountLabel);
            pnlPlayer2.Controls.Add(picPlayer2Piece);
            pnlPlayer2.Controls.Add(Piece2);
            pnlPlayer2.Controls.Add(lblPlayer2Name);
            pnlPlayer2.Controls.Add(lblPlayer2Id);
            pnlPlayer2.Controls.Add(picAvatarPlayer2);
            pnlPlayer2.Controls.Add(prgPlayer2Timer);
            pnlPlayer2.Location = new Point(1145, 61);
            pnlPlayer2.Name = "pnlPlayer2";
            pnlPlayer2.Size = new Size(230, 310);
            pnlPlayer2.TabIndex = 16;
            // 
            // pnlPlayer2Turn
            // 
            pnlPlayer2Turn.AutoSize = true;
            pnlPlayer2Turn.Font = new Font("Segoe UI", 12F);
            pnlPlayer2Turn.Location = new Point(15, 246);
            pnlPlayer2Turn.Name = "pnlPlayer2Turn";
            pnlPlayer2Turn.Size = new Size(62, 21);
            pnlPlayer2Turn.TabIndex = 15;
            pnlPlayer2Turn.Text = "Lượt đi:";
            // 
            // lblPlayer2MoveCount
            // 
            lblPlayer2MoveCount.AutoSize = true;
            lblPlayer2MoveCount.Font = new Font("Segoe UI", 12F);
            lblPlayer2MoveCount.Location = new Point(129, 145);
            lblPlayer2MoveCount.Name = "lblPlayer2MoveCount";
            lblPlayer2MoveCount.Size = new Size(19, 21);
            lblPlayer2MoveCount.TabIndex = 14;
            lblPlayer2MoveCount.Text = "0";
            // 
            // lblPlayer2MoveCountLabel
            // 
            lblPlayer2MoveCountLabel.AutoSize = true;
            lblPlayer2MoveCountLabel.Font = new Font("Segoe UI", 12F);
            lblPlayer2MoveCountLabel.Location = new Point(15, 145);
            lblPlayer2MoveCountLabel.Name = "lblPlayer2MoveCountLabel";
            lblPlayer2MoveCountLabel.Size = new Size(108, 21);
            lblPlayer2MoveCountLabel.TabIndex = 13;
            lblPlayer2MoveCountLabel.Text = "Số nước đã đi:";
            // 
            // picPlayer2Piece
            // 
            picPlayer2Piece.Location = new Point(104, 107);
            picPlayer2Piece.Name = "picPlayer2Piece";
            picPlayer2Piece.Size = new Size(35, 35);
            picPlayer2Piece.TabIndex = 12;
            picPlayer2Piece.TabStop = false;
            // 
            // Piece2
            // 
            Piece2.AutoSize = true;
            Piece2.Font = new Font("Segoe UI", 12F);
            Piece2.Location = new Point(15, 113);
            Piece2.Name = "Piece2";
            Piece2.Size = new Size(72, 21);
            Piece2.TabIndex = 11;
            Piece2.Text = "Quân cờ:";
            // 
            // lblPlayer2Name
            // 
            lblPlayer2Name.AutoSize = true;
            lblPlayer2Name.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPlayer2Name.Location = new Point(104, 46);
            lblPlayer2Name.Name = "lblPlayer2Name";
            lblPlayer2Name.Size = new Size(54, 17);
            lblPlayer2Name.TabIndex = 9;
            lblPlayer2Name.Text = "Player 2";
            // 
            // lblPlayer2Id
            // 
            lblPlayer2Id.AutoSize = true;
            lblPlayer2Id.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblPlayer2Id.Location = new Point(104, 18);
            lblPlayer2Id.Name = "lblPlayer2Id";
            lblPlayer2Id.Size = new Size(50, 21);
            lblPlayer2Id.TabIndex = 8;
            lblPlayer2Id.Text = "ID: ---";
            // 
            // picAvatarPlayer2
            // 
            picAvatarPlayer2.Location = new Point(15, 17);
            picAvatarPlayer2.Name = "picAvatarPlayer2";
            picAvatarPlayer2.Size = new Size(83, 81);
            picAvatarPlayer2.TabIndex = 1;
            picAvatarPlayer2.TabStop = false;
            // 
            // prgPlayer2Timer
            // 
            prgPlayer2Timer.Location = new Point(15, 274);
            prgPlayer2Timer.Name = "prgPlayer2Timer";
            prgPlayer2Timer.Size = new Size(201, 23);
            prgPlayer2Timer.TabIndex = 0;
            // 
            // lblTime
            // 
            lblTime.AutoSize = true;
            lblTime.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTime.Location = new Point(1135, 9);
            lblTime.Name = "lblTime";
            lblTime.Size = new Size(65, 30);
            lblTime.TabIndex = 17;
            lblTime.Text = "TIME:";
            lblTime.Click += label9_Click;
            // 
            // lblAppTitle
            // 
            lblAppTitle.AutoSize = true;
            lblAppTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblAppTitle.Location = new Point(12, 9);
            lblAppTitle.Name = "lblAppTitle";
            lblAppTitle.Size = new Size(176, 32);
            lblAppTitle.TabIndex = 18;
            lblAppTitle.Text = "CARO ONLINE";
            // 
            // GameBoardForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1395, 748);
            Controls.Add(lblAppTitle);
            Controls.Add(lblTime);
            Controls.Add(pnlPlayer2);
            Controls.Add(btnNewGame);
            Controls.Add(btnOfferDraw);
            Controls.Add(btnSurrender);
            Controls.Add(lblTimeCount);
            Controls.Add(pnlBoardContainer);
            Controls.Add(pnlPlayer1);
            Margin = new Padding(3, 2, 3, 2);
            Name = "GameBoardForm";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer1).EndInit();
            pnlPlayer1.ResumeLayout(false);
            pnlPlayer1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer1Piece).EndInit();
            pnlPlayer2.ResumeLayout(false);
            pnlPlayer2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)picPlayer2Piece).EndInit();
            ((System.ComponentModel.ISupportInitialize)picAvatarPlayer2).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Panel pnlBoardContainer;
        private Label lblTimeCount;
        private Button btnSurrender;
        private Button btnOfferDraw;
        private Button btnNewGame;
        private ProgressBar prgPlayer1Timer;
        private PictureBox picAvatarPlayer1;
        private Label lblPlayer1Id;
        private Label lblPlayer1Name;
        private Panel pnlPlayer1;
        private Label Piece1;
        private PictureBox picPlayer1Piece;
        private Label lblPlayer1MoveCountLabel;
        private Label lblPlayer1MoveCount;
        private Label pnlPlayer1Turn;
        private Panel pnlPlayer2;
        private Label pnlPlayer2Turn;
        private Label lblPlayer2MoveCount;
        private Label lblPlayer2MoveCountLabel;
        private PictureBox picPlayer2Piece;
        private Label Piece2;
        private Label lblPlayer2Name;
        private Label lblPlayer2Id;
        private PictureBox picAvatarPlayer2;
        private ProgressBar prgPlayer2Timer;
        private Label lblTime;
        private Label lblAppTitle;
    }
}