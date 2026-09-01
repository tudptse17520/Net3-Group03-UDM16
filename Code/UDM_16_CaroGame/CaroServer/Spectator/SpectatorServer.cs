using System.Net;
using System.Net.Sockets;

namespace CaroServer.Spectator
{
    public sealed class SpectatorServer
    {
        private readonly TcpListener _listener;
        private readonly SpectatorConnectionHandler _handler;

        public SpectatorServer(IPAddress address, int port, SpectatorConnectionHandler handler)
        {
            _listener = new TcpListener(address, port);
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _listener.Start();
            Console.WriteLine($"[{DateTimeOffset.Now:O}] CaroServer listening for spectators on {_listener.LocalEndpoint}.");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    _ = HandleClientAsync(client, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Shutdown bình thường.
            }
            finally
            {
                _listener.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            {
                try
                {
                    Console.WriteLine($"[{DateTimeOffset.Now:O}] Client connected: {client.Client.RemoteEndPoint}");
                    await _handler.HandleAsync(client, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTimeOffset.Now:O}] Client error: {ex.Message}");
                }
                finally
                {
                    Console.WriteLine($"[{DateTimeOffset.Now:O}] Client disconnected.");
                }
            }
        }
    }
}
