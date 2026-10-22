namespace ZombieShooter.Core.Commands;

public interface IGameCommand
{
    int PlayerId { get; }
    bool Execute();
    bool Undo();
}

// ConcreteCommand: holds its receiver and captures only the state it owns. Version checks
// prevent undo after firing, completing a reload, collecting an item, death or a wave transition.
public abstract class PlayerCommand(GameSession receiver, int playerId) : IGameCommand
{
    public int PlayerId { get; } = playerId;
    protected GameSession Receiver { get; } = receiver;
    private bool _executed;
    private WeaponKind _weapon;
    private float _reload;
    private long _version;
    public bool Execute()
    {
        if (_executed) return false;
        var player = Receiver.Players.FirstOrDefault(p => p.Id == PlayerId);
        if (player is null) return false;
        _weapon = player.Weapon;
        _reload = player.ReloadRemaining;
        if (!Apply()) return false;
        _executed = true;
        _version = player.ActionVersion;
        return true;
    }
    protected abstract bool Apply();
    public bool Undo()
    {
        if (!_executed) return false;
        var player = Receiver.Players.FirstOrDefault(p => p.Id == PlayerId);
        if (player is null || !player.IsAlive || Receiver.State == SessionState.GameOver ||
            player.ActionVersion != _version || (_weapon == WeaponKind.Shotgun && player.WeaponTime <= 0)) return false;
        player.Weapon = _weapon;
        player.ReloadRemaining = _reload;
        player.ActionVersion++;
        _executed = false;
        return true;
    }
}
public sealed class ReloadCommand(GameSession receiver, int playerId) : PlayerCommand(receiver, playerId)
{
    protected override bool Apply() => Receiver.TryReload(PlayerId);
}
public sealed class SelectWeaponCommand(GameSession receiver, int playerId, WeaponKind weapon) : PlayerCommand(receiver, playerId)
{
    public WeaponKind Weapon { get; } = weapon;
    protected override bool Apply() => Receiver.TrySelectWeapon(PlayerId, Weapon);
}
public sealed class UndoCommand(GameSession receiver, int playerId) : IGameCommand
{
    public int PlayerId { get; } = playerId;
    public bool Execute() => receiver.UndoLast(PlayerId);
    public bool Undo() => false;
}
