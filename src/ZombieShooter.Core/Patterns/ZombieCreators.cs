using System.Numerics;

namespace ZombieShooter.Core;

// Factory Method: Spawn() is the creator's shared operation; subclasses decide which product Create() makes.
public abstract class ZombieCreator
{
    public abstract Zombie Create(int id, Vector2 position, ZombieArchetype archetype);

    public Zombie Spawn(int id, Vector2 position, ZombieArchetype archetype, int wave)
    {
        archetype.Health += (wave - 1) * 5;
        archetype.Speed += Math.Min(wave - 1, 20) * 2;
        return Create(id, position, archetype);
    }
}
public sealed class Walker(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public sealed class Runner(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public sealed class Brute(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public sealed class WalkerCreator : ZombieCreator
{
    public override Zombie Create(int id, Vector2 position, ZombieArchetype archetype) => new Walker(id, position, archetype);
}
public sealed class RunnerCreator : ZombieCreator
{
    public override Zombie Create(int id, Vector2 position, ZombieArchetype archetype) => new Runner(id, position, archetype);
}
public sealed class BruteCreator : ZombieCreator
{
    public override Zombie Create(int id, Vector2 position, ZombieArchetype archetype) => new Brute(id, position, archetype);
}
