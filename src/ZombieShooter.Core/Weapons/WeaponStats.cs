namespace ZombieShooter.Core;

// Immutable defaults and weapon data carried by snapshots.
public sealed record WeaponStats(WeaponKind Kind, int Damage, float FireCooldown, int Pellets,
    int MagazineSize, float ReloadTime)
{
    public static readonly WeaponStats Pistol = new(WeaponKind.Pistol, 35, 0.18f, 1, 12, 1.2f);
    public static readonly WeaponStats Shotgun = new(WeaponKind.Shotgun, 25, 0.5f, 5, 6, 1.8f);

    public Weapon ToWeapon() => new Weapon(Kind, Damage, FireCooldown, Pellets, MagazineSize, ReloadTime);

    public static WeaponStats From(IWeapon weapon) => new(weapon.Kind, weapon.Damage,
        weapon.FireCooldown, weapon.Pellets, weapon.MagazineSize, weapon.ReloadTime);
}
