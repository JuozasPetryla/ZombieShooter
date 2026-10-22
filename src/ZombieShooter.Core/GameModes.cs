namespace ZombieShooter.Core;

// Strategy: another mode can replace wave composition and pacing without changing combat.
public interface IGameMode
{
    float IntermissionSeconds { get; }
    int EnemyCount(int wave, int playerCount);
    float SpawnInterval(int wave);
    ZombieKind ChooseZombie(int wave, Random random);
}

public sealed class HordeMode : IGameMode
{
    public float IntermissionSeconds => 2;
    public int EnemyCount(int wave, int playerCount) => (10 + wave * 5) * playerCount;
    public float SpawnInterval(int wave) => Math.Max(0.08f, 0.35f - wave * 0.01f);
    public ZombieKind ChooseZombie(int wave, Random random) => ZombieKind.Walker;
}
public sealed class BruteMode : IGameMode
{
    public float IntermissionSeconds => 5;
    public int EnemyCount(int wave, int playerCount) => (2 + wave) * playerCount;
    public float SpawnInterval(int wave) => Math.Max(0.3f, 1.2f - wave * 0.04f);
    public ZombieKind ChooseZombie(int wave, Random random) => ZombieKind.Brute;
}
