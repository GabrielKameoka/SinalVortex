using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SinalVortex.IntegrationTests.Support;

// Real loopback SMTP; controlled failures reject RCPT TO before accepting DATA.
internal sealed class SmtpTestServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly TaskCompletionSource<string> _message = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task _server;
    private readonly int _recipientStatus;
    private int _attempts;

    public SmtpTestServer(int recipientStatus = 250)
    {
        _recipientStatus = recipientStatus;
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _server = RunAsync(_shutdown.Token);
    }

    public int Port { get; }
    public int Attempts => Volatile.Read(ref _attempts);
    public Task<string> Message => _message.Task;

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                try
                {
                    await using var stream = client.GetStream();
                    using var reader = new StreamReader(stream);
                    await using var writer = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true };
                    await writer.WriteLineAsync("220 localhost SMTP test");
                    var content = new StringBuilder();
                    var data = false;
                    while (await reader.ReadLineAsync(cancellationToken) is { } line)
                    {
                        if (data)
                        {
                            if (line == ".")
                            {
                                await writer.WriteLineAsync("250 accepted");
                                _message.TrySetResult(content.ToString());
                                data = false;
                            }
                            else content.AppendLine(line.StartsWith("..") ? line[1..] : line);
                        }
                        else if (line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                        {
                            Interlocked.Increment(ref _attempts);
                            await writer.WriteLineAsync($"{_recipientStatus} recipient response");
                        }
                        else if (line == "DATA")
                        {
                            data = true;
                            await writer.WriteLineAsync("354 send message");
                        }
                        else if (line == "QUIT")
                        {
                            await writer.WriteLineAsync("221 goodbye");
                            break;
                        }
                        else await writer.WriteLineAsync("250 OK");
                    }
                }
                catch (IOException) when (_recipientStatus >= 400)
                {
                    // SmtpClient may reset the connection after a rejected recipient.
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            _message.TrySetException(exception);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _shutdown.CancelAsync();
        await _server;
        _listener.Stop();
        _shutdown.Dispose();
    }
}
