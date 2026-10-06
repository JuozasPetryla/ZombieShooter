namespace ZombieShooter.Core;

/// <summary>
/// Builder pattern: provides a flexible solution to constructing complex objects step by step. It allows for the creation of different representations of an object using the same construction process, making it easier to manage and customize the creation of objects with many optional parameters or configurations.
/// </summary>
public sealed class ZombieBuilder
{
    private ZombieKind _kind = ZombieKind.Walker;
    private int _health = 60;
    private float _speed = 70f;
    private float _radius = 14f;
    private int _xpReward = 10;
    private int _attackDamage = 10;
    private float _attackCooldown = 0.65f;

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
        if (_speed <= 0) throw new InvalidOperationException("Speed must be greater than 0.");
        if (_radius <= 0) throw new InvalidOperationException("Radius must be greater than 0.");
        if (_xpReward < 0) throw new InvalidOperationException("XP reward cannot be negative.");
        if (_attackDamage < 0 || _attackCooldown <= 0) throw new InvalidOperationException("Attack damage must be non-negative and cooldown must be greater than 0.");

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