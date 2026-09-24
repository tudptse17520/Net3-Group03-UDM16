using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using CaroClient;
using CaroClient.Network;
using CaroClient.Settings;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

internal static class SocialVisualChecks
{
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, ref NativeRect bounds);
    [DllImport("user32.dll")] private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    private static void Call(object target, string method, params object?[] args) => target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(target, args);
    private static T Find<T>(Control parent, string name) where T : Control => (T)parent.Controls.Find(name, true).Single();
    internal static void Run()
    {
        typeof(NetworkClient).GetProperty("CurrentNickname")!.SetValue(NetworkClient.Instance, "Trung");
        using var avatar = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(avatar)) { g.Clear(Color.SaddleBrown); g.FillEllipse(Brushes.Bisque, 7, 7, 18, 18); }
        using var bytes = new MemoryStream(); avatar.Save(bytes, System.Drawing.Imaging.ImageFormat.Png);
        Call(AvatarManager.Instance, "HandleAvatarDataReceived", new AvatarDataEvent { PlayerId = "Tu Doan", AvatarVersion = 1, Base64Image = Convert.ToBase64String(bytes.ToArray()) });
        var oldContext = SetThreadDpiAwarenessContext(new IntPtr(-4));
        try
        {
            foreach (int dpi in new[] { 96, 120, 144 })
            {
                Render(new LoginForm(), dpi, "login", form =>
                {
                    Program.Assert(Find<RadioButton>(form, "LocalMode").Font.Bold && Find<RadioButton>(form, "RemoteMode").Font.Bold, "Connection modes must be bold.");
                });
                Render(new LobbyForm(), dpi, "lobby", form =>
                {
                    Call(form, "OnRoomListReceivedHandler", new List<RoomDto> { new() { RoomId = "ROOM-847883", IsSpectatorLocked = true } });
                    Program.Assert(Find<ListBox>(form, "LstRooms").Items[0] is RoomDto, "Room list stores structured models.");
                    var chat = Find<ChatPanel>(form, "LobbyChat");
                    chat.AddMessage(new() { MessageId = Guid.NewGuid(), SenderName = "Trung", Text = "Xin chào 👋 — room hiển thị đúng mã số." });
                    Program.Assert(chat.Top >= Find<Control>(form, "cardPlayers").Bottom, "Lobby chat must remain below original three columns.");
                    InBounds(form, chat);
                });
                Render(new GameBoardForm("ROOM-847883", 1, "ZOROO", Guid.NewGuid(), Program.Sample()), dpi, "player", form =>
                {
                    var board = (GameBoardForm)form;
                    var matchId = Program.Field<Guid>(board, "_currentMatchId");
                    var move = new MoveMadeEventDto { RoomId = "ROOM-847883", MatchIdentity = matchId, IsValid = true,
                        X = 3, Y = 4, PlayerId = NetworkClient.Instance.CurrentNickname, Timing = Program.Sample(2, 2, 2) };
                    Call(board, "HandleMoveMade", move);
                    Call(board, "HandleMoveMade", move);
                    var sound = Program.Field<MoveSound>(board, "_moveSound");
                    Program.Assert(sound.TriggerCount == 1, "Server move triggers one sound; duplicate echo is silent.");
                    Call(board, "HandleMoveMade", move with { IsValid = false, X = 8, Y = 8 });
                    Program.Assert(sound.TriggerCount == 1, "Invalid move is silent.");
                    foreach (var toast in board.Controls.OfType<ToastNotification>().ToArray()) toast.Dispose();
                    Call(board, "HandleMoveMade", move with { X = 5, Y = 6, PlayerId = "ZOROO", Timing = Program.Sample(4, 1, 3) });
                    Program.Assert(Program.Field<int>(board, "_lastAcceptedMoveRow") == 6 && Program.Field<int>(board, "_lastAcceptedMoveCol") == 5, "Last move follows authoritative opponent move.");
                    var presence = new RoomPresenceEvent { Room = SampleRoom() with { Revision = 1 }, JoinedPlayerName = "Tu Doan" };
                    Call(board, "SocialMessage", new NetworkMessage(MessageType.RoomPresenceEvent, JsonSerializer.SerializeToElement(presence)));
                    Find<ChatPanel>(form, "RoomChat").AddMessage(new() { MessageId = Guid.NewGuid(), SenderName = "ZOROO", Text = "Nước cờ đẹp 😊" });
                    CheckGameLayout(board);
                    // Close without a live server or leave confirmation.
                    typeof(GameBoardForm).GetField("_isClosingProgrammatically", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(board, true);
                });
                var state = new GameSessionDto { Board = Enumerable.Range(0, 15).Select(_ => new int[15]).ToArray(), MatchIdentity = Guid.NewGuid(),
                    CurrentTurn = 2, Status = "Playing", Timing = Program.Sample(3, 2), LastMoveX = 2, LastMoveY = 3 };
                state.Board[3][2] = 1;
                Render(new GameBoardForm(new SpectatorStateSnapshotDto { Room = SampleRoom(), Session = state }), dpi, "spectator", form =>
                {
                    var board = (GameBoardForm)form;
                    Program.Assert(!Find<Button>(form, "btnSurrender").Visible && !Find<Button>(form, "btnOfferDraw").Visible && !Find<Button>(form, "btnNewGame").Visible,
                        "Spectator must hide all gameplay actions.");
                    Program.Assert(Find<Button>(form, "btnExitMatch").Text == "THOÁT PHÒNG" && !Find<ChatPanel>(form, "RoomChat").Visible, "Spectator has one exit and no private chat.");
                    var cell = Find<Button>(form, "Cell_0_0"); cell.PerformClick();
                    Program.Assert(cell.Cursor == Cursors.Default && Program.Field<MoveSound>(board, "_moveSound").TriggerCount == 0, "Spectator click is read-only and silent.");
                    Program.Assert(Program.Field<int>(board, "_lastAcceptedMoveRow") == 3, "Spectator restores last move from snapshot.");
                    CheckGameLayout(board);
                });
                Console.WriteLine($"PASS: controlled DPI {dpi} ({dpi * 100 / 96}%) login/lobby/player/spectator renders, layout, sound, last move, read-only UI.");
            }
        }
        finally
        {
            SetThreadDpiAwarenessContext(oldContext);
            Call(AvatarManager.Instance, "HandleAvatarChanged", new AvatarChangedEvent { PlayerId = "Tu Doan", AvatarVersion = 2, HasAvatar = false });
        }
    }
    private static RoomDto SampleRoom() => new() { RoomId = "ROOM-847883", PlayerX = new() { PlayerName = "Trung" },
        PlayerO = new() { PlayerName = "ZOROO" }, SpectatorCount = 2, Spectators = [new() { PlayerName = "Tu Doan", HasAvatar = true, AvatarVersion = 1 }, new() { PlayerName = "Nam" }] };
    private static void CheckGameLayout(GameBoardForm board)
    {
        var frame = Find<Control>(board, "pnlWoodFrame");
        foreach (string name in new[] { "SpectatorStrip", "RoomChat", "SpectatorLock", "SpectatorNotice" })
        {
            var control = Find<Control>(board, name);
            if (!control.Visible) continue;
            InBounds(board, control);
            Program.Assert(!control.Bounds.IntersectsWith(frame.Bounds), $"{name} overlaps board.");
            Program.Assert(!control.Bounds.IntersectsWith(Find<Control>(board, "pnlPlayer1").Bounds) &&
                !control.Bounds.IntersectsWith(Find<Control>(board, "pnlPlayer2").Bounds), $"{name} overlaps player card.");
        }
        Program.Assert(!Find<Control>(board, "SpectatorStrip").Bounds.IntersectsWith(Find<Control>(board, "btnExitMatch").Bounds), "Spectators overlap action row.");
        var chip = Find<Control>(board, "Spectator_Tu Doan");
        Program.Assert(chip.GetType().GetField("_image", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(chip) is Image, "Spectator avatar reuses cached image clone.");
    }
    private static void InBounds(Form form, Control control) => Program.Assert(form.ClientRectangle.Contains(control.Bounds), $"{control.Name} clipped: {control.Bounds} outside {form.ClientRectangle}");
    private static void Render(Form form, int dpi, string name, Action<Form> verify)
    {
        using (form)
        {
            Exception? failure = null;
            form.Shown += (_, _) =>
            {
                try
                {
                    double scale = (double)dpi / form.DeviceDpi;
                    var bounds = new NativeRect { Left = form.Left, Top = form.Top, Right = form.Left + (int)(form.Width * scale), Bottom = form.Top + (int)(form.Height * scale) };
                    SendMessage(form.Handle, 0x02E0, new IntPtr(dpi | dpi << 16), ref bounds);
                    Program.Assert(form.DeviceDpi == dpi, $"DPI transition was not applied: expected {dpi}, got {form.DeviceDpi}.");
                    form.PerformLayout();
                    if (form is GameBoardForm) Call(form, "CenterLayout");
                    verify(form);
                    if (form is GameBoardForm) Call(form, "PresentationTick", Environment.TickCount64 / 1000d);
                    string output = Path.Combine(AppContext.BaseDirectory, "screenshots", $"social-{name}-{dpi}.png");
                    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                    using var bitmap = new Bitmap(form.Width, form.Height);
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Png);
                }
                catch (Exception ex) { failure = ex; }
                finally
                {
                    if (form is GameBoardForm) typeof(GameBoardForm).GetField("_isClosingProgrammatically", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(form, true);
                    form.BeginInvoke(form.Close);
                }
            };
            form.ShowDialog();
            if (failure != null) throw failure;
        }
    }
}
