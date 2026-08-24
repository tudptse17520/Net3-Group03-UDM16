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
            SuspendLayout();
            // 
            // GameBoardForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            // Kích thước Form phù hợp bàn cờ 15x15, mỗi ô 40px
            ClientSize = new Size(600, 620);
            DoubleBuffered = true;
            Name = "GameBoardForm";
            Text = "Caro Game";
            // Đăng ký event handlers
            Load += GameBoardForm_Load;
            FormClosing += GameBoardForm_FormClosing;
            ResumeLayout(false);
        }

        #endregion
    }
}
