using System.Drawing.Drawing2D;
using CaroClient.Settings;
using CaroShared.Contracts;

namespace CaroClient;

public sealed class SpectatorStrip : Panel
{
    private readonly Label _count = new() { Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleLeft, ForeColor = CaroTheme.TextMuted, AutoEllipsis = true };
    private readonly FlowLayoutPanel _people = new() { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true, FlowDirection = FlowDirection.LeftToRight };
    private readonly Dictionary<string, Chip> _chips = new();
    public SpectatorStrip()
    {
        DoubleBuffered = true; BackColor = CaroTheme.Background;
        Controls.Add(_people); Controls.Add(_count);
        AvatarManager.Instance.OnAvatarUpdated += AvatarUpdated;
    }
    public void SetPeople(IReadOnlyList<PlayerInfoDto> people)
    {
        _count.Text = $"Khán giả ({people.Count}):";
        _count.Width = TextRenderer.MeasureText(_count.Text, _count.Font).Width + 12;
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
        finally { _people.ResumeLayout(); }
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        if (_count != null) _count.Width = TextRenderer.MeasureText(_count.Text, _count.Font).Width + 12;
        base.OnLayout(e);
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
        public Chip(string name)
        {
            Text = name; AccessibleName = name; Name = "Spectator_" + name;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(155, 32); _tip.SetToolTip(this, name);
        }
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
