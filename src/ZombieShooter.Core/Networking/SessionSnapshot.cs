using System.Numerics;

namespace ZombieShooter.Core;

public sealed record PlayerSnapshot(int Id, float X, float Y, float AimX, float AimY, int Health,
    WeaponKind Weapon, float WeaponTime, int PistolAmmo, int ShotgunAmmo, float ReloadRemaining,
    WeaponStats Pistol, WeaponStats Shotgun);
public sealed record ZombieSnapshot(int Id, float X, float Y, ZombieKind Kind, int Health, float Speed,
    float Radius, int Damage, float Cooldown, int Reward);
public sealed record BulletSnapshot(int Id, float X, float Y, float Vx, float Vy, int OwnerId, int Damage);
public sealed record PickupSnapshot(int Id, float X, float Y, PickupKind Kind);
public sealed record SequencedEvent(long Sequence, GameEvent Event);
public sealed record SessionSnapshot(SessionState State, int Wave, int Score, int RemainingSpawns,
    float Intermission, PlayerSnapshot[] Players, ZombieSnapshot[] Zombies, BulletSnapshot[] Bullets,
    PickupSnapshot[] Pickups, SequencedEvent[] Events, int ConnectedPlayers);

public sealed partial class GameSession
{
    private long _lastRemoteEvent;
    public SessionSnapshot CaptureSnapshot(SequencedEvent[]? events = null, int connectedPlayers = 0) => new(
        State, Wave, Score, _remainingSpawns, IntermissionRemaining,
        _players.Select(p => new PlayerSnapshot(p.Id, p.Position.X, p.Position.Y, p.Aim.X, p.Aim.Y,
            p.Health, p.Weapon, p.WeaponTime, p.PistolAmmo, p.ShotgunAmmo, p.ReloadRemaining,
            WeaponStats.From(p.PistolGear), WeaponStats.From(p.ShotgunGear))).ToArray(),
        _zombies.Select(z => new ZombieSnapshot(z.Id, z.Position.X, z.Position.Y, z.Kind, z.Health,
            z.Speed, z.Radius, z.Attack.Damage, z.Attack.Cooldown, z.XpReward)).ToArray(),
        _bullets.Select(b => new BulletSnapshot(b.Id, b.Position.X, b.Position.Y, b.Velocity.X,
            b.Velocity.Y, b.OwnerId, b.Damage)).ToArray(),
        _pickups.Select(p => new PickupSnapshot(p.Id, p.Position.X, p.Position.Y, p.Kind)).ToArray(),
        events ?? [], connectedPlayers);

    // Rendering replica only. A remote client never advances this session locally.
    public void ApplySnapshot(SessionSnapshot snapshot)
    {
        State = snapshot.State;
        Wave = snapshot.Wave;
        Score = snapshot.Score;
        _remainingSpawns = snapshot.RemainingSpawns;
        IntermissionRemaining = snapshot.Intermission;
        _players.Clear();
        foreach (var p in snapshot.Players)
            _players.Add(new Player(p.Id, new(p.X, p.Y)) { Aim = new(p.AimX, p.AimY), Health = p.Health,
                Weapon = p.Weapon, WeaponTime = p.WeaponTime, PistolAmmo = p.PistolAmmo,
                ShotgunAmmo = p.ShotgunAmmo, ReloadRemaining = p.ReloadRemaining,
                PistolGear = p.Pistol.ToWeapon(), ShotgunGear = p.Shotgun.ToWeapon() });
        _zombies.Clear();
        foreach (var z in snapshot.Zombies)
            _zombies.Add(new Zombie(z.Id, new(z.X, z.Y), new ZombieArchetype { Kind = z.Kind,
                Health = z.Health, Speed = z.Speed, Radius = z.Radius, XpReward = z.Reward,
                Attack = new AttackProfile { Damage = z.Damage, Cooldown = z.Cooldown } }));
        _bullets.Clear();
        foreach (var b in snapshot.Bullets) _bullets.Add(new(b.Id, new(b.X, b.Y), new(b.Vx, b.Vy), b.OwnerId, b.Damage));
        _pickups.Clear();
        foreach (var p in snapshot.Pickups) _pickups.Add(new(p.Id, new(p.X, p.Y), p.Kind));
        foreach (var e in snapshot.Events)
            if (e.Sequence > _lastRemoteEvent) { _lastRemoteEvent = e.Sequence; Notify(e.Event); }
    }
}
