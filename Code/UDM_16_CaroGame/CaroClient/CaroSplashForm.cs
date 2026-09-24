namespace CaroClient;

public sealed class CaroSplashForm : CaroForm
{
    private readonly PictureBox _image = new() { SizeMode = PictureBoxSizeMode.Zoom };
    private readonly Label _status = new() { TextAlign = ContentAlignment.MiddleCenter, AutoEllipsis = true };
    private readonly Soft3DProgressBar _progress = new() { IsActive = true };
    private readonly PillButton _retry = new() { Text = "THỬ LẠI", Visible = false };
    private readonly PillButton _configure = new() { Text = "CHỌN MÁY CHỦ", Visible = false };
    public event EventHandler? RetryRequested;
    public event EventHandler? ConfigureRequested;
    public CaroSplashForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new(460, 440);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = CaroTheme.Background;
        DoubleBuffered = true;
        _image.Image = CaroBranding.CreateImage();
        _image.SetBounds(140, 30, 180, 180);
        var title = new Label { Text = CaroBranding.DisplayName, Font = new("Segoe UI", 25, FontStyle.Bold),
            ForeColor = CaroTheme.TextDark, TextAlign = ContentAlignment.MiddleCenter };
        title.SetBounds(30, 222, 400, 50);
        _status.ForeColor = CaroTheme.TextMuted;
        _status.SetBounds(25, 285, 410, 46);
        _progress.SetBounds(60, 340, 340, 16);
        _retry.SetBounds(60, 375, 150, 36);
        _configure.SetBounds(220, 375, 180, 36);
        _retry.Click += (_, e) => RetryRequested?.Invoke(this, e);
        _configure.Click += (_, e) => ConfigureRequested?.Invoke(this, e);
        Controls.AddRange([_image, title, _status, _progress, _retry, _configure]);
    }
    public void Report(string status, double fraction)
    {
        _status.Text = status;
        _progress.IsActive = true;
        _progress.SetProgress(fraction, true);
        _retry.Visible = _configure.Visible = false;
    }
    public void ShowFailure(string message)
    {
        _status.Text = message;
        _progress.IsActive = false;
        _retry.Visible = _configure.Visible = true;
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) _image.Image?.Dispose();
        base.Dispose(disposing);
    }
}
