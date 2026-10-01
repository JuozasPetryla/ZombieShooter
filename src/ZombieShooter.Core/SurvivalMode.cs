namespace ZombieShooter.Core;

// Strategy: another mode can replace wave composition and pacing without changing combat.
public interface IGameMode
{
    float IntermissionSeconds { get; }
    int EnemyCount(int wave, int playerCount);
    float SpawnInterval(int wave);
    ZombieKind ChooseZombie(int wave, Random random);
}

public sealed class SurvivalMode : IGameMode
{
    public float IntermissionSeconds => 4;
    public int EnemyCount(int wave, int playerCount) => (4 + wave * 3) * playerCount;
    public float SpawnInterval(int wave) => Math.Max(0.18f, 0.85f - wave * 0.035f);
    public ZombieKind ChooseZombie(int wave, Random random)
    {
        var roll = random.Next(100);
        if (wave >= 5 && roll < 18) return ZombieKind.Brute;
        return wave >= 3 && roll < 45 ? ZombieKind.Runner : ZombieKind.Walker;
    }
}
