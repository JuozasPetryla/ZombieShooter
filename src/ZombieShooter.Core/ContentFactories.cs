// Abstract factory
namespace ZombieShooter.Core;

public interface IContentFactory
{
    string Name { get; }
    EntityFactory CreateEntityFactory();
    IGameMode CreateGameMode();
}

public sealed class NormalContentFactory : IContentFactory
{
    public string Name => "NORMAL";
    public EntityFactory CreateEntityFactory() => new EntityFactory();
    public IGameMode CreateGameMode() => new SurvivalMode();
}

public sealed class NightmareContentFactory : IContentFactory
{
    public string Name => "NIGHTMARE";
    public EntityFactory CreateEntityFactory() => new NightmareEntityFactory();
    public IGameMode CreateGameMode() => new NightmareMode();
}

public sealed class NightmareEntityFactory : EntityFactory
{
    protected override (int Health, float Speed) ZombieStats(ZombieKind kind)
    {
        var (health, speed) = base.ZombieStats(kind);
        return ((int)(health * 1.5f), speed * 1.25f);
    }
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
