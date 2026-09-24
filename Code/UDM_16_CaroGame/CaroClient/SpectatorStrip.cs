using System.Drawing.Drawing2D;
using CaroClient.Settings;
using CaroShared.Contracts;

namespace CaroClient;

public sealed class SpectatorStrip : Soft3DPanel
{
    private readonly Label _count = new() { Name = "SpectatorCount", TextAlign = ContentAlignment.MiddleLeft, ForeColor = CaroTheme.TextMuted, AutoEllipsis = true, BackColor = CaroTheme.Card };
    private readonly FlowLayoutPanel _people = new() { WrapContents = false, AutoScroll = true, FlowDirection = FlowDirection.LeftToRight, BackColor = CaroTheme.Card };
    private readonly Dictionary<string, Chip> _chips = new();
    public SpectatorStrip()
    {
        DoubleBuffered = true; BackColor = CaroTheme.Background; CornerRadius = 12;
        _count.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        Controls.Add(_people); Controls.Add(_count);
        AvatarManager.Instance.OnAvatarUpdated += AvatarUpdated;
    }
    public void SetPeople(IReadOnlyList<PlayerInfoDto> people)
    {
        _count.Text = $"Khán giả ({people.Count})";
        _people.SuspendLayout();
        try
        {
            foreach (string id in _chips.Keys.Except(people.Select(p => p.PlayerName)).ToArray())
            { _people.Controls.Remove(_chips[id]); _chips[id].Dispose(); _chips.Remove(id); }
            foreach (var person in people)
            {
                if (!_chips.TryGetValue(person.PlayerName, out var chip))
                {
                    chip = new Chip(person.PlayerName);
                    _chips.Add(person.PlayerName, chip); _people.Controls.Add(chip);
                }
                chip.SetImage(AvatarManager.Instance.GetAvatar(person.PlayerName, person.AvatarVersion, person.HasAvatar));
            }
        }
        finally { _people.ResumeLayout(); PerformLayout(); }
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_count == null) return;
        float scale = DeviceDpi / 96f;
        int pad = (int)(12 * scale), title = (int)(20 * scale);
        _count.SetBounds(pad, pad / 2, Width - 2 * pad, title);
        _people.SetBounds(pad, _count.Bottom, Width - 2 * pad, Math.Max(1, Height - _count.Bottom - pad / 2));
        foreach (Control chip in _people.Controls)
        {
            chip.Size = new Size((int)(140 * scale), (int)(32 * scale));
            chip.Margin = new Padding(0, 0, (int)(8 * scale), 0);
        }
    }
    private void AvatarUpdated(string name, Image? image)
    {
        if (!IsHandleCreated || IsDisposed) return;
        // Clone before queuing: the cache owns and may replace its image.
        var copy = image == null ? null : (Image)image.Clone();
        try { BeginInvoke(() => { if (!IsDisposed && _chips.TryGetValue(name, out var chip)) chip.SetImage(copy); else copy?.Dispose(); }); }
        catch (InvalidOperationException) { copy?.Dispose(); }
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) AvatarManager.Instance.OnAvatarUpdated -= AvatarUpdated;
        base.Dispose(disposing);
    }
    private sealed class Chip : Control
    {
        private Image? _image;
        private readonly ToolTip _tip = new();
        private bool _hoveringAvatar;
        public Chip(string name)
        {
            Text = name; AccessibleName = name; Name = "Spectator_" + name;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(140, 32); Font = new Font("Segoe UI", 9); BackColor = CaroTheme.Card;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            _hoveringAvatar = new Rectangle(0, 0, Height, Height).Contains(e.Location);
            if (!_hoveringAvatar) _tip.Hide(this);
        }
        protected override void OnMouseHover(EventArgs e)
        {
            base.OnMouseHover(e);
            if (_hoveringAvatar)
                _tip.Show(Text, this, new Point(0, -Font.Height - 12), 2500);
        }
        protected override void OnMouseLeave(EventArgs e) { _tip.Hide(this); base.OnMouseLeave(e); }
        public void SetImage(Image? image) { var old = _image; _image = image; old?.Dispose(); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e)
        {
            int size = Height - 4;
            var circle = new Rectangle(2, 2, size, size);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath(); path.AddEllipse(circle);
            var saved = e.Graphics.Save(); e.Graphics.SetClip(path);
            if (_image != null) e.Graphics.DrawImage(_image, circle);
            else
            {
                using var brush = new SolidBrush(CaroTheme.ButtonNormal); e.Graphics.FillEllipse(brush, circle);
                TextRenderer.DrawText(e.Graphics, System.Globalization.StringInfo.GetNextTextElement(Text), Font, circle, CaroTheme.ButtonText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            e.Graphics.Restore(saved);
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(size + 8, 0, Width - size - 8, Height), CaroTheme.TextDark,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
        protected override void Dispose(bool disposing) { if (disposing) { _image?.Dispose(); _tip.Dispose(); } base.Dispose(disposing); }
    }
}
