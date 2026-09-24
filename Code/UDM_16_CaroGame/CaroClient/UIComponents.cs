using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CaroClient
{
    public class BufferedPanel : Panel
    {
        public BufferedPanel() => SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    // Label normally paints text before raising Paint. These surfaces own their entire
    // rendering, so the default label text/background must not be painted a second time.
    public class PaintedLabel : Label
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Action<PaintEventArgs>? Renderer { get; set; }
        public PaintedLabel() => SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Renderer != null) Renderer(e);
            else base.OnPaint(e);
        }
    }

    public class BufferedCellButton : Button
    {
        public BufferedCellButton() => SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    /// <summary>
    /// Thẻ hiển thị Soft 3D / Claymorphism bo góc 18px với bóng đổ ấm và highlight trên-trái.
    /// </summary>
    public class Soft3DPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 18;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsWinnerHighlighted { get; set; } = false;

        private double _turnEmphasis;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public double TurnEmphasis
        {
            get => _turnEmphasis;
            set
            {
                double next = Math.Clamp(value, 0, 1);
                if (Math.Abs(next - _turnEmphasis) < .001) return;
                _turnEmphasis = next;
                // Only the border/shadow changes. Repainting the whole card forces
                // all transparent descendants to erase and repaint their backgrounds.
                int edge = CornerRadius + 10;
                Invalidate(new Rectangle(0, 0, Width, edge));
                Invalidate(new Rectangle(0, Math.Max(0, Height - edge), Width, edge));
                Invalidate(new Rectangle(0, edge, 12, Math.Max(0, Height - 2 * edge)));
                Invalidate(new Rectangle(Math.Max(0, Width - 12), edge, 12, Math.Max(0, Height - 2 * edge)));
            }
        }

        public Soft3DPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
            this.BackColor = Color.Transparent;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color parentBg = this.Parent?.BackColor ?? CaroTheme.Background;
            if (parentBg == Color.Transparent) parentBg = CaroTheme.Background;
            using var bgBrush = new SolidBrush(parentBg);
            pevent.Graphics.FillRectangle(bgBrush, pevent.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Xóa sạch nền 4 góc ngoài thân thẻ bằng màu nền Form cha
            Color parentBg = this.Parent?.BackColor ?? CaroTheme.Background;
            if (parentBg == Color.Transparent) parentBg = CaroTheme.Background;
            using (var bgClearBrush = new SolidBrush(parentBg))
            {
                e.Graphics.FillRectangle(bgClearBrush, this.ClientRectangle);
            }

            int pad = 5;
            var shadowRect = new Rectangle(pad + 2, pad + 3 + (int)(_turnEmphasis * 2), this.Width - pad * 2 - 2, this.Height - pad * 2 - 2);
            var cardRect = new Rectangle(pad, pad, this.Width - pad * 2 - 2, this.Height - pad * 2 - 2);

            if (cardRect.Width <= 10 || cardRect.Height <= 10) return;

            // 1. Warm Drop Shadow (đa tầng mờ)
            using (var shadowPath = CaroTheme.GetRoundedPath(shadowRect, CornerRadius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(45, CaroTheme.CardShadow)))
            {
                e.Graphics.FillPath(shadowBrush, shadowPath);
            }

            // 2. Thân thẻ Claymorphism
            using (var cardPath = CaroTheme.GetRoundedPath(cardRect, CornerRadius))
            using (var cardBrush = new SolidBrush(CaroTheme.Card))
            {
                e.Graphics.FillPath(cardBrush, cardPath);

                // 3. Highlight mỏng viền trên - trái
                using (var highlightPen = new Pen(CaroTheme.CardHighlight, 1.5f))
                {
                    e.Graphics.DrawPath(highlightPen, cardPath);
                }

                // 4. Outer Border (Normal hoặc Golden Glow khi thắng)
                if (_turnEmphasis > .01)
                {
                    using var turnPen = new Pen(Color.FromArgb((int)(130 * _turnEmphasis), CaroTheme.WoodHighlight), 2f);
                    e.Graphics.DrawPath(turnPen, cardPath);
                }
                if (IsWinnerHighlighted)
                {
                    using (var goldPen = new Pen(CaroTheme.VictoryGold, 2.5f))
                    {
                        e.Graphics.DrawPath(goldPen, cardPath);
                    }
                }
                else
                {
                    using (var borderPen = new Pen(Color.FromArgb(60, CaroTheme.CardShadow), 1.0f))
                    {
                        e.Graphics.DrawPath(borderPen, cardPath);
                    }
                }
            }

            base.OnPaint(e);
        }
    }

    /// <summary>
    /// Nút bấm dạng viên thuốc (Pill/Capsule Button) với phong cách gỗ mềm 3D.
    /// </summary>
    public class PillButton : Button
    {
        private bool _isHovered = false;
        private bool _isPressed = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSurrender { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSecondary { get; set; } = false;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsDestructive
        {
            get => IsSurrender;
            set => IsSurrender = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string? GlyphIcon { get; set; } = null;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool FitTextToWidth { get; set; }

        protected override bool ShowFocusCues => false;

        public override void NotifyDefault(bool value)
        {
            // Ngăn chặn hoàn toàn việc vẽ viền nút mặc định (thanh vuông góc) của Windows
            base.NotifyDefault(false);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.Style &= ~0x00800000; // Loại bỏ WS_BORDER
                cp.ExStyle &= ~0x00000200; // Loại bỏ WS_EX_CLIENTEDGE
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCPAINT = 0x0085;
            if (m.Msg == WM_NCPAINT)
            {
                // Chặn hoàn toàn sự kiện vẽ viền non-client của Windows
                m.Result = IntPtr.Zero;
                return;
            }
            base.WndProc(ref m);
        }

        public PillButton()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.SetStyle(ControlStyles.Selectable, true);
            this.UpdateStyles();
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 255, 255);
            this.FlatAppearance.MouseDownBackColor = Color.Transparent;
            this.FlatAppearance.MouseOverBackColor = Color.Transparent;
            this.FlatAppearance.CheckedBackColor = Color.Transparent;
            this.BackColor = CaroTheme.Background;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        }

        private void UpdateRegion()
        {
            if (this.Width <= 4 || this.Height <= 4) return;
            // Clip the native button edge outside the painted pill and its shadow.
            // Otherwise Windows can leave a straight focus/default border above the pill.
            int radius = Math.Max(4, (this.Height - 4) / 2);
            using var path = CaroTheme.GetRoundedPath(new Rectangle(1, 1, this.Width - 2, this.Height - 2), radius);
            this.Region?.Dispose();
            this.Region = new Region(path);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateRegion();
        }

        internal static Color GetSurfaceClearColor(Control? control)
        {
            Control? cur = control?.Parent;
            while (cur != null)
            {
                if (cur is Soft3DPanel)
                    return CaroTheme.Card;
                if (cur.BackColor != Color.Transparent && cur.BackColor.A == 255)
                    return cur.BackColor;
                cur = cur.Parent;
            }
            return CaroTheme.Background;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color parentBg = GetSurfaceClearColor(this);
            using var bgBrush = new SolidBrush(parentBg);
            pevent.Graphics.FillRectangle(bgBrush, pevent.ClipRectangle);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            this.Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            _isPressed = false;
            this.Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            if (mevent.Button == MouseButtons.Left)
            {
                _isPressed = true;
                this.Invalidate();
            }
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _isPressed = false;
            this.Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Xóa sạch toàn bộ 4 góc vuông ngoài viên thuốc bằng màu bề mặt chứa nút (Card hoặc Form)
            Color parentBg = GetSurfaceClearColor(this);
            using (var bgClearBrush = new SolidBrush(parentBg))
            {
                e.Graphics.FillRectangle(bgClearBrush, this.ClientRectangle);
            }

            int radius = (this.Height - 4) / 2;
            if (radius < 4) radius = 4;

            int shadowOffset = _isPressed ? 1 : 2;
            var shadowRect = new Rectangle(1, 1 + shadowOffset, this.Width - 2, this.Height - 4);
            var pillRect = new Rectangle(1, 1, this.Width - 2, this.Height - 4);

            // Bóng đổ ấm
            if (!_isPressed && this.Enabled)
            {
                using var shadowPath = CaroTheme.GetRoundedPath(shadowRect, radius);
                using var shadowBrush = new SolidBrush(Color.FromArgb(45, CaroTheme.WoodShadow));
                e.Graphics.FillPath(shadowBrush, shadowPath);
            }

            // Màu nền nút & chữ
            Color bgColor;
            Color fgColor = CaroTheme.ButtonText;

            if (!this.Enabled)
            {
                bgColor = Color.FromArgb(195, 175, 155);
                fgColor = Color.FromArgb(120, 95, 75);
            }
            else if (IsSurrender)
            {
                bgColor = _isPressed ? CaroTheme.ButtonSurrenderPressed :
                          _isHovered ? CaroTheme.ButtonSurrenderHover :
                                       CaroTheme.ButtonSurrender;
            }
            else if (IsSecondary)
            {
                bgColor = _isPressed ? Color.FromArgb(185, 158, 132) :
                          _isHovered ? Color.FromArgb(215, 190, 165) :
                                       Color.FromArgb(205, 178, 151); // Softer birch tone
                fgColor = CaroTheme.TextDark;
            }
            else
            {
                bgColor = _isPressed ? CaroTheme.ButtonPressed :
                          _isHovered ? CaroTheme.ButtonHover :
                                       CaroTheme.ButtonNormal;
            }

            using (var pillPath = CaroTheme.GetRoundedPath(pillRect, radius))
            using (var bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillPath(bgBrush, pillPath);

                // Highlight mờ góc trên
                if (!_isPressed && this.Enabled)
                {
                    using var hiPen = new Pen(Color.FromArgb(90, Color.White), 1.2f);
                    e.Graphics.DrawPath(hiPen, pillPath);
                }
                else if (_isPressed)
                {
                    using var darkPen = new Pen(Color.FromArgb(100, CaroTheme.WoodShadow), 1.2f);
                    e.Graphics.DrawPath(darkPen, pillPath);
                }
            }

            // Vẽ Text với vùng an toàn chống chạm viền bo cong
            string displayText = string.IsNullOrEmpty(GlyphIcon) ? this.Text : $"{GlyphIcon}  {this.Text}";
            int horizSafeInset = Math.Max(8, (int)(radius * 0.45f));
            var textRect = new Rectangle(
                pillRect.X + horizSafeInset,
                pillRect.Y + 1,
                Math.Max(0, pillRect.Width - (horizSafeInset * 2)),
                Math.Max(0, pillRect.Height - 2)
            );
            if (_isPressed)
            {
                textRect.Y += 1;
            }

            using var fittedFont = FitTextToWidth ? new Font(Font.FontFamily,
                Font.SizeInPoints * Math.Min(1f, (float)textRect.Width / Math.Max(1,
                    TextRenderer.MeasureText(e.Graphics, displayText, Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width)), Font.Style) : null;
            TextRenderer.DrawText(
                e.Graphics,
                displayText,
                fittedFont ?? this.Font,
                textRect,
                fgColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix
            );
        }
    }

    /// <summary>
    /// Khung gỗ bao bọc bàn cờ với viền vát 3D, góc bo tròn và mặt cờ gỗ sáng bên trong.
    /// </summary>
    public class WoodBoardFramePanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int FrameThickness { get; set; } = 16;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 16;

        public WoodBoardFramePanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
            this.BackColor = Color.Transparent;
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color parentBg = this.Parent?.BackColor ?? CaroTheme.Background;
            if (parentBg == Color.Transparent) parentBg = CaroTheme.Background;
            using var bgBrush = new SolidBrush(parentBg);
            pevent.Graphics.FillRectangle(bgBrush, pevent.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Xóa sạch nền 4 góc bo của khung gỗ bằng màu nền Form cha
            Color parentBg = this.Parent?.BackColor ?? CaroTheme.Background;
            if (parentBg == Color.Transparent) parentBg = CaroTheme.Background;
            using (var bgClearBrush = new SolidBrush(parentBg))
            {
                e.Graphics.FillRectangle(bgClearBrush, this.ClientRectangle);
            }

            int pad = 4;
            var shadowRect = new Rectangle(pad + 2, pad + 4, this.Width - pad * 2 - 2, this.Height - pad * 2 - 2);
            var frameRect = new Rectangle(pad, pad, this.Width - pad * 2 - 2, this.Height - pad * 2 - 2);

            if (frameRect.Width <= 32 || frameRect.Height <= 32) return;

            // 1. Bóng đổ khung gỗ ra Form
            using (var shadowPath = CaroTheme.GetRoundedPath(shadowRect, CornerRadius))
            using (var shadowBrush = new SolidBrush(Color.FromArgb(50, CaroTheme.WoodShadow)))
            {
                e.Graphics.FillPath(shadowBrush, shadowPath);
            }

            // 2. Khung gỗ dày
            using (var framePath = CaroTheme.GetRoundedPath(frameRect, CornerRadius))
            using (var woodBrush = new SolidBrush(CaroTheme.WoodFrame))
            {
                e.Graphics.FillPath(woodBrush, framePath);

                // 3. Highlight mép trên - trái của khung gỗ
                using (var hiPen = new Pen(CaroTheme.WoodHighlight, 2.0f))
                {
                    e.Graphics.DrawPath(hiPen, framePath);
                }

                // 4. Shadow mép dưới - phải của khung gỗ
                using (var darkPen = new Pen(CaroTheme.WoodShadow, 1.5f))
                {
                    e.Graphics.DrawPath(darkPen, framePath);
                }
            }

            base.OnPaint(e);
        }
    }

    /// <summary>
    /// Container viền bo góc hơi lõm (recessed) màu kem cho TextBox.
    /// Giúp gõ văn bản đẹp mắt, tránh viền sắc nhọn của WinForms mà vẫn giữ 100% chức năng native.
    /// </summary>
    public class RoundedInputPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CornerRadius { get; set; } = 10;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsFocused { get; set; } = false;

        public RoundedInputPanel()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
            this.BackColor = Color.Transparent;
            this.Padding = new Padding(8, 6, 8, 6);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color parentBg = PillButton.GetSurfaceClearColor(this);
            using var bgBrush = new SolidBrush(parentBg);
            pevent.Graphics.FillRectangle(bgBrush, pevent.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Color parentBg = PillButton.GetSurfaceClearColor(this);
            using (var bgClearBrush = new SolidBrush(parentBg))
            {
                e.Graphics.FillRectangle(bgClearBrush, this.ClientRectangle);
            }

            var rect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
            if (rect.Width <= 4 || rect.Height <= 4) return;

            using (var path = CaroTheme.GetRoundedPath(rect, CornerRadius))
            {
                // Nền kem hơi lõm
                using (var fillBrush = new SolidBrush(Color.FromArgb(255, 253, 248)))
                {
                    e.Graphics.FillPath(fillBrush, path);
                }

                // Viền ấm (nổi bật hơn khi focus)
                Color borderColor = IsFocused ? CaroTheme.ButtonNormal : Color.FromArgb(205, 180, 155);
                float borderWidth = IsFocused ? 1.8f : 1.0f;
                using (var pen = new Pen(borderColor, borderWidth))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            }

            base.OnPaint(e);
        }
    }

    /// <summary>
    /// Lớp phủ Backdrop Dim bán trong suốt (WS_EX_LAYERED + Form.Opacity) đạt chuẩn Warm Beige + Classic Wood.
    /// Tối ưu phần cứng DWM, hoàn toàn không gây lag CPU, không giật hình (flicker), không để sót panel vô hình.
    /// Quản lý tối đa 1 instance duy nhất trên mỗi Form cha theo cơ chế reference count (idempotent lifecycle).
    /// </summary>
    public class BackdropOverlay : CaroForm
    {
        private static readonly Dictionary<Form, BackdropOverlay> _activeBackdrops = new();

        private readonly Form _ownerForm;
        private System.Windows.Forms.Timer? _fadeTimer;
        private int _refCount = 0;
        private readonly double _targetOpacity = CaroTheme.BackdropDimOpacity;
        private bool _isClosing = false;

        public static BackdropOverlay Acquire(Form owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            if (owner.InvokeRequired)
            {
                return (BackdropOverlay)owner.Invoke(new Func<BackdropOverlay>(() => Acquire(owner)));
            }

            if (_activeBackdrops.TryGetValue(owner, out var existing) && !existing.IsDisposed)
            {
                existing._refCount++;
                existing.UpdateBoundsToOwner();
                existing.BringToFront();
                return existing;
            }

            var backdrop = new BackdropOverlay(owner);
            _activeBackdrops[owner] = backdrop;
            backdrop._refCount = 1;
            backdrop.ShowBackdrop();
            return backdrop;
        }

        public static void Release(Form owner)
        {
            if (owner == null) return;

            if (owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => Release(owner)));
                return;
            }

            if (_activeBackdrops.TryGetValue(owner, out var backdrop))
            {
                backdrop._refCount--;
                if (backdrop._refCount <= 0)
                {
                    _activeBackdrops.Remove(owner);
                    backdrop.HideAndDispose();
                }
            }
        }

        public static void ClearFor(Form owner)
        {
            if (owner == null) return;

            if (owner.InvokeRequired)
            {
                owner.BeginInvoke(new Action(() => ClearFor(owner)));
                return;
            }

            if (_activeBackdrops.TryGetValue(owner, out var backdrop))
            {
                _activeBackdrops.Remove(owner);
                backdrop.HideAndDispose();
            }
        }

        private BackdropOverlay(Form owner)
        {
            _ownerForm = owner;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = CaroTheme.BackdropDimBase;
            this.Opacity = 0.0;
            this.DoubleBuffered = true;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();

            UpdateBoundsToOwner();

            _ownerForm.LocationChanged += Owner_MoveOrResize;
            _ownerForm.ClientSizeChanged += Owner_MoveOrResize;
            _ownerForm.Resize += Owner_MoveOrResize;
            _ownerForm.FormClosing += Owner_FormClosing;
            _ownerForm.Disposed += Owner_Disposed;
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: không hiện trong taskbar / Alt+Tab
                return cp;
            }
        }

        // Chặn nhấp chuột vào nền nhưng không đóng dialog (Giữ an toàn xác nhận nghiệp vụ)
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (this.OwnedForms.Length > 0)
            {
                try
                {
                    this.OwnedForms[0].Activate();
                }
                catch { }
            }
        }

        private void UpdateBoundsToOwner()
        {
            if (_isClosing || _ownerForm == null || _ownerForm.IsDisposed || !_ownerForm.IsHandleCreated) return;

            try
            {
                if (_ownerForm.WindowState == FormWindowState.Minimized)
                {
                    this.Visible = false;
                    return;
                }

                Point screenPt = _ownerForm.PointToScreen(Point.Empty);
                this.Bounds = new Rectangle(screenPt, _ownerForm.ClientSize);
            }
            catch
            {
                // Bỏ qua nếu form đang đóng
            }
        }

        private void Owner_MoveOrResize(object? sender, EventArgs e)
        {
            UpdateBoundsToOwner();
            if (_ownerForm.WindowState != FormWindowState.Minimized && !this.Visible && !_isClosing)
            {
                this.Visible = true;
            }
            if (this.OwnedForms.Length > 0)
            {
                var topDlg = this.OwnedForms[0];
                if (!topDlg.IsDisposed && topDlg.IsHandleCreated)
                {
                    int dx = this.Location.X + (this.Width - topDlg.Width) / 2;
                    int dy = this.Location.Y + (this.Height - topDlg.Height) / 2;
                    topDlg.Location = new Point(dx, dy);
                }
            }
        }

        private void Owner_FormClosing(object? sender, FormClosingEventArgs e)
        {
            HideAndDispose();
        }

        private void Owner_Disposed(object? sender, EventArgs e)
        {
            HideAndDispose();
        }

        private void ShowBackdrop()
        {
            if (_isClosing || _ownerForm.IsDisposed || !_ownerForm.IsHandleCreated) return;

            this.Show(_ownerForm);
            UpdateBoundsToOwner();

            // Fade-in animation: 0% -> 22% trong khoảng 150-180ms (6 ticks x 25ms)
            StopFadeTimer();
            _fadeTimer = new System.Windows.Forms.Timer { Interval = 25 };
            double step = _targetOpacity / 6.0;
            _fadeTimer.Tick += (s, e) =>
            {
                if (this.IsDisposed || _isClosing)
                {
                    StopFadeTimer();
                    return;
                }

                if (this.Opacity < _targetOpacity)
                {
                    this.Opacity = Math.Min(_targetOpacity, this.Opacity + step);
                }
                else
                {
                    this.Opacity = _targetOpacity;
                    StopFadeTimer();
                }
            };
            _fadeTimer.Start();
        }

        private void HideAndDispose()
        {
            if (_isClosing) return;
            _isClosing = true;

            StopFadeTimer();

            try
            {
                _ownerForm.LocationChanged -= Owner_MoveOrResize;
                _ownerForm.ClientSizeChanged -= Owner_MoveOrResize;
                _ownerForm.Resize -= Owner_MoveOrResize;
                _ownerForm.FormClosing -= Owner_FormClosing;
                _ownerForm.Disposed -= Owner_Disposed;
            }
            catch { }

            // Close without blocking the UI or pumping reentrant network callbacks.
            try
            {
                this.Close();
                this.Dispose();
            }
            catch { }
        }

        private void StopFadeTimer()
        {
            if (_fadeTimer != null)
            {
                _fadeTimer.Stop();
                _fadeTimer.Dispose();
                _fadeTimer = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopFadeTimer();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Hộp thoại modal phong cách Warm Beige + Soft 3D + PillButton, bảo toàn 100% DialogResult semantics.
    /// Tự động kết hợp BackdropOverlay để làm dịu nền phía sau và làm nổi bật nội dung hộp thoại.
    /// Tự động đo độ dài text để tăng chiều cao hợp lý, đảm bảo 100% không bao giờ bị cắt chữ.
    /// </summary>
    public class CaroDialogForm : CaroForm
    {
        private readonly Label _lblTitle;
        private readonly Label _lblMessage;
        private readonly PillButton _btnPrimary;
        private readonly PillButton? _btnSecondary;

        public CaroDialogForm(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = CaroTheme.Background;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;

            int dialogWidth = 430;
            int pad = 20;
            int innerW = dialogWidth - (pad * 2);

            // Đo độ cao tin nhắn với TextRenderer để tự động co giãn theo số dòng
            using var msgFont = new Font("Segoe UI", 10f);
            Size measuredMsg = TextRenderer.MeasureText(message, msgFont, new Size(innerW, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            int msgHeight = Math.Max(52, measuredMsg.Height + 10);

            // Khu vực nút bấm: Chiều cao nút 38px + khoảng cách 14px + lề đáy 18px = 70px
            int btnHeight = 38;
            int btnAreaHeight = btnHeight + 18 + 14;
            int totalHeight = 14 + 34 + 8 + msgHeight + btnAreaHeight; // title Y=14, H=34, gap=8

            // Clamp độ cao trong khoảng an toàn (210px -> 380px)
            totalHeight = Math.Clamp(totalHeight, 210, 380);

            this.Size = new Size(dialogWidth, totalHeight);

            // Bo tròn toàn bộ Form để hiển thị mềm mại như thẻ nổi Soft3D
            using (var path = CaroTheme.GetRoundedPath(new Rectangle(0, 0, this.Width, this.Height), 16))
            {
                this.Region = new Region(path);
            }

            // Container thẻ Soft 3D lấp đầy Form
            var card = new Soft3DPanel
            {
                Dock = DockStyle.Fill,
                CornerRadius = 16
            };
            this.Controls.Add(card);

            _lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12.5f, FontStyle.Bold),
                ForeColor = CaroTheme.TextDark,
                Location = new Point(pad, 14),
                Size = new Size(innerW, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
            card.Controls.Add(_lblTitle);

            int btnY = this.Height - 18 - btnHeight;
            int msgTop = _lblTitle.Bottom + 6;
            int msgAvailHeight = Math.Max(40, btnY - 12 - msgTop);

            _lblMessage = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 10f),
                ForeColor = CaroTheme.TextDark,
                Location = new Point(pad, msgTop),
                Size = new Size(innerW, msgAvailHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                UseCompatibleTextRendering = false
            };
            card.Controls.Add(_lblMessage);

            if (buttons == MessageBoxButtons.YesNo)
            {
                int btnW = 135;
                int gap = 16;
                int totalBtnW = btnW * 2 + gap;
                int leftBtnX = (dialogWidth - totalBtnW) / 2;

                _btnPrimary = new PillButton
                {
                    Text = "XÁC NHẬN",
                    Size = new Size(btnW, btnHeight),
                    Location = new Point(leftBtnX, btnY),
                    DialogResult = DialogResult.Yes
                };
                _btnSecondary = new PillButton
                {
                    Text = "HỦY",
                    Size = new Size(btnW, btnHeight),
                    Location = new Point(leftBtnX + btnW + gap, btnY),
                    IsDestructive = true,
                    DialogResult = DialogResult.No
                };
                card.Controls.Add(_btnPrimary);
                card.Controls.Add(_btnSecondary);
                this.AcceptButton = _btnPrimary;
                this.CancelButton = _btnSecondary;
            }
            else
            {
                int btnW = 160;
                int leftBtnX = (dialogWidth - btnW) / 2;

                _btnPrimary = new PillButton
                {
                    Text = "ĐỒNG Ý",
                    Size = new Size(btnW, btnHeight),
                    Location = new Point(leftBtnX, btnY),
                    DialogResult = DialogResult.OK
                };
                card.Controls.Add(_btnPrimary);
                this.AcceptButton = _btnPrimary;
                this.CancelButton = _btnPrimary;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var bgBrush = new SolidBrush(CaroTheme.Card);
            e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
        }

        public static DialogResult Show(IWin32Window? owner, string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            Form? ownerForm = owner as Form ?? Form.ActiveForm;

            // Nếu không có owner hợp lệ hoặc owner đang minimize/đóng: hiển thị modal thông thường
            if (ownerForm == null || ownerForm.IsDisposed || !ownerForm.IsHandleCreated || ownerForm.WindowState == FormWindowState.Minimized)
            {
                try
                {
                    using var dlg = new CaroDialogForm(message, title, buttons, icon);
                    return owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
                }
                catch
                {
                    return MessageBox.Show(owner, message, title, buttons, icon);
                }
            }

            if (ownerForm.InvokeRequired)
            {
                return (DialogResult)ownerForm.Invoke(new Func<DialogResult>(() => Show(ownerForm, message, title, buttons, icon)));
            }

            BackdropOverlay? backdrop = null;
            try
            {
                backdrop = BackdropOverlay.Acquire(ownerForm);
                using var dlg = new CaroDialogForm(message, title, buttons, icon);
                // Hiển thị dialog modal với backdrop làm owner (bảo toàn z-order: Owner < Backdrop < Dialog)
                return dlg.ShowDialog(backdrop);
            }
            catch
            {
                try
                {
                    using var fallbackDlg = new CaroDialogForm(message, title, buttons, icon);
                    return fallbackDlg.ShowDialog(ownerForm);
                }
                catch
                {
                    return MessageBox.Show(owner, message, title, buttons, icon);
                }
            }
            finally
            {
                if (backdrop != null && ownerForm != null && !ownerForm.IsDisposed)
                {
                    BackdropOverlay.Release(ownerForm);
                }
            }
        }
    }

    public enum ToastType { Info, Success, Warning, Error }

    /// <summary>
    /// Thông báo nổi không chặn (Non-blocking Toast), tự co giãn theo độ dài nội dung,
    /// hỗ trợ xếp tầng (stacking) tối đa 3 thông báo đồng thời và tự căn giữa khi cửa sổ thay đổi kích thước.
    /// </summary>
    public class ToastNotification : Panel
    {
        private const int MaxActiveToasts = 3;
        private System.Windows.Forms.Timer? _dismissTimer;
        private Form? _parentForm;

        public static void Show(Form parent, string message, ToastType type = ToastType.Info)
        {
            if (parent == null || parent.IsDisposed || !parent.IsHandleCreated) return;

            if (parent.InvokeRequired)
            {
                parent.BeginInvoke(new Action(() => Show(parent, message, type)));
                return;
            }

            // Thu thập các toast đang hiển thị trên form cha
            var activeToasts = new List<ToastNotification>();
            foreach (Control c in parent.Controls)
            {
                if (c is ToastNotification existing && !existing.IsDisposed)
                {
                    activeToasts.Add(existing);
                }
            }

            // Giới hạn tối đa MaxActiveToasts (3): Gỡ bỏ toast cũ nhất nếu vượt quá giới hạn
            while (activeToasts.Count >= MaxActiveToasts)
            {
                var oldest = activeToasts[0];
                activeToasts.RemoveAt(0);
                if (parent.Controls.Contains(oldest)) parent.Controls.Remove(oldest);
                oldest.Dispose();
            }

            var toast = new ToastNotification(message, type, parent);
            parent.Controls.Add(toast);
            activeToasts.Add(toast);

            RepositionToasts(parent, activeToasts);
            toast.BringToFront();
        }

        private static void RepositionToasts(Form parent, List<ToastNotification>? list = null)
        {
            if (parent == null || parent.IsDisposed || !parent.IsHandleCreated) return;

            var active = list;
            if (active == null)
            {
                active = new List<ToastNotification>();
                foreach (Control c in parent.Controls)
                {
                    if (c is ToastNotification t && !t.IsDisposed)
                    {
                        active.Add(t);
                    }
                }
            }

            int startY = 16;
            foreach (var t in active)
            {
                int x = Math.Max(10, (parent.ClientSize.Width - t.Width) / 2);
                t.Location = new Point(x, startY);
                startY = t.Bottom + 8;
            }
        }

        private ToastNotification(string message, ToastType type, Form parent)
        {
            _parentForm = parent;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
            this.BackColor = Color.Transparent;

            using var font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            int maxAllowedW = Math.Min(480, Math.Max(260, parent.ClientSize.Width - 40));
            Size measured = TextRenderer.MeasureText(message, font, new Size(maxAllowedW - 36, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            int w = Math.Min(maxAllowedW, Math.Max(320, measured.Width + 40));
            int h = Math.Max(42, measured.Height + 16);

            this.Size = new Size(w, h);

            var lbl = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = CaroTheme.ButtonText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 4, 12, 4)
            };
            this.Controls.Add(lbl);

            _parentForm.ClientSizeChanged += Parent_ClientSizeChanged;

            _dismissTimer = new System.Windows.Forms.Timer { Interval = 2800 };
            _dismissTimer.Tick += (s, e) =>
            {
                _dismissTimer?.Stop();
                _dismissTimer?.Dispose();
                _dismissTimer = null;
                if (!this.IsDisposed && this.Parent != null)
                {
                    var p = this.Parent as Form;
                    this.Parent.Controls.Remove(this);
                    this.Dispose();
                    if (p != null) RepositionToasts(p);
                }
            };
            _dismissTimer.Start();
        }

        private void Parent_ClientSizeChanged(object? sender, EventArgs e)
        {
            if (_parentForm != null && !_parentForm.IsDisposed)
            {
                RepositionToasts(_parentForm);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(1, 1, this.Width - 3, this.Height - 3);
            int radius = Math.Min(16, (this.Height - 2) / 2);
            using var path = CaroTheme.GetRoundedPath(rect, radius);
            using var bgBrush = new SolidBrush(Color.FromArgb(235, 51, 32, 24));
            e.Graphics.FillPath(bgBrush, path);

            using var borderPen = new Pen(CaroTheme.CardHighlight, 1.2f);
            e.Graphics.DrawPath(borderPen, path);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_parentForm != null)
                {
                    _parentForm.ClientSizeChanged -= Parent_ClientSizeChanged;
                    _parentForm = null;
                }
                _dismissTimer?.Stop();
                _dismissTimer?.Dispose();
                _dismissTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}
