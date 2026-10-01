using System.Numerics;
using ZombieShooter.Core;
using ZombieShooter.Core.Commands;

int passed = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    passed++;
    Console.WriteLine($"PASS {message}");
}
const float dt = 1f / 60;
var empty = new Dictionary<int, PlayerInput>();
var session = new GameSession();
session.Update(0, empty);
Check(session.Wave == 0 && session.Bullets.Count == 0, "Zero-duration startup update is a no-op");
var player = session.Players[0];
var origin = player.Position;
session.Update(dt, new Dictionary<int, PlayerInput> { [player.Id] = new(new Vector2(1, 1), Vector2.Zero, false) });
Check(Math.Abs(Vector2.Distance(origin, player.Position) - 220 * dt) < 0.001f, "Diagonal movement is normalized");
for (int i = 0; i < 200; i++) session.Update(dt, new Dictionary<int, PlayerInput> { [player.Id] = new(new Vector2(-1, -1), Vector2.UnitX, false) });
Check(player.Position.X >= 31 && player.Position.Y >= 81, "Players remain inside the arena");
session.Update(dt, new Dictionary<int, PlayerInput> { [player.Id] = new(new Vector2(float.NaN, 0), new Vector2(float.PositiveInfinity, 0), false) });
Check(float.IsFinite(player.Position.X) && float.IsFinite(player.Aim.X), "Invalid input cannot poison simulation positions");

