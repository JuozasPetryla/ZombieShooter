namespace ZombieShooter.Core.Commands;

/// <summary>A discrete intent. The receiver validates current game state on execution.</summary>
public interface IGameCommand
{
    int PlayerId { get; }
    bool Execute(GameSession session);
}

public sealed record ReloadCommand(int PlayerId) : IGameCommand
{
    public bool Execute(GameSession session) => session.TryReload(PlayerId);
}

public sealed record SelectWeaponCommand(int PlayerId, WeaponKind Weapon) : IGameCommand
{
    public bool Execute(GameSession session) => session.TrySelectWeapon(PlayerId, Weapon);
}
