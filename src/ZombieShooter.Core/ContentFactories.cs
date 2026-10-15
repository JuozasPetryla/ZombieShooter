// Abstract factory
namespace ZombieShooter.Core;

public interface IContentFactory
{
    string Name { get; }
    EntityFactory CreateEntityFactory();
    IGameMode CreateGameMode();
    IDropPolicy CreateDropPolicy();
}

public sealed class NormalContentFactory : IContentFactory
{
    public string Name => "NORMAL";
    public EntityFactory CreateEntityFactory() => new EntityFactory();
    public IGameMode CreateGameMode() => new SurvivalMode();
    public IDropPolicy CreateDropPolicy() => new NormalDropPolicy();
}

public sealed class NightmareContentFactory : IContentFactory
{
    public string Name => "NIGHTMARE";
    public EntityFactory CreateEntityFactory() => new NightmareEntityFactory();
    public IGameMode CreateGameMode() => new NightmareMode();
    public IDropPolicy CreateDropPolicy() => new NightmareDropPolicy();
}

public sealed class NightmareEntityFactory : EntityFactory
{
    public NightmareEntityFactory() : base(new ArmoredZombieBuilder()) { }
}

public sealed class NightmareMode : IGameMode
{
    public float IntermissionSeconds => 3;
    public int EnemyCount(int wave, int playerCount) => (6 + wave * 4) * playerCount;
    public float SpawnInterval(int wave) => Math.Max(0.12f, 0.6f - wave * 0.035f);
    public ZombieKind ChooseZombie(int wave, Random random)
    {
        var roll = random.Next(100);
        if (wave >= 3 && roll < 25) return ZombieKind.Brute;
        return wave >= 2 && roll < 60 ? ZombieKind.Runner : ZombieKind.Walker;
    }
}

public interface IDropPolicy
{
    PickupKind? Roll(Random random);
}
public sealed class NormalDropPolicy : IDropPolicy
{
    public PickupKind? Roll(Random random) => random.NextSingle() < 0.22f
        ? (random.Next(2) == 0 ? PickupKind.Health : PickupKind.Shotgun) : null;
}
public sealed class NightmareDropPolicy : IDropPolicy
{
    public PickupKind? Roll(Random random) => random.NextSingle() < 0.15f
        ? (random.Next(3) == 0 ? PickupKind.Health : PickupKind.Shotgun) : null;
}
