namespace CaroClient
{
    partial class PersonalizationForm
    {
        private System.ComponentModel.IContainer components = null;
        
        private System.Windows.Forms.TabControl TabControlMain;
        private System.Windows.Forms.TabPage TabProfile;
        private System.Windows.Forms.TabPage TabBoard;
        private System.Windows.Forms.TabPage TabPiece;
        
        private System.Windows.Forms.PictureBox PicAvatar;
        private CaroClient.PillButton BtnUpload;
        private CaroClient.PillButton BtnRemoveAvatar;

        private System.Windows.Forms.Label LblTheme;
        private System.Windows.Forms.ComboBox CmbTheme;
        private System.Windows.Forms.Label LblSecColor;
        private System.Windows.Forms.ComboBox CmbSecColor;

        private System.Windows.Forms.Label LblPieceShape;
        private System.Windows.Forms.ComboBox CmbPieceShape;
        private System.Windows.Forms.Label LblOpponentShape;
        private System.Windows.Forms.ComboBox CmbOpponentShape;
        private System.Windows.Forms.Label LblEffect;
        private System.Windows.Forms.ComboBox CmbEffect;

        private System.Windows.Forms.Panel PnlPreview;
        private System.Windows.Forms.Label LblPreview;

        private CaroClient.PillButton BtnSave;
        private CaroClient.PillButton BtnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.TabControlMain = new System.Windows.Forms.TabControl();
            this.TabProfile = new System.Windows.Forms.TabPage();
            this.TabBoard = new System.Windows.Forms.TabPage();
            this.TabPiece = new System.Windows.Forms.TabPage();
            this.PicAvatar = new System.Windows.Forms.PictureBox();
            this.BtnUpload = new CaroClient.PillButton();
            this.BtnRemoveAvatar = new CaroClient.PillButton();
            this.LblTheme = new System.Windows.Forms.Label();
            this.CmbTheme = new System.Windows.Forms.ComboBox();
            this.LblSecColor = new System.Windows.Forms.Label();
            this.CmbSecColor = new System.Windows.Forms.ComboBox();
            this.LblPieceShape = new System.Windows.Forms.Label();
            this.CmbPieceShape = new System.Windows.Forms.ComboBox();
            this.LblOpponentShape = new System.Windows.Forms.Label();
            this.CmbOpponentShape = new System.Windows.Forms.ComboBox();
            this.LblEffect = new System.Windows.Forms.Label();
            this.CmbEffect = new System.Windows.Forms.ComboBox();
            this.PnlPreview = new System.Windows.Forms.Panel();
            this.LblPreview = new System.Windows.Forms.Label();
            this.BtnSave = new CaroClient.PillButton();
            this.BtnCancel = new CaroClient.PillButton();
            
