namespace ZombieShooter.Core;

public abstract class WeaponDecorator(IWeapon inner) : IWeapon
{
    protected IWeapon Inner { get; } = inner;
    public virtual WeaponKind Kind => Inner.Kind;
    public virtual int Damage => Inner.Damage;
    public virtual float FireCooldown => Inner.FireCooldown;
    public virtual int Pellets => Inner.Pellets;
    public virtual int MagazineSize => Inner.MagazineSize;
    public virtual float ReloadTime => Inner.ReloadTime;
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