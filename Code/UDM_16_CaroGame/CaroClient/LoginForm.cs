using CaroClient.Network;

namespace CaroClient;

public partial class LoginForm : CaroForm
{
    private readonly SoftLoadingIndicator _connectingIndicator = new() { Visible = false, Name = "connectingIndicator" };
    private RadioButton _localMode = null!;
    private RadioButton _remoteMode = null!;
    private Label _modeHint = null!;
    private Label _connectionStatus = null!;
    private string _remoteHost = "";
    private int _localPort = 8888;
    private bool _busy;
    public string PlayerName { get; private set; } = "";
    public string ServerIp { get; private set; } = "";
    public int ServerPort { get; private set; }

    public LoginForm(ServerConfiguration configuration) : this()
    {
        _localPort = configuration.UsesLocalServer ? configuration.Port : 8888;
        _remoteHost = configuration.UsesLocalServer ? "" : configuration.Host;
        TxtServerIp.Text = configuration.Host;
        TxtPort.Text = configuration.Port.ToString();
        _localMode.Checked = configuration.UsesLocalServer;
        _remoteMode.Checked = !configuration.UsesLocalServer;
        UpdateConnectionMode();
    }
    public LoginForm()
    {
        InitializeComponent();
        DoubleBuffered = true;
        TxtNickname.MaxLength = 64;
        foreach (var pair in new[] { (TxtNickname, pnlNickname), (TxtServerIp, pnlServerIp), (TxtPort, pnlPort) })
        {
            pair.Item1.GotFocus += (_, _) => { pair.Item2.IsFocused = true; pair.Item2.Invalidate(); };
            pair.Item1.LostFocus += (_, _) => { pair.Item2.IsFocused = false; pair.Item2.Invalidate(); };
        }
        _localMode.CheckedChanged += (_, _) => UpdateConnectionMode();
        UpdateConnectionMode();
    }
    private void BuildConnectionModeControls()
    {
        _localMode = new RadioButton { Name = "LocalMode", Text = "Chơi trên máy này", Checked = true, BackColor = CaroTheme.Card, ForeColor = CaroTheme.TextDark };
        _remoteMode = new RadioButton { Name = "RemoteMode", Text = "Kết nối máy chủ", BackColor = CaroTheme.Card, ForeColor = CaroTheme.TextDark };
        _localMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _remoteMode.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _modeHint = new Label { Name = "ModeHint", ForeColor = CaroTheme.TextMuted, BackColor = CaroTheme.Card };
        _connectionStatus = new Label { Name = "ConnectionStatus", TextAlign = ContentAlignment.MiddleCenter, ForeColor = CaroTheme.TextDark, BackColor = CaroTheme.Card, AutoEllipsis = true };
        _localMode.SetBounds(25, 100, 390, 28);
        _remoteMode.SetBounds(25, 136, 390, 28);
        _modeHint.SetBounds(25, 177, 390, 52);
        _connectingIndicator.SetBounds(95, 380, 250, 10);
        _connectionStatus.SetBounds(25, 397, 390, 52);
        pnlCard.Controls.AddRange([_localMode, _remoteMode, _modeHint, _connectingIndicator, _connectionStatus]);
    }
    private void UpdateConnectionMode()
    {
        if (_localMode.Checked)
        {
            if (TxtServerIp.Enabled && !ServerConfiguration.IsLoopbackHost(TxtServerIp.Text)) _remoteHost = TxtServerIp.Text;
            TxtServerIp.Text = "127.0.0.1";
            TxtPort.Text = _localPort.ToString();
            _modeHint.Text = "Dành cho hai cửa sổ trên cùng máy. Muốn chơi với máy khác, hãy cùng kết nối một máy chủ.";
        }
        else
        {
            TxtServerIp.Text = _remoteHost;
            _modeHint.Text = "Hai người cần nhập cùng địa chỉ máy chủ. Không dùng 127.0.0.1 để kết nối sang máy khác.";
        }
        TxtServerIp.Enabled = TxtPort.Enabled = !_localMode.Checked && !_busy;
        _connectionStatus.Text = "";
    }
    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new SolidBrush(CaroTheme.Background);
        e.Graphics.FillRectangle(brush, e.ClipRectangle);
    }
    private void SetBusy(bool busy)
    {
        _busy = busy;
        BtnConnect.Enabled = _localMode.Enabled = _remoteMode.Enabled = !busy;
        TxtNickname.Enabled = !busy;
        TxtServerIp.Enabled = TxtPort.Enabled = !busy && !_localMode.Checked;
        BtnConnect.Text = busy ? "ĐANG KẾT NỐI..." : "VÀO GAME";
        _connectingIndicator.Visible = busy;
    }
    private async void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (_busy) return;
        PlayerName = TxtNickname.Text.Trim();
        if (PlayerName.Length == 0 || PlayerName.Any(char.IsControl))
        { _connectionStatus.Text = "Vui lòng nhập tên người chơi hợp lệ."; TxtNickname.Focus(); return; }
        if (!int.TryParse(TxtPort.Text, out int port)) port = 0;
        var selected = new ServerConfiguration
        {
            Host = _localMode.Checked ? "127.0.0.1" : TxtServerIp.Text.Trim(),
            Port = port, AutoStartLocalServer = _localMode.Checked
        };
        try { selected.Validate(); }
        catch (InvalidDataException ex) { _connectionStatus.Text = ex.Message; return; }
        ServerIp = selected.Host; ServerPort = selected.Port;
        ClientLog.Write($"Selected mode={(selected.UsesLocalServer ? "Local" : "Remote")}; host={ServerIp}; port={ServerPort}");
        SetBusy(true);
        _connectionStatus.Text = "Đang kết nối...";
        var network = NetworkClient.Instance;
        var acknowledged = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        void Closed(object? s, FormClosedEventArgs e) => cancellation.Cancel();
        void Result(bool ok, string message) => acknowledged.TrySetResult(ok);
        void Error(Exception error) => acknowledged.TrySetException(error);
        void Disconnected() => acknowledged.TrySetException(new IOException("Kết nối bị ngắt."));
        FormClosed += Closed;
        network.OnConnectResult += Result;
        network.OnError += Error;
        bool enteredLobby = false;
        try
        {
            await LocalServerRuntime.EnsureReadyAsync(selected, cancellation.Token);
            if (!await network.ConnectAsync(ServerIp, ServerPort, cancellation.Token))
            { _connectionStatus.Text = network.LastConnectionError; return; }
            network.OnDisconnected += Disconnected;
            _connectionStatus.Text = "Đang xác thực...";
            await network.SendLoginAsync(PlayerName);
            if (!await acknowledged.Task.WaitAsync(TimeSpan.FromSeconds(12), cancellation.Token)) throw new IOException();
            cancellation.Token.ThrowIfCancellationRequested();
            var lobby = new LobbyForm(PlayerName);
            lobby.FormClosed += (_, _) => Close();
            enteredLobby = true;
            Hide(); lobby.Show();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception ex)
        {
            ClientLog.Write($"Login failed: {ex.GetType().Name}: {ex.Message}");
            if (!IsDisposed) _connectionStatus.Text = ConnectionDiagnostics.Explain(ex, $"{ServerIp}:{ServerPort}");
        }
        finally
        {
            FormClosed -= Closed;
            network.OnConnectResult -= Result;
            network.OnError -= Error;
            network.OnDisconnected -= Disconnected;
            if (!enteredLobby) network.Disconnect();
            if (!IsDisposed) SetBusy(false);
        }
    }
}
