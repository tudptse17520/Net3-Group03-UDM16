using System.Drawing.Drawing2D;

namespace CaroClient;

public sealed class RoomLockButton : Button
{
    private bool _locked;
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Locked { get => _locked; set { _locked = value; AccessibleName = value ? "Phòng đang khóa" : "Phòng đang mở"; Invalidate(); } }
    public RoomLockButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = CaroTheme.Background; Cursor = Cursors.Hand;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        DrawLock(e.Graphics, ClientRectangle, Locked, Enabled ? CaroTheme.ButtonNormal : CaroTheme.TextMuted);
        if (Focused) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -2, -2));
    }
    internal static void DrawLock(Graphics g, Rectangle bounds, bool locked, Color color)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        float unit = Math.Min(bounds.Width, bounds.Height) / 36f;
        float x = bounds.Left + (bounds.Width - 20 * unit) / 2, y = bounds.Top + (bounds.Height - 24 * unit) / 2;
        using var pen = new Pen(color, Math.Max(1.5f, 2 * unit)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawArc(pen, x + 5 * unit, y + 2 * unit, 10 * unit, 13 * unit, locked ? 180 : 205, locked ? 180 : 235);
        using var fill = new SolidBrush(color);
        g.FillRectangle(fill, x + 2 * unit, y + 12 * unit, 16 * unit, 11 * unit);
        using var key = new Pen(CaroTheme.Background, 2 * unit);
        g.DrawLine(key, x + 10 * unit, y + 16 * unit, x + 10 * unit, y + 19 * unit);
    }
}
