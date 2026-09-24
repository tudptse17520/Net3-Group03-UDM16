using CaroClient.Network;
using CaroShared.Contracts;

namespace CaroClient;

public partial class GameBoardForm
{
    private readonly GamePresentationState _presentation = new();
    private readonly MatchClockControl _matchClock = new() { Name = "matchClock" };
    private readonly TurnTransitionBanner _turnBanner = new() { Name = "turnBanner" };
    private readonly SoftLoadingIndicator _waitIndicator = new() { Visible = false };
    private readonly Soft3DProgressBar _reconnectProgress = new() { Visible = false, IsActive = true };
    private readonly Label _waitLabel = new() { AutoEllipsis = true, TextAlign = ContentAlignment.MiddleCenter, ForeColor = CaroTheme.TextMuted };
    private readonly PillButton _reconnectButton = new() { Text = "KẾT NỐI LẠI", Visible = false };
    private bool _turnUiRunning;
    private bool _announceOnShown;
    private double? _reconnectDeadline;
    private int _reconnectWindow;
    private string? _waitReason;
    private double _waitStarted;

    private void InitializeProgressUi()
    {
        lblPlayer1Id.Text = "PLAYER 1";
        lblPlayer2Id.Text = "PLAYER 2";
        Controls.AddRange([_matchClock, _turnBanner, _waitIndicator, _waitLabel, _reconnectProgress, _reconnectButton]);
        _reconnectButton.Click += async (_, _) =>
        {
            _reconnectButton.Enabled = false;
            SetWaiting("ĐANG KẾT NỐI LẠI...", "connection");
            try
            {
                if (!await NetworkClient.Instance.ReconnectToLastServerAsync())
                    SetWaiting("Không thể kết nối lại. Bạn có thể thử lại.", "connection");
            }
            catch (Exception) { SetWaiting("Không thể kết nối lại. Bạn có thể thử lại.", "connection"); }
            finally { if (!IsDisposed) _reconnectButton.Enabled = true; }
        };
        NetworkClient.Instance.OnGameStateRestored += RestorePresentation;
        NetworkClient.Instance.OnDisconnected += PresentationDisconnected;
        NetworkClient.Instance.OnReconnectResult += PresentationReconnectResult;
        NetworkClient.Instance.OnError += PresentationError;
        UiAnimationClock.Subscribe(PresentationTick);
    }

    private void DisposeProgressUi()
    {
        UiAnimationClock.Unsubscribe(PresentationTick);
        NetworkClient.Instance.OnGameStateRestored -= RestorePresentation;
        NetworkClient.Instance.OnDisconnected -= PresentationDisconnected;
        NetworkClient.Instance.OnReconnectResult -= PresentationReconnectResult;
        NetworkClient.Instance.OnError -= PresentationError;
        _turnBanner.Dismiss();
    }

    private void BeginPresentation(GameTimingDto? timing, int startingTurn = 1)
    {
        _turnBanner.Dismiss();
        _announceOnShown = false;
        bool changed = _presentation.Begin(_currentMatchId, timing, startingTurn);
        _turnUiRunning = !_presentation.Terminal;
        SetWaiting(null);
        btnNewGame.Enabled = !_isSpectator;
        UpdateTurnPresentation(changed);
        PresentationTick(UiAnimationClock.Now);
    }

    private void ApplyTiming(GameTimingDto? timing)
    {
        bool changed = _presentation.Apply(timing);
        _turnUiRunning = !_presentation.Terminal;
        UpdateTurnPresentation(changed);
        _matchClock.SetElapsed(_presentation.Elapsed);
    }

    private void UpdateTurnPresentation(bool announce)
    {
        int current = _presentation.CurrentTurn;
        for (int symbol = 1; symbol <= 2; symbol++)
        {
            var badge = symbol == 1 ? pnlPlayer1Turn : pnlPlayer2Turn;
            bool active = current == symbol && !_isGameOver;
            badge.Text = active ? (!_isSpectator && symbol == _mySymbol ? "● LƯỢT CỦA BẠN" : "● ĐANG ĐÁNH") : "ĐANG CHỜ";
            badge.Invalidate();
            var bar = symbol == 1 ? prgPlayer1Timer : prgPlayer2Timer;
            bar.IsActive = active;
            if (announce) bar.SetProgress(active ? _presentation.Remaining / _presentation.TurnDuration : 0, true);
        }
        if (announce && !_isGameOver && !_presentation.Terminal)
        {
            if (Visible) AnnounceTurn();
            else _announceOnShown = true;
        }
        UpdateTimerUI();
    }

