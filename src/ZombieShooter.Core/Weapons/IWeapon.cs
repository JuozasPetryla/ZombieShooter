namespace ZombieShooter.Core;

public interface IWeapon
{
    WeaponKind Kind { get; }
    int Damage { get; }
    float FireCooldown { get; }
    int Pellets { get; }
    int MagazineSize { get; }
    float ReloadTime { get; }
    bool Shoot(Player player, EntityFactory factory, ICollection<Bullet> bullets);
    bool Reload(Player player);
    bool AdvanceReload(Player player, float dt);
    // Overloads carry effective stats through the wrapper chain.
    bool Shoot(Player player, EntityFactory factory, ICollection<Bullet> bullets, WeaponStats? stats);
    bool Reload(Player player, WeaponStats? stats);
    bool AdvanceReload(Player player, float dt, WeaponStats? stats);
}
