namespace ZombieShooter.Core;

// Builder: knows HOW each part is made; Reset() starts a new product.
public sealed class ZombieBuilder
{
    private ZombieKind _kind;
    private int _health;
    private float _speed;
    private float _radius;
    private int _xpReward;
    private int _attackDamage;
    private float _attackCooldown;

    public ZombieBuilder Reset()
    {
        _kind = ZombieKind.Walker;
        _health = 0;
        _speed = 0;
        _radius = 0;
        _xpReward = 0;
        _attackDamage = 0;
        _attackCooldown = 0;
        return this;
    }

    public ZombieBuilder OfKind(ZombieKind kind) { _kind = kind; return this; }
    public ZombieBuilder WithHealth(int health) { _health = health; return this; }
    public ZombieBuilder WithSpeed(float speed) { _speed = speed; return this; }
    public ZombieBuilder WithRadius(float radius) { _radius = radius; return this; }
    public ZombieBuilder WithXpReward(int xpReward) { _xpReward = xpReward; return this; }

    public ZombieBuilder WithAttack(int attackDamage, float attackCooldown)
    {
        _attackDamage = attackDamage;
        _attackCooldown = attackCooldown;
        return this;
    }

    public ZombieArchetype Build()
    {
        if (_health <= 0) throw new InvalidOperationException("Health must be greater than 0.");
        if (!float.IsFinite(_speed) || _speed <= 0) throw new InvalidOperationException("Speed must be greater than 0.");
        if (!float.IsFinite(_radius) || _radius <= 0) throw new InvalidOperationException("Radius must be greater than 0.");
        if (_xpReward < 0) throw new InvalidOperationException("XP reward cannot be negative.");
        if (_attackDamage < 0 || !float.IsFinite(_attackCooldown) || _attackCooldown <= 0) throw new InvalidOperationException("Attack damage must be non-negative and cooldown must be greater than 0.");

        return new ZombieArchetype
        {
            Kind = _kind,
            Health = _health,
            Speed = _speed,
            Radius = _radius,
            XpReward = _xpReward,
            Attack = new AttackProfile { Damage = _attackDamage, Cooldown = _attackCooldown }
        };
    }
}

// Director: knows the ORDER of steps (the recipe) for each zombie kind.
public sealed class ZombieDirector(ZombieBuilder builder)
{
    public ZombieArchetype Construct(ZombieKind kind) => kind switch
    {
        ZombieKind.Walker => builder.Reset().OfKind(kind).WithHealth(60).WithSpeed(70f)
            .WithRadius(14f).WithXpReward(10).WithAttack(10, 0.65f).Build(),
        ZombieKind.Runner => builder.Reset().OfKind(kind).WithHealth(35).WithSpeed(125f)
            .WithRadius(14f).WithXpReward(15).WithAttack(8, 0.5f).Build(),
        ZombieKind.Brute => builder.Reset().OfKind(kind).WithHealth(150).WithSpeed(48f)
            .WithRadius(22f).WithXpReward(25).WithAttack(20, 1.2f).Build(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
