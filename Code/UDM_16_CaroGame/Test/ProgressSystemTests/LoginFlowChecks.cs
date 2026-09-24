using System.Net;
using System.Net.Sockets;
using System.Text;
using CaroClient;
using CaroShared.Contracts;
using CaroShared.Enums;
using CaroShared.Protocol;

internal static class LoginFlowChecks
{
    internal static void Run()
    {
        CheckConnectionFailures().GetAwaiter().GetResult();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledge = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var refreshed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var server = Task.Run(async () =>
        {
            using var peer = await listener.AcceptTcpClientAsync(lifetime.Token);
            using var reader = new StreamReader(peer.GetStream());
            var serializer = new MessageSerializer();
            var login = serializer.Deserialize((await reader.ReadLineAsync(lifetime.Token))!);
            Program.Assert(login.Type == MessageType.LoginRequest, "Login request expected.");
            received.SetResult();
            await acknowledge.Task.WaitAsync(lifetime.Token);
            async Task Send(MessageType type, object payload) => await peer.GetStream().WriteAsync(Encoding.UTF8.GetBytes(serializer.Serialize(new(type, payload))), lifetime.Token);
            await Send(MessageType.LoginResponse, new PlayerListResponse { SessionToken = "login-test", Players = [new() { PlayerName = "ExistingPlayer" }] });
            for (int i = 0; i < 2; i++)
            {
                var request = serializer.Deserialize((await reader.ReadLineAsync(lifetime.Token))!);
                if (request.Type == MessageType.PlayerListRequest) await Send(MessageType.PlayerListResponse, new PlayerListResponse());
                else if (request.Type == MessageType.RoomListRequest) await Send(MessageType.RoomListResponse, new RoomListResponse());
            }
            refreshed.SetResult();
            await Task.Delay(Timeout.Infinite, lifetime.Token);
        });
        using var form = new LoginForm(new ServerConfiguration { Port = port, AutoStartLocalServer = false });
        Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                Program.Find<TextBox>(form, "TxtNickname").Text = "LoginAckTest";
                Program.Find<Button>(form, "BtnConnect").PerformClick();
                await received.Task.WaitAsync(TimeSpan.FromSeconds(5));
                Program.Assert(form.Visible && Program.Find<SoftLoadingIndicator>(form, "connectingIndicator").Visible, "Loading must remain visible until server ACK.");
                acknowledge.SetResult();
                LobbyForm? lobby = null;
                for (int i = 0; i < 100 && lobby == null; i++)
                {
                    await Task.Delay(30);
                    lobby = Application.OpenForms.OfType<LobbyForm>().FirstOrDefault();
                }
                Program.Assert(lobby?.Visible == true && !form.Visible, "Successful ACK must open lobby.");
                Program.Assert(CaroClient.Network.NetworkClient.Instance.SessionToken == "login-test", "Login must retain server token.");
                await refreshed.Task.WaitAsync(TimeSpan.FromSeconds(5));
                lobby!.Close();
                Console.WriteLine("PASS: real login form waits for TCP acknowledgement and opens lobby.");
            }
            catch (Exception ex) { failure = ex; form.Close(); }
        };
        Application.Run(form);
        lifetime.Cancel();
        try { server.GetAwaiter().GetResult(); } catch (OperationCanceledException) { }
        if (failure != null) throw failure;
    }
    private static async Task CheckConnectionFailures()
    {
        foreach (string host in new[] { "127.0.0.1", "localhost", "::1", "[::1]" })
            Program.Assert(new ServerConfiguration { Host = host }.UsesLocalServer, "Loopback aliases must be local.");
        foreach (string host in new[] { "0.0.0.0", "::", "http://localhost", "" })
        {
            bool rejected = false;
            try { new ServerConfiguration { Host = host }.Validate(); } catch (InvalidDataException) { rejected = true; }
            Program.Assert(rejected, "Reject invalid connection destination: " + host);
        }
        var network = CaroClient.Network.NetworkClient.Instance;
        using var reserved = new TcpListener(IPAddress.Loopback, 0);
        reserved.Start(); int closedPort = ((IPEndPoint)reserved.LocalEndpoint).Port; reserved.Stop();
        Program.Assert(!await network.ConnectAsync("127.0.0.1", closedPort) && network.LastConnectionError.Contains("chưa nhận kết nối"), "Closed port must report connection refused.");
        Program.Assert(!await network.ConnectAsync("caro-test.invalid", closedPort) && network.LastConnectionError.Contains("Không tìm thấy"), "Invalid DNS must report host not found.");
        network.Disconnect();
        Console.WriteLine("PASS: loopback aliases, invalid destinations, actual refused connection and DNS failure messages.");
    }
}
