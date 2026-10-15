namespace ZombieShooter.Core;

// Bridge: notification types and output mechanisms evolve independently.
public interface INotificationOutput { void Write(string text); }
public sealed class HudNotificationOutput(Action<string> display) : INotificationOutput
{
    public void Write(string text) => display(text);
}
public sealed class ConsoleNotificationOutput : INotificationOutput
{
    public void Write(string text) => Console.WriteLine(text);
}
public abstract class GameNotification(INotificationOutput output)
{
    protected INotificationOutput Output { get; } = output;
    public abstract void Show(GameEvent message);
}
public sealed class WaveNotification(INotificationOutput output) : GameNotification(output)
{
    public override void Show(GameEvent message) => Output.Write(message.Name switch
    {
        "WaveStarted" => $"WAVE {message.Value}",
        "WeaponsUpgraded" => "WEAPONS UPGRADED",
        _ => "WAVE CLEARED"
    });
}
public sealed class PickupNotification(INotificationOutput output) : GameNotification(output)
{
    public override void Show(GameEvent message) => Output.Write(message.Value == (int)PickupKind.Health
        ? "HEALTH +35" : "SHOTGUN 15 SECONDS");
}
