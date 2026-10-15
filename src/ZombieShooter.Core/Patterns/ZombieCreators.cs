using System.Numerics;

namespace ZombieShooter.Core;

// Factory Method: the creator delegates the concrete product choice to subclasses.
public abstract class ZombieCreator
{
    public Zombie Create(int id, Vector2 position, ZombieArchetype archetype)
        => CreateProduct(id, position, archetype);
    protected abstract Zombie CreateProduct(int id, Vector2 position, ZombieArchetype archetype);
}
public sealed class Walker(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public sealed class Runner(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public sealed class Brute(int id, Vector2 position, ZombieArchetype archetype) : Zombie(id, position, archetype);
public sealed class WalkerCreator : ZombieCreator
{
    protected override Zombie CreateProduct(int id, Vector2 position, ZombieArchetype archetype) => new Walker(id, position, archetype);
}
public sealed class RunnerCreator : ZombieCreator
{
    protected override Zombie CreateProduct(int id, Vector2 position, ZombieArchetype archetype) => new Runner(id, position, archetype);
}
public sealed class BruteCreator : ZombieCreator
{
    protected override Zombie CreateProduct(int id, Vector2 position, ZombieArchetype archetype) => new Brute(id, position, archetype);
}
