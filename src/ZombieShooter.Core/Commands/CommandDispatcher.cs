namespace ZombieShooter.Core.Commands;

// Invoker: owns the bounded FIFO and one-step undo history for one session.
public sealed class CommandDispatcher(GameSession session)
{
    private readonly Queue<IGameCommand> _pending = new();
    private readonly Dictionary<int, IGameCommand> _last = new();

    public bool Queue(IGameCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (session.State == SessionState.GameOver || _pending.Count >= 64) return false;
        _pending.Enqueue(command);
        return true;
    }

    public void ExecutePending()
    {
        int count = _pending.Count;
        for (int i = 0; i < count; i++)
        {
            var command = _pending.Dequeue();
            if (command.Execute(session) && command is not UndoCommand)
                _last[command.PlayerId] = command;
        }
    }

    public bool UndoLast(int playerId) =>
        _last.Remove(playerId, out var command) && command.Undo(session);

    public void Clear()
    {
        _pending.Clear();
        _last.Clear();
    }
}
