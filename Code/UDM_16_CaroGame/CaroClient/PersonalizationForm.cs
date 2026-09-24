using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CaroClient.Settings;
using CaroClient.Drawing;

namespace CaroClient
{
    public partial class PersonalizationForm : CaroForm
    {
        private PlayerPersonalizationSettings _tempSettings;
        private string? _pendingAvatarPath = null;
        private bool _pendingAvatarRemove = false;
        private Image? _currentAvatar;
        private readonly SoftLoadingIndicator _avatarLoading = new() { Visible = false };
        private readonly Label _avatarStatus = new() { AutoEllipsis = true, ForeColor = CaroTheme.TextMuted };
        private TaskCompletionSource<string?>? _avatarAck;

        public PersonalizationForm()
        {
            InitializeComponent();
            _avatarStatus.SetBounds(380, 244, 200, 20);
            _avatarLoading.SetBounds(380, 266, 200, 10);
            Controls.AddRange([_avatarStatus, _avatarLoading]);
            CaroClient.Network.NetworkClient.Instance.OnAvatarUpdateResponse += AvatarUpdateAcknowledged;
            CaroClient.Network.NetworkClient.Instance.OnAvatarRemoveResponse += AvatarRemoveAcknowledged;
            CaroClient.Network.NetworkClient.Instance.OnDisconnected += AvatarDisconnected;
            
            // Clone current settings to temp
            _tempSettings = (PlayerPersonalizationSettings)PersonalizationManager.Instance.Settings.Clone();

            // Load Avatar
            string myNick = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
            var session = CaroClient.Network.NetworkClient.Instance; 
            // Wait, we need to know if we have avatar currently. AvatarManager caches it.
            // In CaroClient, AvatarManager can return null if not loaded yet.
            // But we can subscribe to OnAvatarUpdated to get it.
            
            this.DoubleBuffered = true;
            this.BackColor = CaroTheme.Background;
            this.ForeColor = CaroTheme.TextDark;

            LoadSettingsToUI();
            
            AvatarManager.Instance.OnAvatarUpdated += OnAvatarUpdated;
            // Fetch initial avatar
            // For Personalization, we assume the player's info is in Lobby list, but if not we can't get version.
            // So we just request it directly.
            _ = CaroClient.Network.NetworkClient.Instance.SendAvatarRequestAsync(myNick);
        }

        private void OnAvatarUpdated(string playerId, Image? avatar)
        {
            string myNick = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
            if (playerId == myNick)
            {
                if (IsDisposed || !IsHandleCreated) return;
                var copy = avatar != null ? (Image)avatar.Clone() : null;
                try { BeginInvoke(() =>
                {
                    if (!IsDisposed && !_pendingAvatarRemove && _pendingAvatarPath == null)
                    {
                        if (_currentAvatar != null) _currentAvatar.Dispose();
                        _currentAvatar = copy;
                        PicAvatar.Invalidate();
                    }
                    else copy?.Dispose();
                }); }
                catch (InvalidOperationException) { copy?.Dispose(); }
            }
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            if (this.InvokeRequired) this.BeginInvoke(action);
            else action();
        }

        private void LoadSettingsToUI()
        {
            // Board
            CmbTheme.DataSource = Enum.GetValues(typeof(BoardThemePreset));
            CmbTheme.SelectedItem = _tempSettings.BoardAppearance.Theme;
            
            CmbSecColor.DataSource = Enum.GetValues(typeof(CaroClient.Settings.FrameStyle));
            CmbSecColor.SelectedItem = _tempSettings.BoardAppearance.FrameStyle;

            // Piece
            CmbPieceShape.DataSource = Enum.GetValues(typeof(PieceShape));
            CmbPieceShape.SelectedItem = _tempSettings.PieceAppearance.MyShape;

            CmbOpponentShape.DataSource = Enum.GetValues(typeof(PieceShape));
            CmbOpponentShape.SelectedItem = _tempSettings.PieceAppearance.OpponentShape;

            CmbEffect.DataSource = Enum.GetValues(typeof(PieceEffect));
            CmbEffect.SelectedItem = _tempSettings.PieceAppearance.Effect;
        }

