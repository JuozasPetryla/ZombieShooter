using System.Numerics;

namespace ZombieShooter.Core;

// Factory Method: Spawn() is the creator's shared operation; subclasses decide which product Create() makes.
public abstract class ZombieCreator(IZombieFamilyFactory? family = null)
{
    protected IZombieFamilyFactory Family { get; } = family ?? new StandardZombieFactory();

    public abstract Zombie Create(int id, Vector2 position, ZombieArchetype archetype);

    public Zombie Spawn(int id, Vector2 position, ZombieArchetype archetype, int wave)
    {
        archetype.Health += (wave - 1) * 5;
        archetype.Speed += Math.Min(wave - 1, 20) * 2;
        return Create(id, position, archetype);
    }
}
public sealed class Walker(int id, Vector2 position, ZombieArchetype archetype) : WalkerZombie(id, position, archetype);
public sealed class Runner(int id, Vector2 position, ZombieArchetype archetype) : RunnerZombie(id, position, archetype);
public sealed class Brute(int id, Vector2 position, ZombieArchetype archetype) : BruteZombie(id, position, archetype);
public sealed class WalkerCreator(IZombieFamilyFactory? family = null) : ZombieCreator(family)
{
    public override Zombie Create(int id, Vector2 position, ZombieArchetype archetype) => Family.CreateWalker(id, position, archetype);
}
public sealed class RunnerCreator(IZombieFamilyFactory? family = null) : ZombieCreator(family)
{
    public override Zombie Create(int id, Vector2 position, ZombieArchetype archetype) => Family.CreateRunner(id, position, archetype);
}
public sealed class BruteCreator(IZombieFamilyFactory? family = null) : ZombieCreator(family)
{
    public override Zombie Create(int id, Vector2 position, ZombieArchetype archetype) => Family.CreateBrute(id, position, archetype);
}
