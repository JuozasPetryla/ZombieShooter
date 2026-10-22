namespace ZombieShooter.Core;

// Prototype: clone reusable templates; nested attack data supports the deep/shallow demonstration.
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

    public ZombieArchetype ShallowClone() => (ZombieArchetype)MemberwiseClone();

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
