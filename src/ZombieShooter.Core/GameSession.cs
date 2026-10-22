using System.Numerics;
using ZombieShooter.Core.Commands;

namespace ZombieShooter.Core;

/// <summary>Device- and renderer-independent simulation. Call once per fixed tick on the authority.</summary>
public sealed partial class GameSession
{
    public const float Width = 1280;
    public const float Height = 720;
    private readonly EntityFactory _factory;
    private readonly Random _random;
    private readonly IGameMode _mode;
    private readonly IDropPolicy _drops;
    private readonly List<Player> _players = [];
    private readonly List<Zombie> _zombies = [];
    private readonly List<Bullet> _bullets = [];
    private readonly List<Pickup> _pickups = [];
    internal CommandDispatcher Commands { get; }
    private float _spawnTimer;
    private int _remainingSpawns;

    public IReadOnlyList<Player> Players { get; }
    public IReadOnlyList<Zombie> Zombies { get; }
    public IReadOnlyList<Bullet> Bullets { get; }
    public IReadOnlyList<Pickup> Pickups { get; }
    public SessionState State { get; private set; } = SessionState.Intermission;
    public int Wave { get; private set; }
    public int Score { get; private set; }
    public int EnemiesRemaining => _remainingSpawns + _zombies.Count;
    public float IntermissionRemaining { get; private set; }
    // Observer: consumers subscribe for UI/audio; combat knows nothing about presentation.
    public event Action<GameEvent>? EventOccurred;

    public GameSession(int playerCount = 1, int seed = 1, IGameMode? mode = null, IContentFactory? content = null)
    {
        if (playerCount is < 1 or > 2) throw new ArgumentOutOfRangeException(nameof(playerCount));
        Commands = new CommandDispatcher(this);
        content ??= new NormalContent();
        _factory = content.CreateEntityFactory();
        _mode = mode ?? content.CreateGameMode();
        _drops = content.CreateDropPolicy();
        _random = new Random(seed);
        for (int i = 0; i < playerCount; i++)
            _players.Add(_factory.CreatePlayer(new Vector2(Width / 2 + i * 50, Height / 2)));
        Players = _players.AsReadOnly();
        Zombies = _zombies.AsReadOnly();
        Bullets = _bullets.AsReadOnly();
        Pickups = _pickups.AsReadOnly();
        IntermissionRemaining = _mode.IntermissionSeconds;
    }

    /// <summary>Queue an intent for the next positive-duration tick, in arrival order.</summary>
    public bool EnqueueCommand(IGameCommand command) => Commands.Queue(command);

    // Receiver operations are also validated when commands are executed directly.
    public bool TryReload(int playerId)
    {
        var player = _players.Find(p => p.Id == playerId);
        if (State == SessionState.GameOver || player is null || !player.IsAlive ||
            player.IsReloading || player.Ammo == player.MagazineSize) return false;
        player.ReloadRemaining = player.Gear.ReloadTime;
        player.ActionVersion++;
        EventOccurred?.Invoke(new("ReloadStarted", player.Id));
        return true;
    }

    public bool TrySelectWeapon(int playerId, WeaponKind weapon)
    {
        var player = _players.Find(p => p.Id == playerId);
        if (State == SessionState.GameOver || player is null || !player.IsAlive ||
            !Enum.IsDefined(weapon) || player.Weapon == weapon ||
            (weapon == WeaponKind.Shotgun && player.WeaponTime <= 0)) return false;
        player.ReloadRemaining = 0;
        player.Weapon = weapon;
        player.ActionVersion++;
        EventOccurred?.Invoke(new("WeaponSelected", player.Id, (int)weapon));
        return true;
    }

    internal void DiscardPendingCommands() => Commands.Clear();

    public bool UndoLast(int playerId)
    {
        if (!Commands.UndoLast(playerId)) return false;
        EventOccurred?.Invoke(new("ActionUndone", playerId));
        return true;
    }

