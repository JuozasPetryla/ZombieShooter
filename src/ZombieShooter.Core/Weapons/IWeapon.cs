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

public sealed class Pistol : IWeapon
{
    public WeaponKind Kind => WeaponKind.Pistol;
    public int Damage => 35;
    public float FireCooldown => 0.18f;
    public int Pellets => 1;
    public int MagazineSize => 12;
    public float ReloadTime => 1.2f;
}

public sealed class Shotgun : IWeapon
{
    public WeaponKind Kind => WeaponKind.Shotgun;
    public int Damage => 25;
    public float FireCooldown => 0.5f;
    public int Pellets => 5;
    public int MagazineSize => 6;
    public float ReloadTime => 1.8f;
}