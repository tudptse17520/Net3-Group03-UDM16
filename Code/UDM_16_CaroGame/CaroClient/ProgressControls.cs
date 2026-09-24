using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace CaroClient;

// One timer per UI thread, shared by all presentation controls. No gameplay decisions.
internal static class UiAnimationClock
{
    [ThreadStatic] private static System.Windows.Forms.Timer? _timer;
    [ThreadStatic] private static HashSet<Action<double>>? _listeners;
    internal static double Now => Environment.TickCount64 / 1000d;

    internal static void Subscribe(Action<double> listener)
    {
        _listeners ??= new();
        if (!_listeners.Add(listener)) return;
        if (_timer != null) return;
        _timer = new() { Interval = 33 };
        _timer.Tick += (_, _) =>
        {
            foreach (var tick in _listeners.ToArray())
                if (_listeners.Contains(tick)) tick(Now);
        };
        _timer.Start();
    }

    internal static void Unsubscribe(Action<double> listener)
    {
        _listeners?.Remove(listener);
        if (_listeners?.Count != 0) return;
        _timer?.Dispose();
        _timer = null;
    }
}

public abstract class AnimatedStatusControl : Control
{
    protected AnimatedStatusControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        TabStop = false;
        AccessibleRole = AccessibleRole.StaticText;
    }

    protected abstract void Animate(double now);
    private void Tick(double now) { if (Visible && !IsDisposed) Animate(now); }
    protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); UiAnimationClock.Subscribe(Tick); }
    protected override void OnHandleDestroyed(EventArgs e) { UiAnimationClock.Unsubscribe(Tick); base.OnHandleDestroyed(e); }
    protected override void Dispose(bool disposing)
    {
        if (disposing) UiAnimationClock.Unsubscribe(Tick);
        base.Dispose(disposing);
    }
    protected static Color Blend(Color a, Color b, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb((int)(a.R + (b.R - a.R) * amount),
            (int)(a.G + (b.G - a.G) * amount), (int)(a.B + (b.B - a.B) * amount));
    }
}

public class Soft3DProgressBar : AnimatedStatusControl
{
    private double _target;
    private double _displayed;
    private double _phase;
    private bool _active;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsActive { get => _active; set { if (_active == value) return; _active = value; Invalidate(); } }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool Indeterminate { get; set; }
    [Browsable(false)] public double DisplayedRatio => _displayed;

    public Soft3DProgressBar() { Size = new(180, 14); AccessibleRole = AccessibleRole.ProgressBar; }
    public void SetProgress(double ratio, bool immediate = false)
    {
        _target = double.IsFinite(ratio) ? Math.Clamp(ratio, 0, 1) : 0;
        if (immediate) _displayed = _target;
        Invalidate();
    }
    protected override void Animate(double now)
    {
        if (!IsActive) return;
        if (Indeterminate) { _phase = (Math.Sin(now * 3) + 1) / 2; Invalidate(); }
        else if (Math.Abs(_target - _displayed) > .0001)
        {
            _displayed += (_target - _displayed) * .3;
            Invalidate();
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 6 || Height < 6) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var track = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = CaroTheme.GetRoundedPath(track, track.Height / 2);
        using var brush = new SolidBrush(CaroTheme.CardInnerBox);
        using var shadow = new Pen(Color.FromArgb(65, CaroTheme.WoodShadow));
        using var highlight = new Pen(CaroTheme.CardHighlight);
        g.FillPath(brush, path);
        g.DrawPath(shadow, path);
        g.DrawLine(highlight, track.Left + track.Height / 2, track.Bottom, track.Right - track.Height / 2, track.Bottom);
        if (!IsActive) return;
        int fillWidth = (int)(track.Width * (Indeterminate ? .28 : _displayed));
        if (fillWidth < 1) return;
        var fill = new Rectangle(track.X + (Indeterminate ? (int)((track.Width - fillWidth) * _phase) : 0),
            track.Y, fillWidth, track.Height);
        Color color = Indeterminate ? CaroTheme.ButtonNormal : _displayed >= .5
            ? CaroTheme.ButtonNormal : _displayed >= .2
                ? Blend(CaroTheme.VictoryCopper, CaroTheme.ButtonNormal, (_displayed - .2) / .3)
                : Blend(CaroTheme.ProgressCritical, CaroTheme.VictoryCopper, _displayed / .2);
        var state = g.Save();
        g.SetClip(path);
        using var fillPath = CaroTheme.GetRoundedPath(fill, Math.Min(fill.Width, fill.Height) / 2);
        using var gradient = new LinearGradientBrush(fill, Blend(color, CaroTheme.CardHighlight, .17), color, 90f);
        g.FillPath(gradient, fillPath);
        g.Restore(state);
    }
}

