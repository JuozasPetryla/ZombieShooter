using System.Numerics;

namespace ZombieShooter.Core;

// Abstract Factory: each factory supplies all three related zombie products.
public interface IZombieFamilyFactory
{
    WalkerZombie CreateWalker(int id, Vector2 position, ZombieArchetype archetype);
    RunnerZombie CreateRunner(int id, Vector2 position, ZombieArchetype archetype);
    BruteZombie CreateBrute(int id, Vector2 position, ZombieArchetype archetype);
}

// Abstract products retain the shared combat behavior in Zombie.
public abstract class WalkerZombie(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public abstract class RunnerZombie(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public abstract class BruteZombie(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);

public sealed class StandardZombieFactory : IZombieFamilyFactory
{
    public WalkerZombie CreateWalker(int id, Vector2 position, ZombieArchetype archetype) => new Walker(id, position, archetype);
    public RunnerZombie CreateRunner(int id, Vector2 position, ZombieArchetype archetype) => new Runner(id, position, archetype);
    public BruteZombie CreateBrute(int id, Vector2 position, ZombieArchetype archetype) => new Brute(id, position, archetype);
}

public sealed class ArmoredZombieFactory : IZombieFamilyFactory
{
    public WalkerZombie CreateWalker(int id, Vector2 position, ZombieArchetype archetype) => new ArmoredWalker(id, position, archetype);
    public RunnerZombie CreateRunner(int id, Vector2 position, ZombieArchetype archetype) => new ArmoredRunner(id, position, archetype);
    public BruteZombie CreateBrute(int id, Vector2 position, ZombieArchetype archetype) => new ArmoredBrute(id, position, archetype);
}

// Armor adds 30 health to each spawned product without modifying its prototype.
public sealed class ArmoredWalker : WalkerZombie
{
    public ArmoredWalker(int id, Vector2 position, ZombieArchetype archetype) : base(id, position, archetype)
    {
        Health += 30;
    }
}
public sealed class ArmoredRunner : RunnerZombie
{
    public ArmoredRunner(int id, Vector2 position, ZombieArchetype archetype) : base(id, position, archetype)
    {
        Health += 30;
    }
}
public sealed class ArmoredBrute : BruteZombie
{
    public ArmoredBrute(int id, Vector2 position, ZombieArchetype archetype) : base(id, position, archetype)
    {
        Health += 30;
    }
}
