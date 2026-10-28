using System.Net;
using System.Net.Sockets;
using System.Numerics;
using ZombieShooter.Core.Commands;

namespace ZombieShooter.Core.Networking;

// One authoritative two-player match. Both connections are required to advance.
public sealed class GameServer : IAsyncDisposable, INotificationOutput, IGameObserver
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _gate = new();
    private readonly GameFacade _game;
    private readonly TcpClient?[] _clients = new TcpClient?[2];
    private readonly long[] _lastInput = new long[2];
    private readonly List<Task> _connections = new();
    private Task? _accept;
    private Task? _ticks;
    private readonly WaveNotification _waveNotice;
    private readonly PickupNotification _pickupNotice;
    public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
    public GameServer(int port = 7777, GamePreset preset = GamePreset.Normal)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
        _game = GameFacade.CreatePreset(preset, 2, Environment.TickCount);
        _waveNotice = new WaveNotification(this);
        _pickupNotice = new PickupNotification(this);
        _game.Session.Attach(this);
    }
    // Bridge output and Observer subscriber share the existing server host.
    public void Write(string text) => Console.WriteLine(text);
    public void OnGameEvent(GameEvent message)
    {
        if (message.Name is "WaveStarted" or "WaveCleared" or "WeaponsUpgraded") _waveNotice.Show(message);
        else if (message.Name == "PickupCollected") _pickupNotice.Show(message);
    }

    public void Start()
    {
        if (_accept is not null) throw new InvalidOperationException("Server already started.");
        _listener.Start();
        _accept = AcceptAsync();
        _ticks = TickAsync();
    }
    public async Task AcceptAsync()
    {
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(_stop.Token);
                client.NoDelay = true;
                lock (_gate)
                {
                    int slot = Array.FindIndex(_clients, c => c is null);
                    if (slot < 0) { client.Dispose(); continue; }
                    _clients[slot] = client;
                    _lastInput[slot] = Environment.TickCount64;
                    _connections.RemoveAll(t => t.IsCompleted);
                    _connections.Add(ServeAsync(client, slot));
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
        catch (SocketException) when (_stop.IsCancellationRequested) { }
    }
    public ServerFrame Frame(int slot) => new(_game.Session.Players[slot].Id,
        _game.Snapshot(_clients.Count(c => c is not null)));
    public async Task ServeAsync(TcpClient client, int slot)
    {
        try
        {
            var stream = client.GetStream();
            ServerFrame hello;
            lock (_gate) hello = Frame(slot);
            await WireProtocol.WriteAsync(stream, hello, _stop.Token);
            while (!_stop.IsCancellationRequested)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                var input = await WireProtocol.ReadAsync<ClientFrame>(stream, timeout.Token);
                if (input.Actions is null || input.Actions.Length > 16 ||
                    !float.IsFinite(input.MoveX) || !float.IsFinite(input.MoveY) ||
                    !float.IsFinite(input.AimX) || !float.IsFinite(input.AimY) ||
                    input.Actions.Any(a => !Enum.IsDefined(a))) throw new InvalidDataException("Invalid input.");
                ServerFrame response;
                lock (_gate)
                {
                    int playerId = _game.Session.Players[slot].Id;
                    _lastInput[slot] = Environment.TickCount64;
                    bool ready = _clients.All(c => c is not null);
                    _game.SetInput(playerId, ready ? new(new Vector2(input.MoveX, input.MoveY),
                        new Vector2(input.AimX, input.AimY), input.Fire) : default);
                    if (ready)
                        foreach (var action in input.Actions)
                            _game.Queue(action switch
                            {
                                NetworkAction.Reload => new ReloadCommand(playerId),
                                NetworkAction.Pistol => new SelectWeaponCommand(playerId, WeaponKind.Pistol),
                                NetworkAction.Shotgun => new SelectWeaponCommand(playerId, WeaponKind.Shotgun),
                                _ => new UndoCommand(playerId)
                            });
                    response = Frame(slot);
                }
                await WireProtocol.WriteAsync(stream, response, timeout.Token);
                // Bound each connection's processing rate; the simulation has its own clock.
                await Task.Delay(16, _stop.Token);
            }
        }
        catch (Exception e) when (e is IOException or SocketException or OperationCanceledException or
            System.Text.Json.JsonException or ObjectDisposedException or InvalidDataException) { }
        finally
        {
            lock (_gate)
            {
                _clients[slot] = null;
                _game.ClearInputs();
            }
            client.Dispose();
        }
    }
    public async Task TickAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / 60));
        try
        {
            while (await timer.WaitForNextTickAsync(_stop.Token))
                lock (_gate)
                {
                    for (int i = 0; i < 2; i++)
                        if (Environment.TickCount64 - _lastInput[i] > 250)
                            _game.SetInput(_game.Session.Players[i].Id, default);
                    if (_clients.All(c => c is not null)) _game.Tick(1f / 60);
                }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested) { }
    }
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel();
        _listener.Stop();
        lock (_gate) foreach (var client in _clients) client?.Dispose();
        if (_accept is not null) await _accept;
        if (_ticks is not null) await _ticks;
        await Task.WhenAll(_connections);
        _stop.Dispose();
    }
}
