using System.Drawing.Imaging;
using System.Reflection;
using System.Text.Json;
using CaroClient;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

internal static class Program
{
    internal static readonly DateTime Start = new(2026, 9, 22, 12, 0, 0, DateTimeKind.Utc);
    internal static GameTimingDto Sample(double elapsed = 0, int turn = 1, int number = 1) => new()
    {
        MatchStartedAtUtc = Start, ServerNowUtc = Start.AddSeconds(elapsed),
        TurnDeadlineUtc = Start.AddSeconds(elapsed + 30), TurnDurationSeconds = 30,
        RemainingTimeSeconds = 30, CurrentTurn = turn, TurnNumber = number
    };
    internal static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--visual-only")) { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); SocialVisualChecks.Run(); return 0; }
            TestModel();
            NetworkChecks.Run().GetAwaiter().GetResult();
            SocialChecks.Run().GetAwaiter().GetResult();
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            TestStartupVisuals();
            LoginFlowChecks.Run();
            TestUi();
            SocialVisualChecks.Run();
            Console.WriteLine("PASS: all progress system checks.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void TestStartupVisuals()
    {
        using var splash = new CaroSplashForm();
        splash.Shown += (_, _) =>
        {
            splash.Report("Đang chuẩn bị máy chủ cục bộ...", .65);
            Save(splash, "splash");
            Assert(splash.Icon != null && splash.Text == "C A R O", "Splash branding.");
            splash.BeginInvoke(splash.Close);
        };
        splash.ShowDialog();
        using var login = new LoginForm(new ServerConfiguration { Host = "caro.example.net", Port = 9000, AutoStartLocalServer = false });
        login.Shown += (_, _) =>
        {
            Assert(Find<TextBox>(login, "TxtServerIp").Text == "caro.example.net", "Configurable remote host.");
            Assert(Find<TextBox>(login, "TxtPort").Text == "9000", "Configurable remote port.");
            Save(login, "login");
            login.BeginInvoke(login.Close);
        };
        login.ShowDialog();
        Console.WriteLine("PASS: splash icon, branding, rendering and remote settings in login.");
    }
    private static void TestModel()
    {
        double now = 0;
        var state = new GamePresentationState(() => now);
        Guid match = Guid.NewGuid();
        Assert(!state.Running, "Clock must not start in the lobby.");
        Assert(state.Begin(match, Sample()), "First turn must announce once.");
        now = 7;
        Assert(state.Elapsed.TotalSeconds == 7 && state.Remaining == 23, "Separate clocks must advance from server time.");
        Assert(state.Apply(Sample(7, 2, 2)), "Turn 2 must activate.");
        Assert(state.Elapsed.TotalSeconds == 7 && state.Remaining == 30, "Turn reset must preserve match time.");
        now = 10;
        Assert(!state.Apply(Sample(7, 2, 2)) && state.Remaining == 27, "Duplicate sample must not rewind either clock.");
        state.Pause(); now = 14;
        Assert(state.Remaining == 27 && state.Elapsed.TotalSeconds == 14, "Reconnect pauses turn but not elapsed match time.");
        Assert(!state.Apply(Sample(14, 2, 2) with { RemainingTimeSeconds = 27, TurnDeadlineUtc = Start.AddSeconds(41) }), "Same turn restore must not announce.");
        now = 50;
        Assert(state.Remaining == 0 && !state.Terminal, "Only server may declare timeout.");
        state.Freeze(Sample(50, 2, 2) with { MatchEndedAtUtc = Start.AddSeconds(49) });
        now = 60;
        Assert(state.Elapsed.TotalSeconds == 49 && state.Terminal, "Authoritative final elapsed must freeze.");
        Assert(!state.Apply(Sample(60, 1, 3)), "Late turn events must not restart terminal UI.");
        state.Begin(Guid.NewGuid(), Sample());
        Assert(state.Elapsed == TimeSpan.Zero && state.Remaining == 30, "Rematch must reset both clocks.");
        Assert(MatchClockControl.FormatElapsed(TimeSpan.FromSeconds(3822)) == "01:03:42", "Hour formatting.");
        Console.WriteLine("PASS: model start, turn reset, duplicates, pause/resume, timeout authority, freeze, rematch and hour formatting.");
    }
    private static void TestUi()
    {
        using var board = new GameBoardForm("test-room", 2, "NguyễnHoàngMinhTrung123456789", Guid.NewGuid(), Sample());
        Exception? failure = null;
        ThreadExceptionEventHandler onError = (_, e) => { failure = e.Exception; board.Close(); };
        Application.ThreadException += onError;
        board.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); // Let GameBoardForm.OnShown finish its initial announcement.
                var banner = Find<TurnTransitionBanner>(board, "turnBanner");
                var clock = Find<MatchClockControl>(board, "matchClock");
                Assert(Find<Label>(board, "lblPlayer1Id").Text == "PLAYER 1", "Role 1 must replace ID label.");
                Assert(Find<Label>(board, "lblPlayer2Id").Text == "PLAYER 2", "Role 2 must replace ID label.");
                Assert(Find<Label>(board, "lblPlayer1Name").Text == "NguyễnHoàngMinhTrung123456789", "Preserve nickname.");
                Assert(Find<Soft3DProgressBar>(board, "prgPlayer1Timer").IsActive, "X must be active even when local player is O.");
                Assert(banner.AnnouncementCount == 1 && !banner.Text.Contains("LƯỢT BẠN"), "Initial banner must name the opponent.");
                await Task.Delay(260);
                Save(board, "default-long-name");
                CheckLayout(board);
                board.Size = board.MinimumSize;
                await Task.Delay(100); CheckLayout(board); Save(board, "minimum");
                board.WindowState = FormWindowState.Maximized;
                await Task.Delay(100); CheckLayout(board); Save(board, "maximized");
                board.WindowState = FormWindowState.Normal;
                await Task.Delay(100);
                int statsPaints = 0;
                Find<Control>(board, "pnlPlayer1Stats").Paint += (_, _) => statsPaints++;
                await Task.Delay(1200);
                Assert(statsPaints == 0, $"Countdown repainted unrelated statistics {statsPaints} times.");
                var matchId = Field<Guid>(board, "_currentMatchId");
                var move = new MoveMadeEventDto { RoomId = "test-room", MatchIdentity = matchId, PlayerId = "NguyễnHoàngMinhTrung123456789", X = 3, Y = 4, IsValid = true, Timing = Sample(7, 2, 2) };
                Call(board, "HandleMoveMade", move);
                Assert(banner.AnnouncementCount == 2 && banner.Text == "ĐẾN LƯỢT BẠN", "O must get local banner exactly once.");
                Call(board, "HandleMoveMade", move);
                Assert(banner.AnnouncementCount == 2, "Duplicate move must not animate.");
                await Task.Delay(100);
                Assert(clock.TimeText == "00:07", $"Turn must not reset match clock: {clock.TimeText}; model={Field<GamePresentationState>(board, "_presentation").Elapsed}.");
                Call(board, "HandleDrawOfferResolved", new DrawOfferResolvedDto { MatchIdentity = matchId, Accepted = false, Message = "Từ chối" });
                Assert(Field<GamePresentationState>(board, "_presentation").Running, "Rejected draw keeps clock running.");
                var terminal = move with { Timing = Sample(15, 2, 2) with { MatchEndedAtUtc = Start.AddSeconds(15) }, WinnerSymbol = 0 };
                Call(board, "HandleDrawOfferResolved", new DrawOfferResolvedDto { MatchIdentity = matchId, Accepted = true });
                Call(board, "HandleGameOver", Message(MessageType.GameOverEvent, terminal));
                await Task.Delay(100);
                Assert(clock.TimeText == "00:15" && !banner.Visible, "Draw must freeze clock and hide banner.");
                var shell = Field<Form>(board, "_resultShellForm");
                Assert(Find<Button>(shell, "btnVanMoi").Enabled && Find<Button>(shell, "btnVeSanh").Enabled, "Result buttons must finalize.");
                var next = new NewGameEventDto { MatchIdentity = Guid.NewGuid(), RoomId = "test-room", Timing = Sample() };
                Call(board, "HandleMessageReceived", Message(MessageType.NewGameEvent, next));
                int count = banner.AnnouncementCount;
                Call(board, "HandleMessageReceived", Message(MessageType.NewGameEvent, next));
                Assert(banner.AnnouncementCount == count && clock.TimeText == "00:00", "Rematch and duplicate safety.");
                var state = new GameStateDto { Room = new RoomDto { RoomId = "test-room" }, Session = new GameSessionDto
                { MatchIdentity = next.MatchIdentity, Timing = Sample(5) with { IsPaused = true, TurnDeadlineUtc = null, RemainingTimeSeconds = 20 } },
                    ReconnectDeadlineUtc = Start.AddSeconds(50), ReconnectWindowSeconds = 60 };
                Call(board, "RestorePresentation", state);
                Assert(Field<GamePresentationState>(board, "_presentation").Paused && !banner.Visible, "Reconnect must pause turn and hide banner.");
                await Task.Delay(100); Save(board, "reconnect");
                Console.WriteLine($"PASS: player O UI, labels, long name, turns, duplicate moves, draw, rematch, reconnect, layouts. Host DPI={board.DeviceDpi}.");
            }
            catch (Exception error) { failure = error; }
            finally { SetField(board, "_isClosingProgrammatically", true); board.Close(); }
        };
        try { board.ShowDialog(); }
        finally { Application.ThreadException -= onError; }
        if (failure != null) throw failure;
    }
    private static void CheckLayout(GameBoardForm board)
    {
        var frame = Find<Control>(board, "pnlWoodFrame");
        Assert(!frame.Bounds.IntersectsWith(Find<Control>(board, "matchClock").Bounds), "Clock overlaps board.");
        Assert(!frame.Bounds.IntersectsWith(Find<Control>(board, "turnBanner").Bounds), "Banner overlaps cells.");
        foreach (int i in new[] { 1, 2 })
        {
            var card = Find<Control>(board, $"pnlPlayer{i}");
            Assert(!card.Bounds.IntersectsWith(frame.Bounds), "Player card overlaps board.");
            var progress = Find<Control>(card, $"prgPlayer{i}Timer");
            Assert(card.ClientRectangle.Contains(progress.Bounds), "Progress clipped by card.");
        }
    }
    internal static NetworkMessage Message(MessageType type, object payload) => new(type, JsonSerializer.SerializeToElement(payload));
    internal static T Find<T>(Control root, string name) where T : Control => (T)root.Controls.Find(name, true).Single();
    internal static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;
    private static void SetField(object obj, string name, object value) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(obj, value);
    private static void Call(object obj, string name, params object[] args) => obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(obj, args);
    private static void Save(Form form, string name)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "screenshots");
        Directory.CreateDirectory(directory);
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(Path.Combine(directory, name + ".png"), ImageFormat.Png);
    }
}
