namespace CaroClient;

internal sealed class CaroApplicationContext : ApplicationContext
{
    private readonly CaroSplashForm _splash = new();
    private readonly CancellationTokenSource _lifetime = new();
    private ServerConfiguration _config = new();
    private bool _starting;
    private bool _disposed;
    public CaroApplicationContext()
    {
        MainForm = _splash;
        _splash.Shown += async (_, _) => await InitializeAsync();
        _splash.RetryRequested += async (_, _) => await InitializeAsync();
        _splash.ConfigureRequested += (_, _) => OpenLogin();
        _splash.FormClosed += (_, _) => _lifetime.Cancel();
        _splash.Show();
    }
    private async Task InitializeAsync()
    {
        if (_starting) return;
        _starting = true;
        try
        {
            _splash.Report("Đang đọc thiết lập...", .15);
            _config = await ServerConfiguration.LoadAsync(_lifetime.Token);
            _splash.Report("Đang chuẩn bị giao diện...", .4);
            _ = Settings.PersonalizationManager.Instance.Settings;
            // Wait for the user's Local/Remote selection before launching anything.
            _splash.Report("Đang chuẩn bị màn hình đăng nhập...", .85);
            if (_lifetime.IsCancellationRequested) return;
            _splash.Report("Sẵn sàng chơi Caro", 1);
            OpenLogin();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(ex);
            if (!_splash.IsDisposed) _splash.ShowFailure(ex is TimeoutException or FileNotFoundException or InvalidDataException
                ? ex.Message : "Chưa thể khởi động. Bạn có thể thử lại hoặc chọn máy chủ khác.");
        }
        finally { _starting = false; }
    }
    private void OpenLogin()
    {
        var login = new LoginForm(_config);
        MainForm = login;
        login.Show();
        _splash.Close();
        _splash.Dispose();
    }
    protected override void Dispose(bool disposing)
    {
        // Application.Run disposes its context; the Program using scope may dispose
        // it again. CancellationTokenSource.Cancel must never run after Dispose.
        if (disposing && !_disposed)
        {
            _disposed = true;
            _lifetime.Cancel();
            _splash.Dispose();
            _lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