    private void AnnounceTurn()
    {
        _announceOnShown = false;
        if (_isGameOver || _presentation.Terminal || _presentation.CurrentTurn is not (1 or 2)) return;
        int symbol = _presentation.CurrentTurn;
        _turnBanner.Announce(symbol == 1 ? lblPlayer1Name.Text : lblPlayer2Name.Text,
            symbol, !_isSpectator && symbol == _mySymbol);
    }

    private void FreezePresentation(GameTimingDto? timing = null)
    {
        _presentation.Freeze(timing);
        _turnUiRunning = false;
        _announceOnShown = false;
        _turnBanner.Dismiss();
        prgPlayer1Timer.IsActive = prgPlayer2Timer.IsActive = false;
        pnlPlayer1.TurnEmphasis = pnlPlayer2.TurnEmphasis = 0;
        _matchClock.SetElapsed(_presentation.Elapsed);
        _reconnectDeadline = null;
        _reconnectProgress.Visible = _reconnectButton.Visible = false;
        SetWaiting(null);
    }

    private void PresentationTick(double now)
    {
        if (IsDisposed || !Visible || WindowState == FormWindowState.Minimized) return;
        if (_waitReason == "rematch" && now - _waitStarted > 25)
            PresentationError(new TimeoutException("Chưa nhận được phản hồi tạo ván mới từ máy chủ."));
        _matchClock.SetElapsed(_presentation.Elapsed);
        if (_turnUiRunning && !_isGameOver)
        {
            int seconds = (int)Math.Ceiling(_presentation.Remaining);
            if (_remainingSeconds != seconds) { _remainingSeconds = seconds; UpdateTimerUI(); }
            var activeBar = _presentation.CurrentTurn == 1 ? prgPlayer1Timer : prgPlayer2Timer;
            activeBar.SetProgress(_presentation.Remaining / _presentation.TurnDuration);
        }
        for (int symbol = 1; symbol <= 2; symbol++)
        {
            var card = symbol == 1 ? pnlPlayer1 : pnlPlayer2;
            double target = !_isGameOver && !_presentation.Paused && _presentation.Running && _presentation.CurrentTurn == symbol ? 1 : 0;
            if (Math.Abs(card.TurnEmphasis - target) > .005)
            {
                card.TurnEmphasis += (target - card.TurnEmphasis) * .18;
                (symbol == 1 ? pnlPlayer1Turn : pnlPlayer2Turn).Invalidate();
            }
        }
        if (_reconnectDeadline is double deadline)
        {
            double left = Math.Max(0, deadline - now);
            _reconnectProgress.SetProgress(left / Math.Max(1, _reconnectWindow));
            string text = left > 0 ? $"CHỜ KẾT NỐI LẠI · Còn {Math.Ceiling(left):0} giây" : "ĐANG CHỜ XÁC NHẬN TỪ MÁY CHỦ...";
            if (_waitLabel.Text != text) _waitLabel.Text = text;
        }
    }

    private void LayoutProgressUi(float scale)
    {
        int headerOffset = _isSpectator ? 12 : 0;
        _matchClock.SetBounds((ClientSize.Width - (int)(180 * scale)) / 2, (int)((44 + headerOffset) * scale), (int)(180 * scale), (int)(64 * scale));
        _turnBanner.SetBounds((ClientSize.Width - (int)(380 * scale)) / 2, (int)((110 + headerOffset) * scale), (int)(380 * scale), (int)(40 * scale));
        for (int symbol = 1; symbol <= 2; symbol++)
        {
            var label = symbol == 1 ? lblPlayer1TimerPill : lblPlayer2TimerPill;
            var bar = symbol == 1 ? prgPlayer1Timer : prgPlayer2Timer;
            bar.SetBounds(label.Left, label.Bottom + 3, label.Width, Math.Max(10, (int)(12 * scale)));
        }
        int statusWidth = Math.Max(260, pnlWoodFrame.Width);
        _waitLabel.SetBounds((ClientSize.Width - statusWidth) / 2, btnExitMatch.Bottom + 3, statusWidth, (int)(22 * scale));
        _waitIndicator.SetBounds((ClientSize.Width - 180) / 2, _waitLabel.Bottom + 2, 180, 10);
        _reconnectProgress.Bounds = _waitIndicator.Bounds;
        _reconnectButton.SetBounds(pnlPlayer2.Left, _matchClock.Top, pnlPlayer2.Width, (int)(38 * scale));
    }

