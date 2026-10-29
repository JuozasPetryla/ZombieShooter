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
    public IWeapon PistolGear {get; internal set; } = WeaponStats.Pistol.ToWeapon();
    public IWeapon ShotgunGear {get; internal set; } = WeaponStats.Shotgun.ToWeapon();
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
public sealed class EntityFactory
{
    private readonly IReadOnlyDictionary<ZombieKind, ZombieCreator> _zombieCreators;
    private int _nextId;
    private readonly Dictionary<ZombieKind, ZombieArchetype> _templates = new();

    // The builder makes one prototype per kind; spawns clone it.
    public EntityFactory(IZombieFamilyFactory? family = null)
    {
        family ??= new StandardZombieFactory();
        _zombieCreators = new Dictionary<ZombieKind, ZombieCreator>
        {
            [ZombieKind.Walker] = new WalkerCreator(family),
            [ZombieKind.Runner] = new RunnerCreator(family),
            [ZombieKind.Brute] = new BruteCreator(family)
        };
        var director = new ZombieDirector(new ZombieBuilder());
        foreach (var kind in Enum.GetValues<ZombieKind>())
            _templates[kind] = director.Construct(kind);
    }

    public Player CreatePlayer(Vector2 position) => new(++_nextId, position);

    public Zombie CreateZombie(Vector2 position, ZombieKind kind, int wave) =>
        _zombieCreators[kind].Spawn(++_nextId, position, _templates[kind].Clone(), wave);

    public Bullet CreateBullet(Vector2 position, Vector2 direction, int ownerId, int damage)
        => new(++_nextId, position, direction * 650, ownerId, damage);
    public Pickup CreatePickup(Vector2 position, PickupKind kind) => new(++_nextId, position, kind);
}
