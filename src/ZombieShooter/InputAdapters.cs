using Microsoft.Xna.Framework.Input;
using ZombieShooter.Core;
using Vector2 = System.Numerics.Vector2;

namespace ZombieShooter;

public interface IPlayerInputSource { PlayerInput Read(Vector2 playerPosition); }

// Adaptee has three operations; Adapter offers one domain-level operation.
public sealed class KeyboardMouseDevice
{
    public Vector2 Movement()
    {
        var k = Keyboard.GetState();
        return new((k.IsKeyDown(Keys.D) ? 1 : 0) - (k.IsKeyDown(Keys.A) ? 1 : 0),
            (k.IsKeyDown(Keys.S) ? 1 : 0) - (k.IsKeyDown(Keys.W) ? 1 : 0));
    }
    public Vector2 Cursor() { var m = Mouse.GetState(); return new(m.X, m.Y); }
    public bool Trigger() => Mouse.GetState().LeftButton == ButtonState.Pressed;
}
public sealed class KeyboardMouseAdapter(KeyboardMouseDevice device) : IPlayerInputSource
{
    public PlayerInput Read(Vector2 playerPosition) => new(device.Movement(), device.Cursor() - playerPosition, device.Trigger());
}
public sealed class ControllerDevice
{
    public Vector2 Move() { var p = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One); return new(p.ThumbSticks.Left.X, -p.ThumbSticks.Left.Y); }
    public Vector2 Aim() { var p = GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One); return new(p.ThumbSticks.Right.X, -p.ThumbSticks.Right.Y); }
    public float Trigger() => GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One).Triggers.Right;
}
public sealed class ControllerAdapter(ControllerDevice device) : IPlayerInputSource
{
    public PlayerInput Read(Vector2 playerPosition) => new(device.Move(), device.Aim(), device.Trigger() > 0.2f);
}