public sealed class SoftLoadingIndicator : Soft3DProgressBar
{
    public SoftLoadingIndicator() { Indeterminate = true; IsActive = true; AccessibleName = "Đang xử lý"; }
}

public sealed class MatchClockControl : Control
{
    private readonly Font _captionFont = new("Segoe UI", 8.5f, FontStyle.Bold);
    private readonly Font _timeFont = new("Segoe UI", 18f, FontStyle.Bold);
    private string _timeText = "00:00";
    [Browsable(false)] public string TimeText => _timeText;
    public MatchClockControl()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.ResizeRedraw, true);
        BackColor = Color.Transparent;
        Size = new(180, 64);
        AccessibleName = "THỜI GIAN VÁN";
    }
    public static string FormatElapsed(TimeSpan elapsed) => elapsed.TotalHours >= 1
        ? $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
        : $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}";
    public void SetElapsed(TimeSpan elapsed)
    {
        string text = FormatElapsed(elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed);
        if (_timeText == text) return;
        _timeText = text;
        AccessibleDescription = text;
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (Width < 12 || Height < 12) return;
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using var path = CaroTheme.GetRoundedPath(new(2, 2, Width - 5, Height - 5), Math.Min(12, Height / 3));
        using var bg = new SolidBrush(CaroTheme.Card);
        using var border = new Pen(CaroTheme.CardHighlight, 1.5f);
        g.FillPath(bg, path); g.DrawPath(border, path);

        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;
        int capH = (int)(Height * 0.35);
        TextRenderer.DrawText(g, "THỜI GIAN VÁN", _captionFont, new Rectangle(4, 2, Width - 8, capH), CaroTheme.TextMuted, flags);
        TextRenderer.DrawText(g, _timeText, _timeFont, new Rectangle(4, capH, Width - 8, Height - capH - 2), CaroTheme.TextDark, flags);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _captionFont.Dispose(); _timeFont.Dispose(); }
        base.Dispose(disposing);
    }
}

public sealed class TurnTransitionBanner : AnimatedStatusControl
{
    private readonly Font _titleFont = new("Segoe UI", 12f, FontStyle.Bold);
    private readonly Font _detailFont = new("Segoe UI", 9f);
    private double _started;
    private double _offset;
    private string _detail = "";
    [Browsable(false)] public int AnnouncementCount { get; private set; }
    public TurnTransitionBanner() { Size = new(380, 40); Visible = false; }
    public void Announce(string name, int symbol, bool local)
    {
        string role = $"PLAYER {symbol}";
        string display = string.IsNullOrWhiteSpace(name) ? role : name;
        Text = local ? "ĐẾN LƯỢT BẠN" : $"ĐẾN LƯỢT {display.ToUpperInvariant()}";
        _detail = $"{display} • {role} • {(symbol == 1 ? "X" : "O")}";
        AccessibleDescription = Text + ". " + _detail;
        _started = UiAnimationClock.Now;
        AnnouncementCount++;
        Visible = true;
        BringToFront();
        Animate(_started);
    }
    public void Dismiss() { Visible = false; }
    protected override void Animate(double now)
    {
        double elapsed = now - _started;
        if (elapsed >= 1.1) { Dismiss(); return; }
        double t = elapsed < .2 ? 1 - Math.Pow(1 - elapsed / .2, 3)
            : elapsed < .85 ? 1 : Math.Pow(1 - (elapsed - .85) / .25, 2);
        _offset = -Height * (1 - t);
        Invalidate();
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == 0x0084) { m.Result = new IntPtr(-1); return; } // HTTRANSPARENT
        base.WndProc(ref m);
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var rect = new Rectangle(2, (int)_offset + 2, Width - 5, Height - 5);
        if (rect.Width < 8 || rect.Height < 8) return;
        using var path = CaroTheme.GetRoundedPath(rect, Math.Min(12, rect.Height / 3));
        using var bg = new SolidBrush(CaroTheme.Card);
        using var border = new Pen(CaroTheme.WoodHighlight, 1.3f);
        g.FillPath(bg, path); g.DrawPath(border, path);
        var textRect = new Rectangle(rect.X + 10, rect.Y, rect.Width - 20, rect.Height);
        var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
        TextRenderer.DrawText(g, Text, _titleFont, textRect, CaroTheme.TextDark, flags);
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { _titleFont.Dispose(); _detailFont.Dispose(); }
        base.Dispose(disposing);
    }
}
