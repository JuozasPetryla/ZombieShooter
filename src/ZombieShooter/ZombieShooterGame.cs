using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ZombieShooter.Core;
using ZombieShooter.Core.Commands;
using NVector2 = System.Numerics.Vector2;

namespace ZombieShooter;

public sealed class ZombieShooterGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _batch = null!;
    private Texture2D _pixel = null!;
    private GameSession _session = null!;
    private KeyboardState _previous;
    private GamePadState _previousPad;
    private bool _paused;
    private int _playerCount = 1;
    private string _notice = "SURVIVE THE HORDE";
    private float _noticeTime = 3;
    private readonly Dictionary<int, PlayerInput> _inputs = [];
    private IContentFactory _content = new NormalContentFactory();

    public ZombieShooterGame()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = 1280,
            PreferredBackBufferHeight = 720,
            SynchronizeWithVerticalRetrace = true
        };
        Content.RootDirectory = "Content";
        Window.Title = "Zombie Shooter - Survival";
        IsMouseVisible = true;
        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 60);
        Restart();
    }

    private void Restart()
    {
        if (_session is not null) _session.EventOccurred -= OnGameEvent;
        _session = new GameSession(_playerCount, Environment.TickCount, content: _content);
        _session.EventOccurred += OnGameEvent;
        _paused = false;
        _notice = _playerCount == 2 ? "LOCAL CO-OP" : "SURVIVE THE HORDE";
        _noticeTime = 3;
    }

    private void OnGameEvent(GameEvent message)
    {
        string? notice = message.Name switch
        {
            "WaveStarted" => $"WAVE {message.Value}",
            "WaveCleared" => "WAVE CLEARED",
            "WeaponsUpgraded" => "WEAPONS UPGRADED",
            "PickupCollected" => message.Value == (int)PickupKind.Health ? "HEALTH +35" : "SHOTGUN 15 SECONDS",
            _ => null
        };
        if (notice is not null) { _notice = notice; _noticeTime = 2; }
    }

    protected override void LoadContent()
    {
        _batch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        var keys = Keyboard.GetState();
        var pad = GamePad.GetState(PlayerIndex.One);

        bool PadPressed(Buttons button) => pad.IsConnected && _previousPad.IsConnected &&
            pad.IsButtonDown(button) && _previousPad.IsButtonUp(button);
        bool Pressed(Keys key) => keys.IsKeyDown(key) && _previous.IsKeyUp(key);

        if (Pressed(Keys.Escape)) Exit();
        if (Pressed(Keys.F2)) {
            _playerCount = _playerCount == 1 ? 2 : 1;
            Restart();
        } else if (Pressed(Keys.F3)) {
            _content = _content is NormalContentFactory ? new NightmareContentFactory() : new NormalContentFactory();
            Restart();
        } else if (Pressed(Keys.R)) Restart();

        if (Pressed(Keys.P)) _paused = !_paused;
        if (IsActive && !_paused)
        {
            _inputs.Clear();
            var mouse = Mouse.GetState();
            var p1 = _session.Players[0];
            if (Pressed(Keys.E)) _session.EnqueueCommand(new ReloadCommand(p1.Id));
            if (Pressed(Keys.D1)) _session.EnqueueCommand(new SelectWeaponCommand(p1.Id, WeaponKind.Pistol));
            if (Pressed(Keys.D2)) _session.EnqueueCommand(new SelectWeaponCommand(p1.Id, WeaponKind.Shotgun));
            _inputs[p1.Id] = new PlayerInput(
                new NVector2(Axis(keys, Keys.A, Keys.D), Axis(keys, Keys.W, Keys.S)),
                new NVector2(mouse.X, mouse.Y) - p1.Position,
                mouse.LeftButton == ButtonState.Pressed);
            if (_playerCount == 2)
            {
                var p2 = _session.Players[1];
                if (Pressed(Keys.O) || PadPressed(Buttons.X)) _session.EnqueueCommand(new ReloadCommand(p2.Id));
                if (Pressed(Keys.D9) || PadPressed(Buttons.LeftShoulder))
                    _session.EnqueueCommand(new SelectWeaponCommand(p2.Id, WeaponKind.Pistol));
                if (Pressed(Keys.D0) || PadPressed(Buttons.RightShoulder))
                    _session.EnqueueCommand(new SelectWeaponCommand(p2.Id, WeaponKind.Shotgun));
                var move = new NVector2(Axis(keys, Keys.Left, Keys.Right), Axis(keys, Keys.Up, Keys.Down));
                var aim = new NVector2(Axis(keys, Keys.J, Keys.L), Axis(keys, Keys.I, Keys.K));
                bool fire = keys.IsKeyDown(Keys.RightControl) || keys.IsKeyDown(Keys.Enter);
                if (pad.IsConnected)
                {
                    move = new NVector2(pad.ThumbSticks.Left.X, -pad.ThumbSticks.Left.Y);
                    aim = new NVector2(pad.ThumbSticks.Right.X, -pad.ThumbSticks.Right.Y);
                    fire = pad.Triggers.Right > 0.2f;
                }
                _inputs[_session.Players[1].Id] = new PlayerInput(move, aim, fire);
            }
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _session.Update(dt, _inputs);
            HighScores.Instance.Submit(_session.Score);
            _noticeTime -= dt;
        }
        _previous = keys;
        _previousPad = pad;
        base.Update(gameTime);
    }

    private static int Axis(KeyboardState keys, Keys negative, Keys positive)
        => (keys.IsKeyDown(positive) ? 1 : 0) - (keys.IsKeyDown(negative) ? 1 : 0);

    private void Box(float x, float y, float width, float height, Color color)
        => _batch.Draw(_pixel, new Rectangle((int)x, (int)y, (int)width, (int)height), color);
    private void Text(string text, float x, float y, Color color, int scale = 3)
        => PixelText.Draw(_batch, _pixel, text, new Vector2(x, y), color, scale);

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(18, 23, 27));
        _batch.Begin(samplerState: SamplerState.PointClamp);
        for (int x = 16; x < 1264; x += 40) Box(x, 66, 1, 638, new Color(28, 35, 39));
        for (int y = 66; y < 704; y += 40) Box(16, y, 1248, 1, new Color(28, 35, 39));
        Box(12, 62, 1256, 4, Color.SlateGray);
        Box(12, 704, 1256, 4, Color.SlateGray);
        Box(12, 62, 4, 646, Color.SlateGray);
        Box(1264, 62, 4, 646, Color.SlateGray);
        foreach (var pickup in _session.Pickups)
        {
            var p = pickup.Position;
            Box(p.X - 12, p.Y - 12, 24, 24, pickup.Kind == PickupKind.Health ? Color.DarkGreen : Color.DarkGoldenrod);
            Text(pickup.Kind == PickupKind.Health ? "+" : "S", p.X - 5, p.Y - 8, Color.White);
        }
        foreach (var zombie in _session.Zombies)
        {
            var p = zombie.Position;
            var r = zombie.Radius;
            var color = zombie.Kind switch { ZombieKind.Runner => Color.Olive, ZombieKind.Brute => Color.DarkOliveGreen, _ => Color.SeaGreen };
            Box(p.X - r + 3, p.Y - r + 5, r * 2, r * 2, Color.Black * 0.4f);
            Box(p.X - r, p.Y - r, r * 2, r * 2, color);
            Box(p.X - 7, p.Y - 5, 4, 4, Color.OrangeRed);
            Box(p.X + 3, p.Y - 5, 4, 4, Color.OrangeRed);
        }
        foreach (var player in _session.Players)
        {
            var p = player.Position;
            var color = player == _session.Players[0] ? Color.DeepSkyBlue : Color.Orange;
            if (!player.IsAlive) { Text("X", p.X - 6, p.Y - 6, color); continue; }
            Box(p.X - 13, p.Y - 13, 26, 26, color);
            var gun = new Vector2(p.X, p.Y);
            _batch.Draw(_pixel, gun, null, Color.LightGray, MathF.Atan2(player.Aim.Y, player.Aim.X),
                new Vector2(0, 0.5f), new Vector2(25, 7), SpriteEffects.None, 0);
            Box(p.X - 17, p.Y - 23, 34, 4, Color.DarkRed);
            Box(p.X - 17, p.Y - 23, 34 * player.Health / 100f, 4, Color.LimeGreen);
        }
        foreach (var bullet in _session.Bullets) Box(bullet.Position.X - 3, bullet.Position.Y - 3, 6, 6, Color.LightGoldenrodYellow);
        Box(0, 0, 1280, 60, new Color(12, 16, 20));
        Text($"WAVE {_session.Wave}  LEFT {_session.EnemiesRemaining}  SCORE {_session.Score}", 20, 12, Color.White);
        Text("WASD MOUSE  E RELOAD  1/2 WEAPON  P PAUSE  R RESET  F2 CO-OP", 20, 38, Color.LightSlateGray, 2);
        for (int i = 0; i < _session.Players.Count; i++)
        {
            var player = _session.Players[i];
            Text($"P{i + 1} HP {player.Health} {player.Weapon} {player.Ammo}/{player.MagazineSize} " +
                (player.IsReloading ? $"RELOAD {player.ReloadRemaining:0.0}" : player.WeaponTime > 0 ? $"SG {player.WeaponTime:0}" : ""), 730, 10 + i * 24,
                i == 0 ? Color.DeepSkyBlue : Color.Orange, 2);
        }
        if (_noticeTime > 0) Text(_notice, 440, 92, Color.Gold);
        if (_session.State == SessionState.Intermission)
            Text($"NEXT WAVE IN {Math.Max(1, (int)MathF.Ceiling(_session.IntermissionRemaining))}", 490, 125, Color.White, 2);
        if (_playerCount == 2)
            Text("P2 ARROWS IJKL ENTER FIRE  O RELOAD  9/0 WEAPON / PAD X LB RB", 190, 680, Color.Orange, 2);
        if (_paused || !IsActive || _session.State == SessionState.GameOver)
        {
            Box(300, 280, 680, 150, Color.Black * 0.9f);
            Text(_session.State == SessionState.GameOver ? "GAME OVER" : "PAUSED", 470, 310, Color.White, 5);
            Text(_session.State == SessionState.GameOver ? "R RESTART / F2 CHANGE PLAYERS" : "P RESUME / R RESTART", 420, 380, Color.LightGray, 3);
        }

        Text($"BEST {HighScores.Instance.Best}", 1060, 10, Color.Gold, 2);
        Text($"MODE {_content.Name}", 1060, 34, Color.LightSlateGray, 2);
        
        _batch.End();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _session.EventOccurred -= OnGameEvent;
        _pixel.Dispose();
        _batch.Dispose();
        base.UnloadContent();
    }
}
