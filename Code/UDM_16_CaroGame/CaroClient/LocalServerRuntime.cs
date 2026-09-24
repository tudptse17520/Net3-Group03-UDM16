using System.Diagnostics;

namespace CaroClient;

public static class LocalServerRuntime
{
    // Readiness uses a named event, never a TCP probe that would create a fake player.
    public static Task EnsureReadyAsync(ServerConfiguration config, CancellationToken token = default)
    {
        if (!config.IsLocal || !config.AutoStartLocalServer) return Task.CompletedTask;
        return Task.Run(() =>
        {
            using var launchLock = new Mutex(false, $"Local\\Caro.Launch.{config.Port}");
            int acquired;
            try { acquired = WaitHandle.WaitAny([launchLock, token.WaitHandle], TimeSpan.FromSeconds(20)); }
            catch (AbandonedMutexException) { acquired = 0; }
            if (acquired == 1) token.ThrowIfCancellationRequested();
            if (acquired != 0) throw new TimeoutException("Máy chủ đang khởi động. Vui lòng thử lại.");
            try
            {
                using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, $"Local\\Caro.Ready.{config.Port}");
                if (ready.WaitOne(0) && ServerOwnsMutex(config.Port)) return;
                ready.Reset();
                string executable = Path.Combine(AppContext.BaseDirectory, "Server", "CaroServer.exe");
                if (!File.Exists(executable))
                    throw new FileNotFoundException("Chưa có máy chủ cục bộ. Hãy dùng bản phát hành đầy đủ hoặc nhập địa chỉ máy chủ.");
                var start = new ProcessStartInfo(executable)
                {
                    UseShellExecute = false, CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.GetDirectoryName(executable)!
                };
                start.ArgumentList.Add(config.Port.ToString());
                start.ArgumentList.Add("--background");
                using var process = Process.Start(start) ?? throw new IOException("Không mở được máy chủ cục bộ.");
                int result = WaitHandle.WaitAny([ready, token.WaitHandle], TimeSpan.FromSeconds(15));
                if (result == 1) token.ThrowIfCancellationRequested();
                if (result != 0) throw new TimeoutException("Máy chủ chưa sẵn sàng. Cổng kết nối có thể đang được ứng dụng khác sử dụng.");
            }
            finally { launchLock.ReleaseMutex(); }
        }, token);
    }

    private static bool ServerOwnsMutex(int port)
    {
        using var server = new Mutex(false, $"Local\\Caro.Server.{port}");
        bool acquired;
        try { acquired = server.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
        if (acquired) server.ReleaseMutex();
        return !acquired;
    }
}
