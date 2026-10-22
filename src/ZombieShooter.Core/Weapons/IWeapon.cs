namespace ZombieShooter.Core;

public interface IWeapon
{
    WeaponKind Kind { get; }
    int Damage { get; }
    float FireCooldown { get; }
    int Pellets { get; }
    int MagazineSize { get; }
    float ReloadTime { get; }
}
