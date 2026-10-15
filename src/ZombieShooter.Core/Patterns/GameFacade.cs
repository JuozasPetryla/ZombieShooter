using ZombieShooter.Core.Commands;

namespace ZombieShooter.Core;

public sealed class InputBuffer
{
    private readonly Dictionary<int, PlayerInput> _inputs = new();
    public IReadOnlyDictionary<int, PlayerInput> Values => _inputs;
    public void Set(int playerId, PlayerInput input) => _inputs[playerId] = input;
    public void Clear() => _inputs.Clear();
}
public sealed class CommandDispatcher(GameSession session)
{
    public bool Queue(IGameCommand command) => session.EnqueueCommand(command);
}
public sealed class MatchSimulation(GameSession session)
{
    public void Tick(float dt, IReadOnlyDictionary<int, PlayerInput> inputs) => session.Update(dt, inputs);
}

// Facade: desktop host and TCP server use one interface to three subsystems.
// Callers serialize access; each match owns its own facade and simulation.
public sealed class GameFacade
{
    private readonly InputBuffer _inputs = new();
    private readonly CommandDispatcher _commands;
    private readonly MatchSimulation _simulation;
    private readonly Queue<SequencedEvent> _events = new();
    private long _eventSequence;
    public GameSession Session { get; }
    public GameFacade(int players = 1, int seed = 1, IGameMode? mode = null, IContentFactory? content = null)
    {
        Session = new GameSession(players, seed, mode, content);
        _commands = new CommandDispatcher(Session);
        _simulation = new MatchSimulation(Session);
        Session.EventOccurred += e =>
        {
            _events.Enqueue(new(++_eventSequence, e));
            while (_events.Count > 128) _events.Dequeue();
        };
    }
    public void SetInput(int playerId, PlayerInput input) => _inputs.Set(playerId, input);
    public bool Queue(IGameCommand command) => _commands.Queue(command);
    public void Tick(float dt) => _simulation.Tick(dt, _inputs.Values);
    public void ClearInputs()
    {
        _inputs.Clear();
        Session.DiscardPendingCommands();
    }
    public SessionSnapshot Snapshot(int connectedPlayers = 0) => Session.CaptureSnapshot(_events.ToArray(), connectedPlayers);
}
