using CaroShared.Contracts;

namespace CaroClient;

public sealed class ChatPanel : Soft3DPanel
{
    private readonly Label _title = new() { ForeColor = CaroTheme.TextDark, BackColor = Color.Transparent };
    private readonly RichTextBox _history = new() { Name = "ChatHistory", ReadOnly = true, DetectUrls = false, BorderStyle = BorderStyle.None, BackColor = CaroTheme.Background, ForeColor = CaroTheme.TextDark, TabStop = false };
    private readonly TextBox _input = new() { Name = "ChatInput", MaxLength = 500, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Nhập tin nhắn..." };
    private readonly PillButton _send = new() { Name = "ChatSend", Text = "GỬI" };
    private readonly Queue<ChatEvent> _messages = new();
    public event Func<string, Task>? SendRequested;
    public ChatPanel(string title)
    {
        CornerRadius = 14;
        _title.Text = title;
        _title.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        Font = new Font("Segoe UI", 9);
        _send.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        Controls.AddRange([_title, _history, _input, _send]);
        _send.Click += async (_, _) => await SendAsync();
        _input.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SendAsync(); } };
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_send == null) return;
        float s = DeviceDpi / 96f;
        int p = (int)(10 * s), h = (int)(30 * s), t = (int)(22 * s), w = (int)(62 * s);
        _title.SetBounds(p, p, Width - 2 * p, t);
        _history.SetBounds(p, p + t, Width - 2 * p, Math.Max(20, Height - 3 * p - t - h));
        _input.SetBounds(p, Height - p - h + (h - _input.PreferredHeight) / 2, Math.Max(35, Width - 3 * p - w), _input.PreferredHeight);
        _send.SetBounds(Width - p - w, Height - p - h, w, h);
    }
    private async Task SendAsync()
    {
        string text = _input.Text.Trim();
        if (text.Length == 0 || !_send.Enabled) return;
        _send.Enabled = false;
        try { if (SendRequested != null) await SendRequested(text); if (!IsDisposed) _input.Clear(); }
        catch (Exception) { if (!IsDisposed) _title.Text = "Mất kết nối — hãy thử lại"; }
        finally { if (!IsDisposed) _send.Enabled = true; }
    }
    public void AddMessage(ChatEvent message)
    {
        if (_messages.Any(m => m.MessageId == message.MessageId)) return;
        _messages.Enqueue(message);
        if (_messages.Count > 100)
        {
            _messages.Dequeue();
            _history.Text = string.Join(Environment.NewLine, _messages.Select(m => $"{m.SenderName}: {m.Text}")) + Environment.NewLine;
        }
        else _history.AppendText($"{message.SenderName}: {message.Text}{Environment.NewLine}");
        _history.SelectionStart = _history.TextLength;
        _history.ScrollToCaret();
    }
}