    private void SetWaiting(string? text, string? reason = null)
    {
        _waitReason = reason;
        _waitStarted = UiAnimationClock.Now;
        _waitLabel.Text = text ?? "";
        _waitLabel.Visible = _waitIndicator.Visible = !string.IsNullOrEmpty(text);
        if (reason == "rematch" && _resultShellForm?.Controls.Count > 0)
        {
            var label = _resultShellForm.Controls.Find("lblLoading", true).FirstOrDefault();
            if (label != null) { label.Text = text; label.Visible = true; }
            var loading = _resultShellForm.Controls.Find("resultLoading", true).FirstOrDefault();
            if (loading != null) loading.Visible = true;
        }
    }

    private void PresentationDisconnected() => SafeInvoke(() =>
    {
        if (_isGameOver) return;
        _presentation.Pause();
        _turnBanner.Dismiss();
        SetWaiting("MẤT KẾT NỐI · Chọn kết nối lại để tiếp tục", "connection");
        _reconnectButton.Visible = true;
        _reconnectButton.BringToFront();
    });

    private void PresentationReconnectResult(bool success, ReconnectResponse response) => SafeInvoke(() =>
    {
        if (!success) { SetWaiting(response.Message, "connection"); _waitIndicator.Visible = false; }
    });

    private void PresentationError(Exception error) => SafeInvoke(() =>
    {
        if (_waitReason is "draw" or "rematch")
        {
            SetWaiting(null);
            btnOfferDraw.Text = "HÒA";
            btnOfferDraw.Enabled = !_isGameOver;
            btnNewGame.Enabled = !_isSpectator;
            if (_resultShellForm?.Controls.Count > 0)
            {
                var button = _resultShellForm.Controls.Find("btnVanMoi", true).FirstOrDefault();
                if (button != null) button.Enabled = true;
                foreach (string name in new[] { "lblLoading", "resultLoading" })
                {
                    var loading = _resultShellForm.Controls.Find(name, true).FirstOrDefault();
                    if (loading != null) loading.Visible = false;
                }
            }
            ToastNotification.Show(this, error.Message, ToastType.Warning);
        }
    });

    private void RestorePresentation(GameStateDto state) => SafeInvoke(() =>
    {
        ApplyRoomPresence(state.Room);
        if (state.Session == null || (state.Room?.RoomId is { Length: > 0 } room && room != _roomId)) return;
        if (_isGameOver && state.Session.MatchIdentity == _currentMatchId) return;
        if (state.Session.Board.Length == BoardSize) UpdateBoard(state.Session.Board);
        RestoreLastMove(state.Session);
        if (_currentMatchId != state.Session.MatchIdentity)
        {
            _currentMatchId = state.Session.MatchIdentity;
            BeginPresentation(state.Session.Timing, state.Session.CurrentTurn);
        }
        else ApplyTiming(state.Session.Timing);
        _reconnectButton.Visible = false;
        SetWaiting(null);
        _reconnectDeadline = null;
        _reconnectProgress.Visible = false;
        if (state.ReconnectDeadlineUtc is DateTime deadline && state.Session.Timing is { } timing)
        {
            _reconnectWindow = state.ReconnectWindowSeconds;
            _reconnectDeadline = UiAnimationClock.Now + Math.Max(0, (deadline - timing.ServerNowUtc).TotalSeconds);
            _waitLabel.Visible = _reconnectProgress.Visible = true;
            _turnBanner.Dismiss();
        }
    });
}