    public void Update(float dt, IReadOnlyDictionary<int, PlayerInput> inputs)
    {
        if (!float.IsFinite(dt) || dt < 0 || dt > 0.1f) throw new ArgumentOutOfRangeException(nameof(dt));
        // MonoGame may issue an initial update with zero elapsed time.
        if (dt == 0 || State == SessionState.GameOver) return;
        foreach (var player in _players.Where(p => p.IsAlive))
        {
            player.FireCooldown -= dt;
            player.HurtCooldown -= dt;
            player.WeaponTime = Math.Max(0, player.WeaponTime - dt);
            if (player.WeaponTime == 0 && player.Weapon == WeaponKind.Shotgun)
            {
                player.ActionVersion++;
                player.Weapon = WeaponKind.Pistol;
                player.ReloadRemaining = 0;
            }
            if (player.IsReloading)
            {
                player.ReloadRemaining = Math.Max(0, player.ReloadRemaining - dt);
                if (!player.IsReloading)
                {
                    player.ActionVersion++;
                    if (player.Weapon == WeaponKind.Pistol) player.PistolAmmo = player.MagazineSize;
                    else player.ShotgunAmmo = player.MagazineSize;
                    EventOccurred?.Invoke(new("ReloadCompleted", player.Id));
                }
            }
        }
        // Apply discrete actions before continuous input (including held fire).
        Commands.ExecutePending();
        foreach (var player in _players.Where(p => p.IsAlive))
        {
            inputs.TryGetValue(player.Id, out var input);
            var move = SafeDirection(input.Move, false);
            player.Position = Clamp(player.Position + move * 220 * dt, player.Radius);
            var aim = SafeDirection(input.Aim, true);
            if (aim != Vector2.Zero) player.Aim = aim;
            if (input.Fire && player.FireCooldown <= 0 && !player.IsReloading && player.Ammo > 0) Fire(player);
        }
        if (State == SessionState.Intermission)
        {
            IntermissionRemaining -= dt;
            if (IntermissionRemaining <= 0)
            {
                Wave++;
                _remainingSpawns = _mode.EnemyCount(Wave, _players.Count);
                _spawnTimer = 0;
                State = SessionState.Active;
                EventOccurred?.Invoke(new("WaveStarted", Value: Wave));
            }
        }
        if (State == SessionState.Active && _remainingSpawns > 0)
        {
            _spawnTimer -= dt;
            if (_spawnTimer <= 0)
            {
                SpawnZombie();
                _remainingSpawns--;
                _spawnTimer += _mode.SpawnInterval(Wave);
            }
        }
        UpdateZombies(dt);
        UpdateBullets(dt);
        UpdatePickups(dt);
        if (_players.All(p => !p.IsAlive))
        {
            State = SessionState.GameOver;
            Commands.Clear();
            EventOccurred?.Invoke(new("GameOver", Value: Score));
        }
        else if (State == SessionState.Active && EnemiesRemaining == 0)
        {
            State = SessionState.Intermission;
            IntermissionRemaining = _mode.IntermissionSeconds;
            // Upgrade surviving players' weapons at wave boundaries.
            foreach (var player in _players.Where(p => p.IsAlive)) UpgradeWeapons(player);
            EventOccurred?.Invoke(new("WaveCleared", Value: Wave));
            EventOccurred?.Invoke(new("WeaponsUpgraded", Value: Wave));
            // Co-op players return at wave boundaries if their teammate survives.
            foreach (var player in _players.Where(p => !p.IsAlive))
            {
                player.Health = 50;
                player.Position = new Vector2(Width / 2, Height / 2);
                player.HurtCooldown = 2;
            }
        }
    }

