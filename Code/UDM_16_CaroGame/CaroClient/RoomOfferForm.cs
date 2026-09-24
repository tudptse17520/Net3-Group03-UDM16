namespace CaroClient;

// Modeless, owned by the game/result window; placed in the free area above Player 2.
public sealed class RoomOfferForm : CaroForm
{
    private readonly Soft3DPanel _panel = new() { Dock = DockStyle.Fill, CornerRadius = 14 };
    private readonly Label _title = new() { Text = "Ván mới", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, ForeColor = CaroTheme.TextDark };
    private readonly Label _message = new() { Name = "NewGameMessage", TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent, ForeColor = CaroTheme.TextDark };
    private readonly PillButton _yes = new() { Name = "NewGameAccept", Text = "ĐỒNG Ý", FitTextToWidth = true };
    private readonly PillButton _no = new() { Name = "NewGameDecline", Text = "TỪ CHỐI", FitTextToWidth = true, IsSecondary = true };
    public RoomOfferForm(string requester)
    {
        Name = "NewGamePrompt"; Text = "Ván mới"; AutoScaleMode = AutoScaleMode.Dpi; AutoScaleDimensions = new SizeF(96, 96);
        FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual; ShowInTaskbar = false;
        BackColor = CaroTheme.Background;
        _title.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        _message.Font = new Font("Segoe UI", 9);
        _message.Text = $"{requester} muốn bắt đầu ván mới.\nChỉ đặt lại khi bạn đồng ý.";
        _panel.Controls.AddRange([_title, _message, _yes, _no]); Controls.Add(_panel);
        _yes.Click += (_, _) => { DialogResult = DialogResult.Yes; Close(); };
        _no.Click += (_, _) => { DialogResult = DialogResult.No; Close(); };
        ClientSize = new Size(260, 160);
    }
    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (_yes == null) return;
        float s = DeviceDpi / 96f; int pad = (int)(12 * s), title = (int)(28 * s), button = (int)(32 * s);
        _title.SetBounds(pad, pad, ClientSize.Width - 2 * pad, title);
        _message.SetBounds(pad, _title.Bottom + 2, ClientSize.Width - 2 * pad, Math.Max(20, ClientSize.Height - title - button - 4 * pad));
        int width = (ClientSize.Width - 3 * pad) / 2;
        _yes.SetBounds(pad, ClientSize.Height - pad - button, width, button);
        _no.SetBounds(_yes.Right + pad, _yes.Top, width, button);
    }
}
