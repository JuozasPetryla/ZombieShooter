using System.Numerics;

namespace ZombieShooter.Core;

public enum WeaponKind { Pistol, Shotgun }
public enum ZombieKind { Walker, Runner, Brute }
public enum PickupKind { Health, Shotgun }
public enum SessionState { Intermission, Active, GameOver }

// Input is data: local devices and a future network adapter share this boundary.
public readonly record struct PlayerInput(Vector2 Move, Vector2 Aim, bool Fire);
public readonly record struct GameEvent(string Name, int EntityId = 0, int Value = 0);

public abstract class Entity(int id, Vector2 position, float radius)
{
    public int Id { get; } = id;
    public Vector2 Position { get; internal set; } = position;
    public float Radius { get; } = radius;
}

public sealed class Player(int id, Vector2 position) : Entity(id, position, 15)
{
    public int Health { get; internal set; } = 100;
    public bool IsAlive => Health > 0;
    public Vector2 Aim { get; internal set; } = Vector2.UnitX;
    public WeaponKind Weapon { get; internal set; }
    public float WeaponTime { get; internal set; }
    public IWeapon PistolGear {get; internal set; } = WeaponStats.Pistol;
    public IWeapon ShotgunGear {get; internal set; } = WeaponStats.Shotgun;
    public IWeapon Gear => Weapon == WeaponKind.Pistol ? PistolGear : ShotgunGear;
    public int PistolAmmo { get; internal set; } = 12;
    public int ShotgunAmmo { get; internal set; } = 6;
    public int Ammo => Weapon == WeaponKind.Pistol ? PistolAmmo : ShotgunAmmo;
    public int MagazineSize => Gear.MagazineSize;
    public float ReloadRemaining { get; internal set; }
    public bool IsReloading => ReloadRemaining > 0;
    internal long ActionVersion;
    internal float FireCooldown;
    internal float HurtCooldown;
}

public class Zombie(int id, Vector2 position, ZombieArchetype archetype)
    : Entity(id, position, archetype.Radius)
{
    public ZombieKind Kind { get; } = archetype.Kind;
    public int Health { get; internal set; } = archetype.Health;
    public float Speed { get; } = archetype.Speed;
    public AttackProfile Attack { get; } = archetype.Attack;
    public int XpReward { get; } = archetype.XpReward;
}

public sealed class Bullet(int id, Vector2 position, Vector2 velocity, int ownerId, int damage)
    : Entity(id, position, 3)
{
    public Vector2 Velocity { get; } = velocity;
    public int OwnerId { get; } = ownerId;
    public int Damage { get; } = damage;
    public float Lifetime { get; internal set; } = 1.4f;
}

public sealed class Pickup(int id, Vector2 position, PickupKind kind) : Entity(id, position, 12)
{
    public PickupKind Kind { get; } = kind;
    public float Lifetime { get; internal set; } = 18;
}

// Factory centralizes identities and construction; it belongs to one session.
public class EntityFactory
{
    private static readonly IReadOnlyDictionary<ZombieKind, ZombieCreator> ZombieCreators =
        new Dictionary<ZombieKind, ZombieCreator>
        {
            [ZombieKind.Walker] = new WalkerCreator(),
            [ZombieKind.Runner] = new RunnerCreator(),
            [ZombieKind.Brute] = new BruteCreator()
        };
    private int _nextId;
    private readonly Dictionary<ZombieKind, ZombieArchetype> _templates = new();

    public EntityFactory(ZombieBuilder? builder = null)
    {
        builder ??= new NormalContent();
        foreach (var kind in Enum.GetValues<ZombieKind>())
            _templates[kind] = BuildArchetype(kind, builder);
    }
    // Builder director role: both content families use the same construction recipes.
    public static ZombieArchetype BuildArchetype(ZombieKind kind, ZombieBuilder? builder = null)
    {
        builder ??= new NormalContent();
        return kind switch
        {
            ZombieKind.Walker => builder.OfKind(kind).WithHealth(60).WithSpeed(70f)
                .WithRadius(14f).WithXpReward(10).WithAttack(10, 0.65f).Build(),
            ZombieKind.Runner => builder.OfKind(kind).WithHealth(35).WithSpeed(125f)
                .WithRadius(14f).WithXpReward(15).WithAttack(8, 0.5f).Build(),
            ZombieKind.Brute => builder.OfKind(kind).WithHealth(150).WithSpeed(48f)
                .WithRadius(22f).WithXpReward(25).WithAttack(20, 1.2f).Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public Player CreatePlayer(Vector2 position) => new(++_nextId, position);

    public Zombie CreateZombie(Vector2 position, ZombieKind kind, int wave)
    {
        var archetype = _templates[kind].Clone();
        archetype.Health += (wave - 1) * 5;
        archetype.Speed += Math.Min(wave - 1, 20) * 2;
        return ZombieCreators[kind].Create(++_nextId, position, archetype);
    }

    public Bullet CreateBullet(Vector2 position, Vector2 direction, int ownerId, int damage)
        => new(++_nextId, position, direction * 650, ownerId, damage);
    public Pickup CreatePickup(Vector2 position, PickupKind kind) => new(++_nextId, position, kind);
}