    private static Vector2 SafeDirection(Vector2 value, bool normalize)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) return Vector2.Zero;
        float length = value.Length();
        if (!float.IsFinite(length) || length < 0.001f) return Vector2.Zero;
        return normalize || length > 1 ? value / length : value;
    }

    private static Vector2 Clamp(Vector2 position, float radius) => Vector2.Clamp(position,
        new Vector2(radius + 16, radius + 66), new Vector2(Width - radius - 16, Height - radius - 16));

    private void Fire(Player player)
    {
        player.ActionVersion++;
        var gear = player.Gear;
        if (player.Weapon == WeaponKind.Shotgun) player.ShotgunAmmo--;
        else player.PistolAmmo--;
        for (int i = 0; i < gear.Pellets; i++)
        {
            var direction = Vector2.Transform(player.Aim, Matrix3x2.CreateRotation((i - (gear.Pellets - 1) / 2f) * 0.12f));
            _bullets.Add(_factory.CreateBullet(player.Position + player.Aim * 20, direction, player.Id, gear.Damage));
        }
        player.FireCooldown = gear.FireCooldown;
        EventOccurred?.Invoke(new("ShotFired", player.Id));
    }

    private void SpawnZombie()
    {
        float x = 32 + _random.NextSingle() * (Width - 64);
        float y = 82 + _random.NextSingle() * (Height - 114);
        var position = _random.Next(4) switch
        {
            0 => new Vector2(-24, y),
            1 => new Vector2(Width + 24, y),
            2 => new Vector2(x, 40),
            _ => new Vector2(x, Height + 24)
        };
        _zombies.Add(_factory.CreateZombie(position, _mode.ChooseZombie(Wave, _random), Wave));
    }

    private void UpdateZombies(float dt)
    {
        foreach (var zombie in _zombies)
        {
            var target = _players.Where(p => p.IsAlive).MinBy(p => Vector2.DistanceSquared(p.Position, zombie.Position));
            if (target is null) return;
            var direction = SafeDirection(target.Position - zombie.Position, true);
            zombie.Position += direction * zombie.Speed * dt;
            foreach (var player in _players.Where(p => p.IsAlive))
            {
                if (Vector2.Distance(player.Position, zombie.Position) > player.Radius + zombie.Radius || player.HurtCooldown > 0) continue;
                player.Health = Math.Max(0, player.Health - zombie.Attack.Damage);
                if (!player.IsAlive) player.ActionVersion++;
                player.HurtCooldown = zombie.Attack.Cooldown;
                EventOccurred?.Invoke(new("PlayerHurt", player.Id, player.Health));
            }
        }
    }

    private void UpdateBullets(float dt)
    {
        foreach (var bullet in _bullets)
        {
            var start = bullet.Position;
            var end = start + bullet.Velocity * dt;
            Zombie? hit = null;
            float nearest = float.PositiveInfinity;
            foreach (var zombie in _zombies.Where(z => z.Health > 0))
            {
                // Segment/circle intersection prevents fast bullets passing through enemies.
                var delta = end - start;
                var offset = start - zombie.Position;
                float a = delta.LengthSquared();
                float b = 2 * Vector2.Dot(offset, delta);
                float c = offset.LengthSquared() - MathF.Pow(zombie.Radius + bullet.Radius, 2);
                float discriminant = b * b - 4 * a * c;
                if (discriminant < 0) continue;
                float t = c <= 0 ? 0 : (-b - MathF.Sqrt(discriminant)) / (2 * a);
                if (t >= 0 && t <= 1 && t < nearest) { nearest = t; hit = zombie; }
            }
            bullet.Position = end;
            bullet.Lifetime -= dt;
            if (hit is null) continue;
            bullet.Lifetime = 0;
            hit.Health -= bullet.Damage;
            if (hit.Health > 0) continue;
            Score += hit.XpReward;
            EventOccurred?.Invoke(new("ZombieKilled", hit.Id, bullet.OwnerId));
            if (_drops.Roll(_random) is { } kind)
                _pickups.Add(_factory.CreatePickup(Clamp(hit.Position, 12), kind));
        }
        _bullets.RemoveAll(b => b.Lifetime <= 0 || b.Position.X < -50 || b.Position.X > Width + 50 || b.Position.Y < 0 || b.Position.Y > Height + 50);
        _zombies.RemoveAll(z => z.Health <= 0);
    }

    private void UpdatePickups(float dt)
    {
        foreach (var pickup in _pickups)
        {
            pickup.Lifetime -= dt;
            foreach (var player in _players.Where(p => p.IsAlive))
            {
                if (Vector2.Distance(player.Position, pickup.Position) > player.Radius + pickup.Radius) continue;
                if (pickup.Kind == PickupKind.Health)
                {
                    if (player.Health == 100) continue;
                    player.Health = Math.Min(100, player.Health + 35);
                }
                else
                {
                    player.ReloadRemaining = 0;
                    player.Weapon = WeaponKind.Shotgun;
                    player.WeaponTime = 15;
                    player.ShotgunAmmo = player.ShotgunGear.MagazineSize;
                }
                player.ActionVersion++;
                pickup.Lifetime = 0;
                EventOccurred?.Invoke(new("PickupCollected", player.Id, (int)pickup.Kind));
                break;
            }
        }
        _pickups.RemoveAll(p => p.Lifetime <= 0);
    }

    private static void UpgradeWeapons(Player player)
    {
        player.ActionVersion++;
        player.PistolGear = new MagazineSizeBonus(new FireRateBonus(new DamageBonus(player.PistolGear, 5), 0.95f), 1);
        player.ShotgunGear = new MagazineSizeBonus(new FireRateBonus(new DamageBonus(player.ShotgunGear, 3), 0.95f), 1);
    }
}
