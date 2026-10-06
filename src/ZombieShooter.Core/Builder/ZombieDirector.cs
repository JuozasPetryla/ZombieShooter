namespace ZombieShooter.Core;

public static class ZombieDirector
{
    public static ZombieArchetype Walker() => new ZombieBuilder()
        .OfKind(ZombieKind.Walker)
        .WithHealth(60)
        .WithSpeed(70f)
        .WithRadius(14f)
        .WithXpReward(10)
        .WithAttack(10, 0.65f)
        .Build();

    public static ZombieArchetype Runner() => new ZombieBuilder()
        .OfKind(ZombieKind.Runner)
        .WithHealth(35)
        .WithSpeed(125f)
        .WithRadius(14f)
        .WithXpReward(15)
        .WithAttack(8, 0.5f)
        .Build();

    public static ZombieArchetype Brute() => new ZombieBuilder()
        .OfKind(ZombieKind.Brute)
        .WithHealth(150)
        .WithSpeed(48f)
        .WithRadius(22f)
        .WithXpReward(25)
        .WithAttack(20, 1.2f)
        .Build();
}