        private void ApplyUIToSettings()
        {
            _tempSettings.BoardAppearance.Theme = (BoardThemePreset)(CmbTheme.SelectedItem ?? BoardThemePreset.ClassicWood);
            _tempSettings.BoardAppearance.FrameStyle = (CaroClient.Settings.FrameStyle)(CmbSecColor.SelectedItem ?? CaroClient.Settings.FrameStyle.ClassicWood);

            _tempSettings.PieceAppearance.MyShape = (PieceShape)(CmbPieceShape.SelectedItem ?? PieceShape.ClassicX);
            _tempSettings.PieceAppearance.OpponentShape = (PieceShape)(CmbOpponentShape.SelectedItem ?? PieceShape.ClassicO);
            _tempSettings.PieceAppearance.Effect = (PieceEffect)(CmbEffect.SelectedItem ?? PieceEffect.Soft3D);
        }

        private async void BtnSave_Click(object sender, EventArgs e)
        {
            if (_avatarLoading.Visible) return;
            ApplyUIToSettings();
            PersonalizationManager.Instance.CommitTemporarySettings(_tempSettings);

            if (_pendingAvatarRemove || _pendingAvatarPath != null)
            {
                BtnSave.Enabled = false;
                _avatarLoading.Visible = true;
                _avatarAck = new(TaskCreationOptions.RunContinuationsAsynchronously);
                try
                {
                    if (_pendingAvatarRemove)
                    {
                        _avatarStatus.Text = "ĐANG ĐỒNG BỘ ẢNH...";
                        await CaroClient.Network.NetworkClient.Instance.SendAvatarRemoveAsync();
                    }
                    else
                    {
                        _avatarStatus.Text = "ĐANG XỬ LÝ ẢNH...";
                        string path = _pendingAvatarPath!;
                        string base64 = await Task.Run(() => AvatarManager.ProcessAndEncodeAvatar(path));
                        if (IsDisposed) return;
                        _avatarStatus.Text = "ĐANG ĐỒNG BỘ ẢNH...";
                        await CaroClient.Network.NetworkClient.Instance.SendAvatarUpdateAsync(base64);
                    }
                    string? error = await _avatarAck.Task.WaitAsync(TimeSpan.FromSeconds(15));
                    if (error != null) throw new InvalidOperationException(error);
                    if (Owner is Form owner) ToastNotification.Show(owner, "ĐÃ ĐỒNG BỘ ẢNH", ToastType.Success);
                }
                catch (Exception ex)
                {
                    if (IsDisposed) return;
                    _avatarStatus.Text = "Đồng bộ chưa thành công";
                    CaroDialogForm.Show(this, "Lỗi xử lý ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                finally
                {
                    if (!IsDisposed) { BtnSave.Enabled = true; _avatarLoading.Visible = false; }
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void AvatarUpdateAcknowledged(CaroShared.Contracts.AvatarUpdateResponse response) =>
            _avatarAck?.TrySetResult(response.Success ? null : response.Message ?? "Máy chủ từ chối cập nhật ảnh.");
        private void AvatarRemoveAcknowledged(CaroShared.Contracts.AvatarRemoveResponse response) =>
            _avatarAck?.TrySetResult(response.Success ? null : response.Message ?? "Máy chủ từ chối xóa ảnh.");
        private void AvatarDisconnected() => _avatarAck?.TrySetResult("Mất kết nối tới máy chủ.");

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void BtnUpload_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog();
            ofd.Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _pendingAvatarPath = ofd.FileName;
                _pendingAvatarRemove = false;
                
                try
                {
                    var img = Image.FromFile(_pendingAvatarPath);
                    if (_currentAvatar != null) _currentAvatar.Dispose();
                    _currentAvatar = (Image)img.Clone();
                    img.Dispose();
                    PicAvatar.Invalidate();
                }
                catch (Exception ex)
                {
                    CaroDialogForm.Show(this, "Không thể đọc file ảnh: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _pendingAvatarPath = null;
                }
            }
        }

        private void BtnRemoveAvatar_Click(object sender, EventArgs e)
        {
            _pendingAvatarPath = null;
            _pendingAvatarRemove = true;
            if (_currentAvatar != null)
            {
                _currentAvatar.Dispose();
                _currentAvatar = null;
            }
            PicAvatar.Invalidate();
        }

        private void PnlPreview_Paint(object sender, PaintEventArgs e)
        {
            ApplyUIToSettings();
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Generate Palette
            var palette = BoardPaletteGenerator.GeneratePreset(_tempSettings.BoardAppearance.Theme);
            
            // Draw Mini Board
            int cellSize = 50;
            int margin = 20;
            
            g.Clear(palette.SurfaceColor);
            
            using var gridPen = new Pen(palette.GridColor, 2f);
            for (int i = 0; i <= 3; i++)
            {
                g.DrawLine(gridPen, margin, margin + i * cellSize, margin + 3 * cellSize, margin + i * cellSize);
                g.DrawLine(gridPen, margin + i * cellSize, margin, margin + i * cellSize, margin + 3 * cellSize);
            }

            // Draw Pieces
            Rectangle rectX = new Rectangle(margin + 5, margin + 5, cellSize - 10, cellSize - 10);
            PieceRenderer.DrawPiece(g, rectX, _tempSettings.PieceAppearance.MyShape, _tempSettings.PieceAppearance.MyColor, _tempSettings.PieceAppearance.Effect, _tempSettings.PieceAppearance.Intensity);

            Rectangle rectO = new Rectangle(margin + cellSize + 5, margin + cellSize + 5, cellSize - 10, cellSize - 10);
            PieceRenderer.DrawPiece(g, rectO, _tempSettings.PieceAppearance.OpponentShape, _tempSettings.PieceAppearance.OpponentColor, _tempSettings.PieceAppearance.Effect, _tempSettings.PieceAppearance.Intensity);
        }

        private void PicAvatar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            
            Rectangle rect = new Rectangle(0, 0, PicAvatar.Width - 1, PicAvatar.Height - 1);
            if (_currentAvatar != null)
            {
                using var path = new GraphicsPath();
                path.AddEllipse(rect);
                g.SetClip(path);
                g.DrawImage(_currentAvatar, rect);
                g.ResetClip();
            }
            else
            {
                g.FillEllipse(Brushes.LightGray, rect);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                string name = CaroClient.Network.NetworkClient.Instance.CurrentNickname;
                if (string.IsNullOrEmpty(name)) name = "?";
                using var avatarFont = new Font("Segoe UI", 24, FontStyle.Bold);
                g.DrawString(name.Substring(0, 1).ToUpper(), avatarFont, Brushes.White, rect, sf);
            }
            using var borderPen = new Pen(CaroTheme.WoodFrame, 2f);
            g.DrawEllipse(borderPen, rect);
        }

        private void UI_Changed(object sender, EventArgs e)
        {
            PnlPreview.Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _avatarAck?.TrySetCanceled();
            CaroClient.Network.NetworkClient.Instance.OnAvatarUpdateResponse -= AvatarUpdateAcknowledged;
            CaroClient.Network.NetworkClient.Instance.OnAvatarRemoveResponse -= AvatarRemoveAcknowledged;
            CaroClient.Network.NetworkClient.Instance.OnDisconnected -= AvatarDisconnected;
            AvatarManager.Instance.OnAvatarUpdated -= OnAvatarUpdated;
            if (_currentAvatar != null)
            {
                _currentAvatar.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
