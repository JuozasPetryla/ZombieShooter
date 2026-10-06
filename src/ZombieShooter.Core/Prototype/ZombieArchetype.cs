namespace ZombieShooter.Core;

/// <summary>
/// Prototype pattern: defines a method for creating objects by cloning existing instances, allowing for the creation of new objects without specifying their concrete classes.
/// This pattern is useful when the cost of creating a new object is expensive or complex, and when you want to avoid subclassing for every possible object type.
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IPrototype<T>
{
    T Clone();
}

public sealed class AttackProfile : IPrototype<AttackProfile>
{
    public int Damage { get; set; }
    public float Cooldown { get; set; }

    public AttackProfile Clone() => new AttackProfile { Damage = Damage, Cooldown = Cooldown };
}

public sealed class ZombieArchetype : IPrototype<ZombieArchetype>
{
    public ZombieKind Kind { get; set; }
    public int Health { get; set; }
    public float Speed { get; set; }
    public float Radius { get; set; }
    public int XpReward { get; set; }
    public AttackProfile Attack { get; set; } = new();

    public ZombieArchetype Clone() => new ()
    {
        Kind = Kind,
        Health = Health,
        Speed = Speed,
        Radius = Radius,
        XpReward = XpReward,
        Attack = Attack.Clone()
    };
}

public sealed class ArchetypeRegistry
{
    private readonly Dictionary<ZombieKind, ZombieArchetype> _templates = new();

    public void Register(ZombieArchetype archetype) => _templates[archetype.Kind] = archetype;

    public ZombieArchetype Get(ZombieKind kind) => _templates[kind].Clone();
}