            this.TabControlMain.SuspendLayout();
            this.TabProfile.SuspendLayout();
            this.TabBoard.SuspendLayout();
            this.TabPiece.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PicAvatar)).BeginInit();
            this.SuspendLayout();
            
            // TabControlMain
            this.TabControlMain.Controls.Add(this.TabProfile);
            this.TabControlMain.Controls.Add(this.TabBoard);
            this.TabControlMain.Controls.Add(this.TabPiece);
            this.TabControlMain.Location = new System.Drawing.Point(12, 12);
            this.TabControlMain.Name = "TabControlMain";
            this.TabControlMain.SelectedIndex = 0;
            this.TabControlMain.Size = new System.Drawing.Size(350, 250);
            this.TabControlMain.Font = new System.Drawing.Font("Segoe UI", 10F);
            
            // TabProfile
            this.TabProfile.Controls.Add(this.BtnRemoveAvatar);
            this.TabProfile.Controls.Add(this.BtnUpload);
            this.TabProfile.Controls.Add(this.PicAvatar);
            this.TabProfile.Location = new System.Drawing.Point(4, 26);
            this.TabProfile.Name = "TabProfile";
            this.TabProfile.Padding = new System.Windows.Forms.Padding(3);
            this.TabProfile.Size = new System.Drawing.Size(342, 220);
            this.TabProfile.Text = "Hồ sơ";
            this.TabProfile.UseVisualStyleBackColor = true;
            
            // PicAvatar
            this.PicAvatar.Location = new System.Drawing.Point(110, 20);
            this.PicAvatar.Name = "PicAvatar";
            this.PicAvatar.Size = new System.Drawing.Size(100, 100);
            this.PicAvatar.Paint += new System.Windows.Forms.PaintEventHandler(this.PicAvatar_Paint);
            
            // BtnUpload
            this.BtnUpload.Location = new System.Drawing.Point(48, 140);
            this.BtnUpload.Name = "BtnUpload";
            this.BtnUpload.Size = new System.Drawing.Size(115, 35);
            this.BtnUpload.Text = "Tải ảnh lên";
            this.BtnUpload.Click += new System.EventHandler(this.BtnUpload_Click);
            
            // BtnRemoveAvatar
            this.BtnRemoveAvatar.Location = new System.Drawing.Point(178, 140);
            this.BtnRemoveAvatar.Name = "BtnRemoveAvatar";
            this.BtnRemoveAvatar.Size = new System.Drawing.Size(115, 35);
            this.BtnRemoveAvatar.Text = "Xóa ảnh";
            this.BtnRemoveAvatar.IsSecondary = true;
            this.BtnRemoveAvatar.Click += new System.EventHandler(this.BtnRemoveAvatar_Click);
            
            // TabBoard
            this.TabBoard.Controls.Add(this.LblTheme);
            this.TabBoard.Controls.Add(this.CmbTheme);
            this.TabBoard.Controls.Add(this.LblSecColor);
            this.TabBoard.Controls.Add(this.CmbSecColor);
            this.TabBoard.Location = new System.Drawing.Point(4, 26);
            this.TabBoard.Name = "TabBoard";
            this.TabBoard.Padding = new System.Windows.Forms.Padding(3);
            this.TabBoard.Size = new System.Drawing.Size(342, 220);
            this.TabBoard.Text = "Bàn cờ";
            this.TabBoard.UseVisualStyleBackColor = true;
            
            // LblTheme
            this.LblTheme.AutoSize = true;
            this.LblTheme.Location = new System.Drawing.Point(20, 20);
            this.LblTheme.Name = "LblTheme";
            this.LblTheme.Text = "Chủ đề (Theme):";
            
            // CmbTheme
            this.CmbTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbTheme.Location = new System.Drawing.Point(20, 45);
            this.CmbTheme.Name = "CmbTheme";
            this.CmbTheme.Size = new System.Drawing.Size(200, 25);
            this.CmbTheme.SelectedIndexChanged += new System.EventHandler(this.UI_Changed);
            
            // LblSecColor
            this.LblSecColor.AutoSize = true;
            this.LblSecColor.Location = new System.Drawing.Point(20, 90);
            this.LblSecColor.Name = "LblSecColor";
            this.LblSecColor.Text = "Loại Khung:";
            
            // CmbSecColor
            this.CmbSecColor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbSecColor.Location = new System.Drawing.Point(20, 115);
            this.CmbSecColor.Name = "CmbSecColor";
            this.CmbSecColor.Size = new System.Drawing.Size(200, 25);
            this.CmbSecColor.SelectedIndexChanged += new System.EventHandler(this.UI_Changed);
            
            // TabPiece
            this.TabPiece.Controls.Add(this.LblPieceShape);
            this.TabPiece.Controls.Add(this.CmbPieceShape);
            this.TabPiece.Controls.Add(this.LblOpponentShape);
            this.TabPiece.Controls.Add(this.CmbOpponentShape);
            this.TabPiece.Controls.Add(this.LblEffect);
            this.TabPiece.Controls.Add(this.CmbEffect);
            this.TabPiece.Location = new System.Drawing.Point(4, 26);
            this.TabPiece.Name = "TabPiece";
            this.TabPiece.Padding = new System.Windows.Forms.Padding(3);
            this.TabPiece.Size = new System.Drawing.Size(342, 220);
            this.TabPiece.Text = "Quân cờ";
            this.TabPiece.UseVisualStyleBackColor = true;
            
            // LblPieceShape
            this.LblPieceShape.AutoSize = true;
            this.LblPieceShape.Location = new System.Drawing.Point(20, 20);
            this.LblPieceShape.Name = "LblPieceShape";
            this.LblPieceShape.Text = "Hình quân mình:";
            
            // CmbPieceShape
            this.CmbPieceShape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbPieceShape.Location = new System.Drawing.Point(20, 45);
            this.CmbPieceShape.Name = "CmbPieceShape";
            this.CmbPieceShape.Size = new System.Drawing.Size(200, 25);
            this.CmbPieceShape.SelectedIndexChanged += new System.EventHandler(this.UI_Changed);
            
            // LblOpponentShape
            this.LblOpponentShape.AutoSize = true;
            this.LblOpponentShape.Location = new System.Drawing.Point(20, 80);
            this.LblOpponentShape.Name = "LblOpponentShape";
            this.LblOpponentShape.Text = "Hình đối thủ:";
            
            // CmbOpponentShape
            this.CmbOpponentShape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbOpponentShape.Location = new System.Drawing.Point(20, 105);
            this.CmbOpponentShape.Name = "CmbOpponentShape";
            this.CmbOpponentShape.Size = new System.Drawing.Size(200, 25);
            this.CmbOpponentShape.SelectedIndexChanged += new System.EventHandler(this.UI_Changed);
            
            // LblEffect
            this.LblEffect.AutoSize = true;
            this.LblEffect.Location = new System.Drawing.Point(20, 140);
            this.LblEffect.Name = "LblEffect";
            this.LblEffect.Text = "Hiệu ứng quân cờ:";
            
            // CmbEffect
            this.CmbEffect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CmbEffect.Location = new System.Drawing.Point(20, 165);
            this.CmbEffect.Name = "CmbEffect";
            this.CmbEffect.Size = new System.Drawing.Size(200, 25);
            this.CmbEffect.SelectedIndexChanged += new System.EventHandler(this.UI_Changed);
            
            // LblPreview
            this.LblPreview.AutoSize = true;
            this.LblPreview.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.LblPreview.Location = new System.Drawing.Point(380, 12);
            this.LblPreview.Name = "LblPreview";
            this.LblPreview.Text = "XEM TRƯỚC";
            
            // PnlPreview
            this.PnlPreview.Location = new System.Drawing.Point(380, 38);
            this.PnlPreview.Name = "PnlPreview";
            this.PnlPreview.Size = new System.Drawing.Size(200, 200);
            this.PnlPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlPreview_Paint);
            
            // BtnCancel
            this.BtnCancel.Location = new System.Drawing.Point(380, 280);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(95, 40);
            this.BtnCancel.Text = "HỦY";
            this.BtnCancel.IsSecondary = true;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            
            // BtnSave
            this.BtnSave.Location = new System.Drawing.Point(485, 280);
            this.BtnSave.Name = "BtnSave";
            this.BtnSave.Size = new System.Drawing.Size(95, 40);
            this.BtnSave.Text = "LƯU";
            this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
            
            // PersonalizationForm
            this.ClientSize = new System.Drawing.Size(600, 340);
            this.Controls.Add(this.BtnSave);
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.PnlPreview);
            this.Controls.Add(this.LblPreview);
            this.Controls.Add(this.TabControlMain);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PersonalizationForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Cá Nhân Hóa";
            
            this.TabControlMain.ResumeLayout(false);
            this.TabProfile.ResumeLayout(false);
            this.TabBoard.ResumeLayout(false);
            this.TabBoard.PerformLayout();
            this.TabPiece.ResumeLayout(false);
            this.TabPiece.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PicAvatar)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
