namespace ZombieShooter.Core;

public class WeaponDecorator(IWeapon inner) : IWeapon
{
    protected IWeapon Inner { get; } = inner;
    public virtual WeaponKind Kind => Inner.Kind;
    public virtual int Damage => Inner.Damage;
    public virtual float FireCooldown => Inner.FireCooldown;
    public virtual int Pellets => Inner.Pellets;
    public virtual int MagazineSize => Inner.MagazineSize;
    public virtual float ReloadTime => Inner.ReloadTime;

    public bool Shoot(Player player, EntityFactory factory, ICollection<Bullet> bullets)
        => Shoot(player, factory, bullets, null);
    public bool Reload(Player player) => Reload(player, null);
    public bool AdvanceReload(Player player, float dt) => AdvanceReload(player, dt, null);

    public virtual bool Shoot(Player player, EntityFactory factory, ICollection<Bullet> bullets, WeaponStats? stats)
    {
        if (stats is null && !ReferenceEquals(player.Gear, this)) return false;
        return Inner.Shoot(player, factory, bullets, stats ?? WeaponStats.From(this));
    }
    public virtual bool Reload(Player player, WeaponStats? stats)
    {
        if (stats is null && !ReferenceEquals(player.Gear, this)) return false;
        return Inner.Reload(player, stats ?? WeaponStats.From(this));
    }
    public virtual bool AdvanceReload(Player player, float dt, WeaponStats? stats)
    {
        if (stats is null && !ReferenceEquals(player.Gear, this)) return false;
        return Inner.AdvanceReload(player, dt, stats ?? WeaponStats.From(this));
    }

}

public sealed class DamageBonus(IWeapon inner, int bonus) : WeaponDecorator(inner)
{
    public override int Damage => Inner.Damage + bonus;
}

public sealed class FireRateBonus(IWeapon inner, float multiplier) : WeaponDecorator(inner)
{
    public override float FireCooldown => Math.Max(0.05f, Inner.FireCooldown * multiplier);
}

public sealed class MagazineSizeBonus(IWeapon inner, int bonus) : WeaponDecorator(inner)
{
    public override int MagazineSize => Inner.MagazineSize + bonus;
}
