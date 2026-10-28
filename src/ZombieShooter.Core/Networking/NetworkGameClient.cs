using System.Net.Sockets;

namespace ZombieShooter.Core.Networking;

public sealed class NetworkGameClient : IDisposable
{
    private readonly TcpClient _client;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _gate = new();
    private readonly Queue<NetworkAction> _actions = new();
    private PlayerInput _input;
    private ServerFrame _latest;
    private string? _error;
    private readonly Task _worker;
    public int PlayerId { get; }
    public ServerFrame Latest => Volatile.Read(ref _latest);
    public string? Error => Volatile.Read(ref _error);
    private NetworkGameClient(TcpClient client, ServerFrame hello)
    {
        _client = client;
        _latest = hello;
        PlayerId = hello.PlayerId;
        _worker = ExchangeAsync();
    }
    public static async Task<NetworkGameClient> ConnectAsync(string host, int port = 7777)
    {
        var client = new TcpClient { NoDelay = true };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await client.ConnectAsync(host, port, timeout.Token);
            var hello = await WireProtocol.ReadAsync<ServerFrame>(client.GetStream(), timeout.Token);
            return new(client, hello);
        }
        catch { client.Dispose(); throw; }
    }
    public void SetInput(PlayerInput input) { lock (_gate) _input = input; }
    public bool Queue(NetworkAction action)
    {
        lock (_gate)
        {
            if (_actions.Count >= 16 || Error is not null) return false;
            _actions.Enqueue(action);
            return true;
        }
    }
    public async Task ExchangeAsync()
    {
        try
        {
            var stream = _client.GetStream();
            while (!_stop.IsCancellationRequested)
            {
                ClientFrame frame;
                lock (_gate)
                {
                    frame = new(_input.Move.X, _input.Move.Y, _input.Aim.X, _input.Aim.Y, _input.Fire, _actions.ToArray());
                    _actions.Clear();
                }
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                await WireProtocol.WriteAsync(stream, frame, timeout.Token);
                var response = await WireProtocol.ReadAsync<ServerFrame>(stream, timeout.Token);
                Volatile.Write(ref _latest, response);
                await Task.Delay(16, _stop.Token);
            }
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException or
            System.Text.Json.JsonException or ObjectDisposedException or InvalidDataException)
        {
            if (!_stop.IsCancellationRequested) Volatile.Write(ref _error, "CONNECTION LOST - ESC TO EXIT");
        }
    }
    public void Dispose()
    {
        _stop.Cancel();
        _client.Dispose();
        _worker.GetAwaiter().GetResult();
        _stop.Dispose();
    }
}
