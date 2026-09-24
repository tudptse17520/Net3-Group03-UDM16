using System.Text.Json;
using CaroClient.Network;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

namespace CaroClient;

public partial class GameBoardForm
{
    private readonly ChatPanel _roomChat = new("CHAT VỚI ĐỐI THỦ") { Name = "RoomChat" };
    private readonly SpectatorStrip _spectators = new() { Name = "SpectatorStrip" };
    private readonly Label _roomCode = new() { Name = "RoomCode", ForeColor = CaroTheme.TextMuted, TextAlign = ContentAlignment.MiddleCenter };
    private readonly PillButton _spectatorLock = new() { Name = "SpectatorLock", Text = "KHÁN GIẢ: MỞ" };
    private readonly Label _spectatorNotice = new() { Name = "SpectatorNotice", ForeColor = CaroTheme.TextDark, AutoEllipsis = true, Visible = false };
    private readonly System.Windows.Forms.Timer _noticeTimer = new() { Interval = 2800 };
    private readonly MoveSound _moveSound = new();
    private bool _spectatorLocked;
    private long _presenceRevision = -1;
    private Guid? _newGameOffer;
    private CaroDialogForm? _newGameDialog;
    private bool _dismissNewGameDialog;
    private bool _socialDisposed;

    private void InitializeSocialUi()
    {
        foreach (var button in new[] { btnSurrender, btnOfferDraw, btnNewGame, btnExitMatch }) button.FitTextToWidth = true;
        Controls.AddRange([_roomChat, _spectators, _roomCode, _spectatorLock, _spectatorNotice]);
        _spectators.SetPeople([]);
        _roomChat.SendRequested += text => NetworkClient.Instance.SendMessageAsync(new(MessageType.RoomChatRequest,
            new ChatRequest { RoomId = _roomId, Text = text }));
        _spectatorLock.Click += async (_, _) =>
        {
            if (_isSpectator) return;
            _spectatorLock.Enabled = false;
            await SendSocialAsync(MessageType.SetSpectatorLockRequest, new RoomAccessRequest { RoomId = _roomId, IsLocked = !_spectatorLocked });
        };
        _noticeTimer.Tick += (_, _) => { _noticeTimer.Stop(); _spectatorNotice.Visible = false; };
        NetworkClient.Instance.OnMessageReceived += SocialMessage;
        Shown += async (_, _) =>
        {
            LayoutSocialUi(DeviceDpi / 96f);
            if (NetworkClient.Instance.IsConnected && _roomId.Length > 0)
                await SendSocialAsync(MessageType.RoomPresenceRequest, new RoomAccessRequest { RoomId = _roomId });
        };
        FormClosed += (_, _) => DisposeSocialUi();
    }
    private void DisposeSocialUi()
    {
        if (_socialDisposed) return;
        _socialDisposed = true;
        NetworkClient.Instance.OnMessageReceived -= SocialMessage;
        _noticeTimer.Dispose(); _moveSound.Dispose(); CloseNewGameDialog();
    }
    private async Task SendSocialAsync(MessageType type, object payload)
    {
        try { await NetworkClient.Instance.SendMessageAsync(new(type, payload)); }
        catch (Exception)
        {
            if (!IsDisposed) { _spectatorLock.Enabled = !_isSpectator; ShowSocialNotice("Mất kết nối, vui lòng thử lại."); }
        }
    }
    private void LayoutSocialUi(float scale)
    {
        if (_roomChat.Parent == null) return;
        int gap = (int)(8 * scale);
        _roomCode.Text = RoomCodes.Display(_roomId);
        _roomCode.SetBounds(pnlPlayer1.Left, _matchClock.Top, pnlPlayer1.Width, (int)(24 * scale));
        _spectatorLock.SetBounds(pnlPlayer1.Left, _roomCode.Bottom + gap, pnlPlayer1.Width, (int)(34 * scale));
        _spectatorNotice.SetBounds(pnlPlayer1.Left, _spectatorLock.Bottom + gap, pnlPlayer1.Width, (int)(44 * scale));
        _spectatorLock.Visible = !_isSpectator;
        if (_isSpectator)
        {
            btnSurrender.Visible = btnOfferDraw.Visible = btnNewGame.Visible = false;
            btnExitMatch.Text = "THOÁT PHÒNG"; btnExitMatch.GlyphIcon = "";
            btnExitMatch.Width = (int)(190 * scale);
            btnExitMatch.Left = (ClientSize.Width - btnExitMatch.Width) / 2;
        }
        _spectators.SetBounds(pnlWoodFrame.Left, btnExitMatch.Bottom + gap, pnlWoodFrame.Width, (int)(50 * scale));
        _roomChat.Visible = !_isSpectator;
        int chatTop = pnlPlayer2.Bottom + gap;
        int chatHeight = Math.Min((int)(175 * scale), ClientSize.Height - chatTop - gap);
        _roomChat.SetBounds(pnlPlayer2.Left, chatTop, pnlPlayer2.Width, Math.Max((int)(120 * scale), chatHeight));
        // Status stays in the header gap; it does not overlap the spectator row.
        _waitLabel.SetBounds(pnlWoodFrame.Left, pnlWoodFrame.Top - (int)(30 * scale), pnlWoodFrame.Width, (int)(22 * scale));
        _waitIndicator.SetBounds((ClientSize.Width - 180) / 2, _waitLabel.Bottom, 180, 6);
        _reconnectProgress.Bounds = _waitIndicator.Bounds;
    }
    private void ShowSocialNotice(string text)
    {
        _spectatorNotice.Text = text; _spectatorNotice.Visible = true;
        _sharedToolTip.SetToolTip(_spectatorNotice, text);
        _noticeTimer.Stop(); _noticeTimer.Start();
    }
    private void ApplyRoomPresence(RoomDto? room)
    {
        if (room == null || room.RoomId != _roomId || room.Revision < _presenceRevision) return;
        _presenceRevision = room.Revision;
        _spectatorLocked = room.IsSpectatorLocked;
        _spectatorLock.Text = _spectatorLocked ? "KHÁN GIẢ: KHÓA" : "KHÁN GIẢ: MỞ";
        _spectatorLock.Enabled = !_isSpectator;
        _spectators.SetPeople(room.Spectators);
    }
    private void SocialMessage(NetworkMessage message)
    {
        if (message.Payload is not JsonElement json) return;
        SafeInvoke(() =>
        {
            switch (message.Type)
            {
                case MessageType.RoomPresenceEvent:
                    var presence = json.Deserialize<RoomPresenceEvent>();
                    if (presence?.Room.RoomId != _roomId) return;
                    bool fresh = presence.Room.Revision > _presenceRevision;
                    ApplyRoomPresence(presence.Room);
                    if (fresh && presence.JoinedPlayerName is string name && name != NetworkClient.Instance.CurrentNickname)
                        ShowSocialNotice($"{name} đã vào xem trận đấu.");
                    break;
                case MessageType.RoomChatEvent:
                    var chat = json.Deserialize<ChatEvent>();
                    if (!_isSpectator && chat?.RoomId == _roomId) _roomChat.AddMessage(chat);
                    break;
                case MessageType.NewGameOfferEvent:
                    var offer = json.Deserialize<NewGameOfferDto>();
                    if (!_isSpectator && offer?.RoomId == _roomId && offer.MatchIdentity == _currentMatchId) ShowNewGameOffer(offer);
                    break;
                case MessageType.NewGameOfferResolvedEvent:
                    var resolved = json.Deserialize<NewGameOfferDto>();
                    if (resolved?.RoomId != _roomId || resolved.OfferIdentity != _newGameOffer) return;
                    _newGameOffer = null; CloseNewGameDialog();
                    if (!resolved.Accepted) { PresentationError(new IOException(resolved.Message)); ShowSocialNotice(resolved.Message); }
                    break;
            }
        });
    }
    private void ShowNewGameOffer(NewGameOfferDto offer)
    {
        if (_newGameOffer == offer.OfferIdentity) return;
        _newGameOffer = offer.OfferIdentity;
        SetWaiting("ĐANG CHỜ ĐỒNG Ý VÁN MỚI...", "rematch");
        btnNewGame.Enabled = false;
        if (_resultShellForm?.Controls.Find("btnVanMoi", true).FirstOrDefault() is Control button) button.Enabled = false;
        if (offer.RequesterName == NetworkClient.Instance.CurrentNickname) return;
        _newGameDialog = new CaroDialogForm($"{offer.RequesterName} muốn bắt đầu ván mới.\nVán hiện tại chỉ đặt lại khi bạn đồng ý.", "Ván mới", MessageBoxButtons.YesNo);
        var dialog = _newGameDialog;
        foreach (var action in DescendantButtons(dialog))
        {
            bool accept = action.DialogResult == DialogResult.Yes;
            action.Name = accept ? "NewGameAccept" : "NewGameDecline";
            action.Text = accept ? "ĐỒNG Ý" : "TỪ CHỐI";
            action.Click += (_, _) => dialog.Close();
        }
        dialog.FormClosed += async (_, _) =>
        {
            if (!_dismissNewGameDialog && _newGameOffer == offer.OfferIdentity)
                await SendSocialAsync(MessageType.NewGameResponseRequest, new NewGameResponseRequest
                { RoomId = _roomId, OfferIdentity = offer.OfferIdentity, Accept = dialog.DialogResult == DialogResult.Yes });
        };
        dialog.Show(_resultShellForm is { IsDisposed: false } ? _resultShellForm : this);
    }
    private static IEnumerable<Button> DescendantButtons(Control parent) => parent.Controls.Cast<Control>()
        .SelectMany(c => c is Button b ? new[] { b } : DescendantButtons(c));
    private void CloseNewGameDialog()
    {
        _dismissNewGameDialog = true;
        _newGameDialog?.Close(); _newGameDialog?.Dispose(); _newGameDialog = null;
        _dismissNewGameDialog = false;
    }
    private void SetLastMove(int row, int col, int symbol)
    {
        if (_lastAcceptedMoveRow >= 0 && _lastAcceptedMoveCol >= 0) _cells[_lastAcceptedMoveRow, _lastAcceptedMoveCol]?.Invalidate();
        _lastAcceptedMoveRow = row; _lastAcceptedMoveCol = col; _lastAcceptedMoveSymbol = symbol;
        if (row >= 0 && col >= 0) _cells[row, col]?.Invalidate();
    }
    private void RestoreLastMove(GameSessionDto? session)
    {
        if (session?.LastMoveX is int x && session.LastMoveY is int y && x >= 0 && x < BoardSize && y >= 0 && y < BoardSize)
            SetLastMove(y, x, _board[y][x]);
        else SetLastMove(-1, -1, 0);
        lblPlayer1MoveCount.Text = _board.Sum(row => row.Count(v => v == 1)).ToString();
        lblPlayer2MoveCount.Text = _board.Sum(row => row.Count(v => v == 2)).ToString();
    }
}
