using System.Reflection;
using System.Text.Json;
using CaroClient;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        ApplicationConfiguration.Initialize();
        var cases = new (string Name, Func<GameBoardForm, Task> Check)[]
        {
            ("Result ownership, duplicate open, minimize and restore", CheckResultWindow),
            ("Victory animation with early server confirmation", b => CheckGameOver(b, 1, false, true)),
            ("Victory cannot skip the 3-second celebration; late confirmation", b => CheckGameOver(b, 1, true, false)),
            ("Opponent victory", b => CheckGameOver(b, 2, false, false)),
            ("Draw", b => CheckGameOver(b, 0, false, true))
        };
        foreach (var test in cases)
        {
            if (!RunCase(test.Name, test.Check)) return 1;
        }
        return 0;
    }

    private static bool RunCase(string name, Func<GameBoardForm, Task> check)
    {
        using var board = new GameBoardForm();
        Exception? failure = null;
        ThreadExceptionEventHandler onError = (_, e) =>
        {
            failure = e.Exception;
            board.Close();
        };
        Application.ThreadException += onError;
        board.Shown += async (_, _) =>
        {
            try
            {
                await check(board);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                board.Close();
            }
        };
        try
        {
            board.ShowDialog();
        }
        finally
        {
            Application.ThreadException -= onError;
        }
        Console.WriteLine($"{(failure == null ? "PASS" : "FAIL")}: {name}");
        if (failure != null) Console.Error.WriteLine(failure);
        return failure == null;
    }

    private static async Task CheckResultWindow(GameBoardForm board)
    {
        Call(board, "ShowNonBlockingResultShell", "CHIẾN THẮNG!", "Chúc mừng! Bạn đã chiến thắng.");
        var shell = Result(board)!;
        Require(shell.Visible && shell.Owner == board, "Result must be visible and owned by the board.");
        Call(board, "ShowNonBlockingResultShell", "Duplicate", "Duplicate");
        Require(ReferenceEquals(shell, Result(board)), "Duplicate results must reuse the existing window.");
        board.WindowState = FormWindowState.Minimized;
        await WaitUntil(() => !shell.Visible);
        board.WindowState = FormWindowState.Normal;
        await WaitUntil(() => shell.Visible);
        Require(shell.Owner == board, "Restoring must retain the result owner.");
        board.Location = new Point(board.Left + 10, board.Top + 10);
        var expected = board.PointToScreen(new Point(
            (board.ClientSize.Width - shell.Width) / 2, (board.ClientSize.Height - shell.Height) / 2));
        Require(shell.Location == expected, "Result must stay centered when the board moves.");
    }

    private static async Task CheckGameOver(GameBoardForm board, int winner, bool skip, bool confirmEarly)
    {
        // Feed the same client handlers used by network events, without a live server.
        var lastMove = new MoveMadeEventDto { IsValid = true, X = 4, Y = 7, WinnerSymbol = winner };
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        if (winner != 0)
        {
            for (int x = 0; x < 4; x++)
                Call(board, "HandleMoveMade", lastMove with { X = x, WinnerSymbol = 0 });
            Call(board, "HandleMoveMade", lastMove);
        }
        var gameOver = new NetworkMessage(MessageType.GameOverEvent, JsonSerializer.SerializeToElement(lastMove));
        if (confirmEarly) Call(board, "HandleGameOver", gameOver);
        if (skip) Call(board, "SkipCelebrationIfActive");
        if (winner == 1) Require(Result(board) == null, "Result must wait for celebration, even after an early click.");
        await WaitUntil(() => Result(board) is { Visible: true });
        if (winner == 1) Require(elapsed.Elapsed.TotalSeconds is >= 2.9 and < 5.5, "Celebration must last approximately 3 seconds.");
        var shell = Result(board)!;
        Require(shell.Owner == board, "Result must belong to the board.");
        if (!confirmEarly)
        {
            Require(!shell.Controls.Find("btnVanMoi", true).Single().Enabled,
                "New game must wait for server confirmation.");
            Call(board, "HandleGameOver", gameOver);
        }
        await WaitUntil(() => shell.Controls.Find("btnVanMoi", true).Single().Enabled);
        Require(shell.Controls.Find("btnVeSanh", true).Single().Enabled, "Return to lobby must be enabled.");
        Require(!shell.Controls.Find("lblLoading", true).Single().Visible, "Loading must finish.");
        Call(board, "HandleGameOver", gameOver);
        Require(ReferenceEquals(shell, Result(board)), "Repeated game-over events must not open another result.");
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(10);
        while (!condition())
        {
            if (DateTime.UtcNow >= timeout) throw new TimeoutException("UI did not reach the expected state.");
            await Task.Delay(50);
        }
    }

    private static Form? Result(GameBoardForm board) =>
        (Form?)typeof(GameBoardForm).GetField("_resultShellForm", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(board);

    private static void Call(GameBoardForm board, string method, params object[] args) =>
        typeof(GameBoardForm).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(board, args);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
