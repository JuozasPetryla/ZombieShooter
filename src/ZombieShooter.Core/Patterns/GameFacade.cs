using ZombieShooter.Core.Commands;

namespace ZombieShooter.Core;

public enum GamePreset { Normal, Nightmare, Horde, Brute }

public sealed class InputBuffer
{
    private readonly Dictionary<int, PlayerInput> _inputs = new();
    public IReadOnlyDictionary<int, PlayerInput> Values => _inputs;
    public void Set(int playerId, PlayerInput input) => _inputs[playerId] = input;
    public void Clear() => _inputs.Clear();
}
// Facade: desktop host and TCP server use one interface to input buffering, command dispatch and the game session.
// Callers serialize access; each match owns its own facade and simulation.
public sealed class GameFacade
{
    private readonly InputBuffer _inputs = new();
    private readonly CommandDispatcher _commands;

    private readonly Queue<SequencedEvent> _events = new();
    private long _eventSequence;
    public GameSession Session { get; }

    // Desktop and server expose the same four choices, without independent family overrides.
    public static GameFacade CreatePreset(GamePreset preset, int players = 1, int seed = 1) => preset switch
    {
        GamePreset.Normal => new(players, seed),
        GamePreset.Nightmare => new(players, seed, content: new NightmareContent()),
        GamePreset.Horde => new(players, seed, mode: new HordeMode()),
        GamePreset.Brute => new(players, seed, mode: new BruteMode()),
        _ => throw new ArgumentOutOfRangeException(nameof(preset))
    };

    public GameFacade(int players = 1, int seed = 1, IGameMode? mode = null, IContentFactory? content = null)
    {
        Session = new GameSession(players, seed, mode, content);
        _commands = Session.Commands;
        Session.EventOccurred += e =>
        {
            _events.Enqueue(new(++_eventSequence, e));
            while (_events.Count > 128) _events.Dequeue();
        };
    }
    public void SetInput(int playerId, PlayerInput input) => _inputs.Set(playerId, input);
    public bool Queue(IGameCommand command) => _commands.Queue(command);
    public void Tick(float dt) => Session.Update(dt, _inputs.Values);
    public void ClearInputs()
    {
        _inputs.Clear();
        Session.DiscardPendingCommands();
    }
    public SessionSnapshot Snapshot(int connectedPlayers = 0) => Session.CaptureSnapshot(_events.ToArray(), connectedPlayers);
}
