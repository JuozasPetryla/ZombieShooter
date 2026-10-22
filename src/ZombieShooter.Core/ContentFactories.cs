namespace ZombieShooter.Core;

public interface IContentFactory
{
    EntityFactory CreateEntityFactory();
    IGameMode CreateGameMode();
    IDropPolicy CreateDropPolicy();
}

public interface IDropPolicy
{
    PickupKind? Roll(Random random);
}

// Each family is an Abstract Factory, a concrete Builder and a wave Strategy.
// Wave/drop methods are stateless: the factory can return itself through those
// product interfaces. Entity factories use fresh builders for session isolation.
public sealed class NormalContent : ZombieBuilder, IContentFactory, IGameMode, IDropPolicy
{
    public EntityFactory CreateEntityFactory() => new EntityFactory();
    public IGameMode CreateGameMode() => this;
    public IDropPolicy CreateDropPolicy() => this;

    public float IntermissionSeconds => 4;
    public int EnemyCount(int wave, int playerCount) => (4 + wave * 3) * playerCount;
    public float SpawnInterval(int wave) => Math.Max(0.18f, 0.85f - wave * 0.035f);
    public ZombieKind ChooseZombie(int wave, Random random)
    {
        var roll = random.Next(100);
        if (wave >= 5 && roll < 18) return ZombieKind.Brute;
        return wave >= 3 && roll < 45 ? ZombieKind.Runner : ZombieKind.Walker;
    }
    public PickupKind? Roll(Random random) => random.NextSingle() < 0.22f
        ? (random.Next(2) == 0 ? PickupKind.Health : PickupKind.Shotgun) : null;
}

public sealed class NightmareContent : ZombieBuilder, IContentFactory, IGameMode, IDropPolicy
{
    public EntityFactory CreateEntityFactory() => new NightmareEntityFactory();
    public IGameMode CreateGameMode() => this;
    public IDropPolicy CreateDropPolicy() => this;

    public override ZombieArchetype Build()
    {
        var result = base.Build();
        result.Health = (int)(result.Health * 1.5f);
        result.Speed *= 1.25f;
        return result;
    }

    public float IntermissionSeconds => 3;
    public int EnemyCount(int wave, int playerCount) => (6 + wave * 4) * playerCount;
    public float SpawnInterval(int wave) => Math.Max(0.12f, 0.6f - wave * 0.035f);
    public ZombieKind ChooseZombie(int wave, Random random)
    {
        var roll = random.Next(100);
        if (wave >= 3 && roll < 25) return ZombieKind.Brute;
        return wave >= 2 && roll < 60 ? ZombieKind.Runner : ZombieKind.Walker;
    }
    public PickupKind? Roll(Random random) => random.NextSingle() < 0.15f
        ? (random.Next(3) == 0 ? PickupKind.Health : PickupKind.Shotgun) : null;
}

public sealed class NightmareEntityFactory : EntityFactory
{
    public NightmareEntityFactory() : base(new NightmareContent()) { }
}
