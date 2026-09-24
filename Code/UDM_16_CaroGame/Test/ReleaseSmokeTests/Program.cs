using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using CaroClient;

internal static class Program
{
    internal static void Assert(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    internal static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(obj)!;
    private static async Task<int> Main(string[] args)
    {
        string source = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/publish/Caro");
        string stage = args.Contains("--installed") ? source : Path.GetFullPath("artifacts/release-smoke/" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(stage);
        foreach (string file in args.Contains("--installed") ? Array.Empty<string>() : Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(stage, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start(); int port = ((IPEndPoint)reservation.LocalEndpoint).Port; reservation.Stop();
        string configPath = Path.Combine(stage, "server-config.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new ServerConfiguration { Port = port }));
        // This suite must never initialize or write the developer's database.
        await File.WriteAllTextAsync(Path.Combine(stage, "Server", "appsettings.json"), JsonSerializer.Serialize(new
        {
            ConnectionStrings = new { DefaultConnection = "Server=127.0.0.1,1;Database=CaroReleaseTest;User Id=test;Password=TestOnly!;Connect Timeout=1;Encrypt=False" }
        }));
        var processes = new List<Process>();
        try
        {
            var first = Start(Path.Combine(stage, "Caro.exe")); processes.Add(first);
            var second = Start(Path.Combine(stage, "Caro.exe")); processes.Add(second);
            var third = Start(Path.Combine(stage, "Caro.exe")); processes.Add(third);
            await WaitUntil(() => LoginReady(first) && LoginReady(second), "Both published apps must pass splash and show login.");
            Assert(Servers(stage).Count == 0, "Splash must not launch server before mode selection.");
            ReleaseUi.Login(first, "ReleaseUiA");
            ReleaseUi.Login(second, "ReleaseUiB");
            await WaitUntil(() => LoginReady(third), "Third published app must show login.");
            ReleaseUi.Login(third, "ReleaseViewer");
            await WaitUntil(() => ReleaseUi.InLobby(first) && ReleaseUi.InLobby(second), "Both published apps must authenticate into lobby.");
            await WaitUntil(() => ReleaseUi.Sees(first, "ReleaseUiB") && ReleaseUi.Sees(second, "ReleaseUiA"), "Both real lobby lists must show the opponent.");
            Assert(ReleaseUi.Find(first, "EndpointStatus")!.Current.Name.Contains(port.ToString()), "Lobby endpoint must match selected server.");
            using var ready = EventWaitHandle.OpenExisting($"Local\\Caro.Ready.{port}");
            Assert(ready.WaitOne(0), "Server readiness signal.");
            Assert(Servers(stage).Count == 1, "Concurrent app launches must start exactly one server.");
            using (var duplicate = Start(Path.Combine(stage, "Server", "CaroServer.exe"), port.ToString(), "--background"))
            {
                await duplicate.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
                Assert(duplicate.ExitCode == 0 && Servers(stage).Count == 1, "Duplicate server must exit without binding a second listener.");
            }
            Console.WriteLine("PASS: two published Caro.exe processes, both logins, shared ready server, duplicate server exit.");
            await WaitUntil(() => ReleaseUi.InLobby(third), "Third client must authenticate.");
            ReleaseUi.Chat(first, "LobbyChat", "Xin chào từ client A 👋");
            await WaitUntil(() => ReleaseUi.ChatContains(third, "LobbyChat", "Xin chào từ client A 👋"), "Lobby chat must reach third real client.");
            ReleaseUi.Invite(first, "ReleaseUiB");
            await WaitUntil(() => ReleaseUi.AcceptInvite(second), "Second app must receive and accept invitation.");
            await WaitUntil(() => ReleaseUi.Count(first, 1) == "0" && ReleaseUi.Count(second, 1) == "0", "Both boards must open.");
            string roomCode = new string(ReleaseUi.Across(first, "RoomCode")!.Current.Name.Where(char.IsDigit).ToArray());
            Assert(roomCode.Length == 6, "Visible room code has six digits.");
            ReleaseUi.Set(third, "TxtRoomCode", roomCode); ReleaseUi.Click(third, "BtnJoinRoom");
            await WaitUntil(() => ReleaseUi.Count(third, 1) == "0", "Numeric code must open third client as spectator.");
            Console.WriteLine("PASS: third actual EXE joins by numeric room code.");
            Assert(ReleaseUi.Across(third, "btnExitMatch")!.Current.Name == "THOÁT PHÒNG", "Spectator exit label.");
            await WaitUntil(() => ReleaseUi.Across(first, "Spectator_ReleaseViewer") != null, "Player must see spectator presence.");
            ReleaseUi.ClickAcross(first, "SpectatorLock");
            await WaitUntil(() => ReleaseUi.Across(second, "SpectatorLock")?.Current.Name.Contains("KHÓA") == true, "Room lock must synchronize to player B.");
            ReleaseUi.ClickAcross(second, "SpectatorLock");
            await WaitUntil(() => ReleaseUi.Across(first, "SpectatorLock")?.Current.Name.Contains("MỞ") == true, "Player B unlock must synchronize.");
            Console.WriteLine("PASS: actual player lock/unlock synchronization and spectator presence.");
            ReleaseUi.Move(first, 9, 9); ReleaseUi.Move(second, 8, 9);
            await WaitUntil(() => ReleaseUi.Count(third, 2) == "1", "Spectator sees both accepted moves.");
            ReleaseUi.Chat(first, "RoomChat", "Tin nhắn riêng giữa hai người chơi");
            await WaitUntil(() => ReleaseUi.ChatContains(second, "RoomChat", "Tin nhắn riêng"), "Player chat must reach opponent.");
            ReleaseUi.ClickAcross(first, "btnNewGame");
            await WaitUntil(() => ReleaseUi.Across(second, "NewGameAccept") != null, "Rematch must ask opponent.");
            Assert(ReleaseUi.Count(third, 1) == "1", "Rematch request cannot reset board before acceptance.");
            ReleaseUi.ClickAcross(second, "NewGameAccept");
            await WaitUntil(() => ReleaseUi.Count(first, 1) == "0" && ReleaseUi.Count(second, 1) == "0" && ReleaseUi.Count(third, 1) == "0", "Acceptance resets all three real boards.");
            Console.WriteLine("PASS: actual private chat and rematch accepted by opponent, synchronized reset.");
            ReleaseUi.ClickAcross(third, "btnExitMatch");
            await WaitUntil(() => ReleaseUi.InLobby(third), "Spectator returns to lobby.");
            ReleaseUi.DoubleClickRoom(third);
            await WaitUntil(() => ReleaseUi.Count(third, 1) == "0", "Double-clicking room must enter spectator mode.");
            Console.WriteLine("PASS: three actual EXEs, lobby/private chat, numeric join, spectator presence, lock synchronization, rematch consent and double-click spectate.");
            for (int col = 0; col < 5; col++)
            {
                ReleaseUi.Move(first, 0, col);
                await WaitUntil(() => ReleaseUi.Count(first, 1) == (col + 1).ToString() && ReleaseUi.Count(second, 1) == (col + 1).ToString() && ReleaseUi.Count(third, 1) == (col + 1).ToString(), "X move must appear in all three actual apps.");
                if (col == 4) break;
                ReleaseUi.Move(second, 2, col);
                await WaitUntil(() => ReleaseUi.Count(first, 2) == (col + 1).ToString() && ReleaseUi.Count(second, 2) == (col + 1).ToString(), "O move must appear in both actual apps.");
            }
            await WaitUntil(() => ReleaseUi.ReturnFromResult(first), "Winner result must complete without modal exception.");
            await WaitUntil(() => ReleaseUi.ReturnFromResult(second), "Loser result must complete without modal exception.");
            await WaitUntil(() => ReleaseUi.ReturnFromResult(third), "Spectator can leave result even after both players leave.");
            await WaitUntil(() => ReleaseUi.InLobby(third), "Spectator result returns to lobby.");
            await Close(third);
            await WaitUntil(() => ReleaseUi.InLobby(first) && ReleaseUi.InLobby(second), "Both apps must return to lobby.");
            await WaitUntil(() => ReleaseUi.Sees(first, "ReleaseUiB") && ReleaseUi.Sees(second, "ReleaseUiA"), "Returning from match must restore both lobby entries.");
            Console.WriteLine("PASS: actual published UI invitation, acceptance, nine alternating moves, synchronized counts, win/result and return to lobby without .NET error.");
            string host = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))?.ToString() ?? "127.0.0.1";
            Console.WriteLine($"Testing published TCP server through {host}:{port}.");
            await Close(first);
            await WaitUntil(() => !ReleaseUi.Sees(second, "ReleaseUiA"), "Leave broadcast must remove the other app.");
            Assert(Servers(stage).Count == 1, "Closing app A must keep shared server for B.");
            var remoteSuccess = Start(Path.Combine(stage, "Caro.exe")); processes.Add(remoteSuccess);
            await WaitUntil(() => LoginReady(remoteSuccess), "Remote test app must reach login.");
            ReleaseUi.RemoteLogin(remoteSuccess, host, port, "ReleaseRemote");
            await WaitUntil(() => ReleaseUi.InLobby(remoteSuccess), "Actual remote-mode app must login through non-loopback address.");
            await WaitUntil(() => ReleaseUi.Sees(second, "ReleaseRemote") && ReleaseUi.Sees(remoteSuccess, "ReleaseUiB"), "Remote/local clients must share presence.");
            Assert(Servers(stage).Count == 1, "Remote login must not create another server.");
            Console.WriteLine($"PASS: actual EXE Remote mode and two-way presence through {host}:{port}.");
            await Close(remoteSuccess);
            await Close(second);
            if (!args.Contains("--startup-only")) { await NetworkChecks.Run(port, host); await SocialChecks.Run(port, host); }
            using (var stop = EventWaitHandle.OpenExisting($"Local\\Caro.Stop.{port}")) stop.Set();
            await WaitUntil(() => Servers(stage).Count == 0, "Server must stop cleanly.");
            await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new ServerConfiguration { Host = "192.0.2.1", Port = port, AutoStartLocalServer = true }));
            var remote = Start(Path.Combine(stage, "Caro.exe")); processes.Add(remote);
            await WaitUntil(() => LoginReady(remote), "Remote configuration must reach login.");
            ReleaseUi.Login(remote, "RemoteFailureTest");
            await WaitUntil(() => ReleaseUi.Find(remote, "BtnConnect")?.Current.IsEnabled == true, "Failed remote attempt must restore retry button.");
            Assert(LoginReady(remote) && !ReleaseUi.InLobby(remote), "Remote failure must not open a fake lobby.");
            Assert(Servers(stage).Count == 0, "Remote failure must never start a local server or fall back to localhost.");
            Assert(!string.IsNullOrWhiteSpace(ReleaseUi.Find(remote, "ConnectionStatus")!.Current.Name), "Remote failure needs a friendly message.");
            Console.WriteLine("PASS: remote configuration opens login without starting any local server.");
            Console.WriteLine("PASS: release smoke suite. Stage: " + stage);
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally
        {
            foreach (var p in processes)
            {
                try { await Close(p); }
                catch (TimeoutException) { }
                finally { p.Dispose(); }
            }
            if (EventWaitHandle.TryOpenExisting($"Local\\Caro.Stop.{port}", out var stop)) using (stop) stop.Set();
            // Cleanup only the uniquely staged test processes, never another user's server.
            foreach (var p in Servers(stage)) { if (!p.WaitForExit(3000)) p.Kill(); p.Dispose(); }
        }
    }
    private static Process Start(string path, params string[] args)
    {
        var info = new ProcessStartInfo(path) { UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, WorkingDirectory = Path.GetDirectoryName(path)! };
        foreach (string arg in args) info.ArgumentList.Add(arg);
        return Process.Start(info)!;
    }
    private static bool LoginReady(Process p)
    {
        p.Refresh(); return !p.HasExited && p.MainWindowTitle.Contains("Đăng Nhập - C A R O");
    }
    private static List<Process> Servers(string stage) => Process.GetProcessesByName("CaroServer")
        .Where(p => { try { return p.MainModule?.FileName.StartsWith(stage + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) == true; } catch { return false; } }).ToList();
    private static async Task WaitUntil(Func<bool> condition, string message)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition()) { Assert(timeout.Elapsed < TimeSpan.FromSeconds(25), message); await Task.Delay(100); }
    }
    private static async Task Close(Process p)
    {
        if (p.HasExited) return;
        p.Refresh();
        Console.WriteLine($"Closing {p.Id}: title={p.MainWindowTitle}, handle={p.MainWindowHandle}, sent={p.CloseMainWindow()}");
        try { await p.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3)); }
        catch (TimeoutException) { p.Refresh(); Console.WriteLine($"FAILED graceful close: {p.MainWindowTitle}, responding={p.Responding}"); p.Kill(); await p.WaitForExitAsync(); throw; }
    }
}