var combat = new GameSession(mode: new TestMode());
var events = new List<GameEvent>();
combat.EventOccurred += events.Add;
combat.Update(dt, empty);
Check(combat.Wave == 1 && combat.Zombies.Count == 1, "Mode strategy starts a wave and spawns enemies");
var zombie = combat.Zombies[0];
var shooter = combat.Players[0];
zombie.Position = shooter.Position + new Vector2(45, 0);
zombie.Health = 35;
combat.Update(0.1f, new Dictionary<int, PlayerInput> { [shooter.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(combat.Score == 10 && combat.Zombies.Count == 0, "Swept bullet hits an enemy crossed within a tick");
Check(combat.State == SessionState.Intermission && events.Any(e => e.Name == "WaveCleared"), "Wave ends only when all enemies are defeated");
Check(events.Count(e => e.Name == "ZombieKilled") == 1, "Kill observer fires once");
combat.Update(dt, empty);
Check(combat.Wave == 2, "Next wave starts after intermission");
shooter.Health = 10;
combat.Zombies[0].Position = shooter.Position;
combat.Update(dt, empty);
Check(combat.State == SessionState.GameOver, "Last player death ends session");
int score = combat.Score;
combat.Update(dt, empty);
Check(combat.Score == score && shooter.Health == 0, "Game-over simulation is frozen");

var coop = new GameSession(2, mode: new TestMode());
coop.Update(dt, empty);
coop.Players[0].Health = 0;
coop.Players[1].Position = new Vector2(640, 360);
coop.Zombies[0].Position = coop.Players[1].Position + new Vector2(45, 0);
coop.Zombies[0].Health = 35;
coop.Update(0.1f, new Dictionary<int, PlayerInput> { [coop.Players[1].Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(coop.State == SessionState.Intermission && coop.Players[0].Health == 50, "Surviving teammate revives partner at wave boundary");
var fresh = new GameSession();
Check(fresh.Wave == 0 && fresh.Score == 0 && fresh.Players[0].Health == 100 && fresh.Bullets.Count == 0, "Sessions do not share global state");

var shotgun = new GameSession();
shotgun.Players[0].Weapon = WeaponKind.Shotgun;
shotgun.Players[0].WeaponTime = 1;
var fire = new Dictionary<int, PlayerInput> { [shotgun.Players[0].Id] = new(Vector2.Zero, Vector2.UnitX, true) };
shotgun.Update(dt, fire);
Check(shotgun.Bullets.Count == 5, "Shotgun fires five pellets");
shotgun.Update(dt, fire);
Check(shotgun.Bullets.Count == 5, "Fire cooldown prevents shots every tick");
for (int i = 0; i < 100; i++) shotgun.Update(dt, empty);
Check(shotgun.Players[0].Weapon == WeaponKind.Pistol && shotgun.Bullets.Count == 0, "Temporary weapon and bullets expire");

// Exercise both pickup effects through the actual collection path.
foreach (PickupKind kind in Enum.GetValues<PickupKind>())
{
    bool collected = false;
    for (int seed = 0; seed < 100 && !collected; seed++)
    {
        var drops = new GameSession(seed: seed, mode: new TestMode());
        drops.Update(dt, empty);
        var p = drops.Players[0];
        p.Health = 40;
        drops.Zombies[0].Position = p.Position + new Vector2(45, 0);
        drops.Zombies[0].Health = 1;
        drops.Update(0.1f, new Dictionary<int, PlayerInput> { [p.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
        var pickup = drops.Pickups.FirstOrDefault(x => x.Kind == kind);
        if (pickup is null) continue;
        p.Position = pickup.Position;
        drops.Update(dt, empty);
        Check(drops.Pickups.Count == 0 && (kind == PickupKind.Health ? p.Health == 75 : p.Weapon == WeaponKind.Shotgun), $"{kind} pickup applies and is consumed");
        collected = true;
    }
    Check(collected, $"Seeded drops include {kind}");
}
var commands = new GameSession(2);
var cp = commands.Players[0];
var partner = commands.Players[1];
cp.PistolAmmo = 2;
Check(commands.EnqueueCommand(new ReloadCommand(cp.Id)) && !cp.IsReloading, "Enqueue defers execution");
commands.Update(0, empty);
Check(!cp.IsReloading, "Zero tick retains queued commands");
commands.Update(dt, empty);
Check(cp.IsReloading && !partner.IsReloading, "Reload targets only the requested player");
float reloadTime = cp.ReloadRemaining;
Check(!new ReloadCommand(cp.Id).Execute(commands) && cp.ReloadRemaining == reloadTime, "Repeated reload does not restart timer");
commands.Update(dt, new Dictionary<int, PlayerInput> { [cp.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(commands.Bullets.Count == 0, "Reload blocks held fire");
for (int i = 0; i < 80; i++) commands.Update(dt, empty);
Check(cp.PistolAmmo == 12 && !cp.IsReloading, "Reload completes and refills magazine");
Check(!new ReloadCommand(cp.Id).Execute(commands), "Full magazine cannot reload");
cp.PistolAmmo = 1;
commands.Update(dt, new Dictionary<int, PlayerInput> { [cp.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(cp.PistolAmmo == 0, "Pistol consumes one round per shot");
for (int i = 0; i < 20; i++) commands.Update(dt, new Dictionary<int, PlayerInput> { [cp.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(cp.PistolAmmo == 0 && commands.Bullets.Count == 1, "Empty magazine cannot fire");
Check(!new SelectWeaponCommand(cp.Id, WeaponKind.Shotgun).Execute(commands), "Unowned shotgun selection rejected");
Check(!new SelectWeaponCommand(cp.Id, (WeaponKind)99).Execute(commands) &&
    !new ReloadCommand(999).Execute(commands), "Invalid weapon and unknown player rejected");
cp.WeaponTime = 15;
commands.EnqueueCommand(new ReloadCommand(cp.Id));
commands.EnqueueCommand(new SelectWeaponCommand(cp.Id, WeaponKind.Shotgun));
commands.Update(dt, empty);
Check(cp.Weapon == WeaponKind.Shotgun && !cp.IsReloading && cp.PistolAmmo == 0, "FIFO reload then switch cancels reload without granting ammo");
commands.Update(dt, new Dictionary<int, PlayerInput> { [cp.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(cp.ShotgunAmmo == 5, "Shotgun consumes one shell for all five pellets");
commands.EnqueueCommand(new SelectWeaponCommand(cp.Id, WeaponKind.Pistol));
commands.EnqueueCommand(new ReloadCommand(cp.Id));
commands.Update(dt, empty);
Check(cp.Weapon == WeaponKind.Pistol && cp.IsReloading && cp.ShotgunAmmo == 5, "FIFO switch then reload targets newly selected weapon and preserves other ammo");
new SelectWeaponCommand(cp.Id, WeaponKind.Shotgun).Execute(commands);
new ReloadCommand(cp.Id).Execute(commands);
cp.WeaponTime = dt / 2;
commands.Update(dt, empty);
Check(cp.Weapon == WeaponKind.Pistol && !cp.IsReloading && cp.PistolAmmo == 0, "Shotgun expiry cancels reload without refilling pistol");
cp.Health = 0;
Check(!new ReloadCommand(cp.Id).Execute(commands) &&
    !new SelectWeaponCommand(cp.Id, WeaponKind.Shotgun).Execute(commands), "Dead players cannot execute actions");
Check(!combat.EnqueueCommand(new ReloadCommand(shooter.Id)) &&
    !new ReloadCommand(shooter.Id).Execute(combat), "Game over rejects queued and direct actions");
var capacity = new GameSession();
for (int i = 0; i < 64; i++) capacity.EnqueueCommand(new ReloadCommand(999));
Check(!capacity.EnqueueCommand(new ReloadCommand(999)), "Command queue is bounded");
capacity.Update(dt, empty);
Check(capacity.EnqueueCommand(new ReloadCommand(999)), "Invalid commands drain without blocking the queue");
Console.WriteLine($"{passed} checks passed.");

sealed class TestMode : IGameMode
{
    public float IntermissionSeconds => 0;
    public int EnemyCount(int wave, int playerCount) => 1;
    public float SpawnInterval(int wave) => 1;
    public ZombieKind ChooseZombie(int wave, Random random) => ZombieKind.Walker;
}
