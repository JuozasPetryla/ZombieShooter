namespace ZombieShooter.Core.Commands;

public interface IGameCommand
{
    int PlayerId { get; }
    bool Execute(GameSession session);
    bool Undo(GameSession session);
}

// ConcreteCommand: receives the session at execution and remembers it for safe undo. Version checks
// prevent undo after firing, completing a reload, collecting an item, death or a wave transition.
public abstract class PlayerCommand(int playerId) : IGameCommand
{
    public int PlayerId { get; } = playerId;
    private GameSession? _receiver;
    private bool _executed;
    private WeaponKind _weapon;
    private float _reload;
    private long _version;
    public bool Execute(GameSession session)
    {
        if (_executed) return false;
        var player = session.Players.FirstOrDefault(p => p.Id == PlayerId);
        if (player is null) return false;
        _weapon = player.Weapon;
        _reload = player.ReloadRemaining;
        if (!Apply(session)) return false;
        _receiver = session;
        _executed = true;
        _version = player.ActionVersion;
        return true;
    }
    public abstract bool Apply(GameSession session);
    public bool Undo(GameSession session)
    {
        if (!_executed || !ReferenceEquals(_receiver, session)) return false;
        var player = session.Players.FirstOrDefault(p => p.Id == PlayerId);
        if (player is null || !player.IsAlive || session.State == SessionState.GameOver ||
            player.ActionVersion != _version || (_weapon == WeaponKind.Shotgun && player.WeaponTime <= 0)) return false;
        player.Weapon = _weapon;
        player.ReloadRemaining = _reload;
        player.ActionVersion++;
        _executed = false;
        return true;
    }
}
public sealed class ReloadCommand(int playerId) : PlayerCommand(playerId)
{
    public override bool Apply(GameSession session) => session.TryReload(PlayerId);
}
public sealed class SelectWeaponCommand(int playerId, WeaponKind weapon) : PlayerCommand(playerId)
{
    public WeaponKind Weapon { get; } = weapon;
    public override bool Apply(GameSession session) => session.TrySelectWeapon(PlayerId, Weapon);
}
public sealed class UndoCommand(int playerId) : IGameCommand
{
    public int PlayerId { get; } = playerId;
    public bool Execute(GameSession session) => session.UndoLast(PlayerId);
    public bool Undo(GameSession session) => false;
}
