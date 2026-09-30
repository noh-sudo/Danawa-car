using System.Net;
using System.Net.Sockets;

namespace Danawa.Server.Net;

public sealed class TcpServer
{
    private readonly TcpListener _listener;

    public TcpServer(int port)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        _listener.Start();
        Console.WriteLine($"서버 시작: 포트 {((IPEndPoint)_listener.LocalEndpoint).Port}");

        while (!ct.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var session = new ClientSession(client);
            _ = session.RunAsync(ct);
        }

        _listener.Stop();
    }
}
