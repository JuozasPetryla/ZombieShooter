using System.Numerics;

namespace ZombieShooter.Core;

// Concrete component. The outer wrapper passes effective stats through the chain.
public class Weapon(WeaponKind kind, int damage, float fireCooldown, int pellets,
    int magazineSize, float reloadTime) : IWeapon
{
    public WeaponKind Kind { get; } = kind;
    public int Damage { get; } = damage;
    public float FireCooldown { get; } = fireCooldown;
    public int Pellets { get; } = pellets;
    public int MagazineSize { get; } = magazineSize;
    public float ReloadTime { get; } = reloadTime;

    public bool Shoot(Player player, EntityFactory factory, ICollection<Bullet> bullets)
        => Shoot(player, factory, bullets, null);
    public bool Reload(Player player) => Reload(player, null);
    public bool AdvanceReload(Player player, float dt) => AdvanceReload(player, dt, null);

    public bool Shoot(Player player, EntityFactory factory, ICollection<Bullet> bullets, WeaponStats? stats)
    {
        if ((stats is null && !ReferenceEquals(player.Gear, this)) || !player.IsAlive || player.FireCooldown > 0 ||
            player.IsReloading || player.Ammo <= 0) return false;
        stats ??= WeaponStats.From(this);
        player.ActionVersion++;
        if (stats.Kind == WeaponKind.Shotgun) player.ShotgunAmmo--;
        else player.PistolAmmo--;
        for (int i = 0; i < stats.Pellets; i++)
        {
            var direction = Vector2.Transform(player.Aim,
                Matrix3x2.CreateRotation((i - (stats.Pellets - 1) / 2f) * 0.12f));
            bullets.Add(factory.CreateBullet(player.Position + player.Aim * 20, direction, player.Id, stats.Damage));
        }
        player.FireCooldown = stats.FireCooldown;
        return true;
    }

    public bool Reload(Player player, WeaponStats? stats)
    {
        if ((stats is null && !ReferenceEquals(player.Gear, this)) || !player.IsAlive ||
            player.IsReloading || player.Ammo >= (stats?.MagazineSize ?? MagazineSize)) return false;
        player.ReloadRemaining = stats?.ReloadTime ?? ReloadTime;
        player.ActionVersion++;
        return true;
    }

    public bool AdvanceReload(Player player, float dt, WeaponStats? stats)
    {
        if ((stats is null && !ReferenceEquals(player.Gear, this)) || !player.IsAlive || !player.IsReloading) return false;
        player.ReloadRemaining = Math.Max(0, player.ReloadRemaining - dt);
        if (player.IsReloading) return false;
        player.ActionVersion++;
        if ((stats?.Kind ?? Kind) == WeaponKind.Pistol) player.PistolAmmo = stats?.MagazineSize ?? MagazineSize;
        else player.ShotgunAmmo = stats?.MagazineSize ?? MagazineSize;
        return true;
    }
}
