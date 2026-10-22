using System.Numerics;
using ZombieShooter.Core;
using ZombieShooter.Core.Commands;
using ZombieShooter.Core.Networking;
using ZombieShooter;

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
// Quantitative pattern requirements and regression checks.
var instances = new System.Collections.Concurrent.ConcurrentBag<HighScores>();
Parallel.For(0, 10000, i => { instances.Add(HighScores.Instance); HighScores.Instance.Submit(i); });
Check(instances.All(i => ReferenceEquals(i, HighScores.Instance)) && HighScores.Instance.Best == 9999,
    "Singleton construction and concurrent maximum updates are thread safe");
var factory = new EntityFactory();
Check(factory.CreateZombie(Vector2.Zero, ZombieKind.Walker, 1) is Walker &&
    factory.CreateZombie(Vector2.Zero, ZombieKind.Runner, 1) is Runner &&
    factory.CreateZombie(Vector2.Zero, ZombieKind.Brute, 1) is Brute,
    "Factory Method produces three concrete product classes");
var normal = new NormalContent();
var nightmare = new NightmareContent();
Check(normal.CreateGameMode() is NormalContent && normal.CreateDropPolicy() is NormalContent &&
    nightmare.CreateGameMode() is NightmareContent && nightmare.CreateDropPolicy() is NightmareContent &&
    nightmare.CreateEntityFactory() is NightmareEntityFactory, "Abstract factories provide three matching products per family");
foreach (var content in new IContentFactory[] { normal, nightmare })
{
    // Builder state must not leak into a new match or the stateless product roles.
    var rules = content.CreateGameMode();
    var drops = content.CreateDropPolicy();
    int expectedCount = rules.EnemyCount(1, 1);
    var expectedDrop = drops.Roll(new Random(42));
    ((ZombieBuilder)content).WithSpeed(float.NaN).WithHealth(1);
    var first = content.CreateEntityFactory().CreateZombie(Vector2.Zero, ZombieKind.Walker, 1);
    first.Attack.Damage = 999;
    var second = content.CreateEntityFactory().CreateZombie(Vector2.Zero, ZombieKind.Walker, 1);
    Check(ReferenceEquals(content, rules) && ReferenceEquals(content, drops) &&
        rules.EnemyCount(1, 1) == expectedCount && drops.Roll(new Random(42)) == expectedDrop &&
        second.Attack.Damage == 10 && second.Health == (content is NightmareContent ? 90 : 60) &&
        second.Speed == (content is NightmareContent ? 87.5f : 70f),
        $"{content.GetType().Name} shares stateless roles and isolates builder and session state");
}
Check(new IGameMode[] { new NormalContent(), new NightmareContent(), new HordeMode(), new BruteMode() }
    .Select(m => m.GetType()).Distinct().Count() == 4, "Four production strategies available");
Check(new HordeMode().EnemyCount(1, 1) > new NormalContent().EnemyCount(1, 1) &&
    new BruteMode().ChooseZombie(1, new Random(1)) == ZombieKind.Brute, "Additional strategies change gameplay");
// Exercise the shared desktop/server selection through actual wave spawning.
foreach (var preset in Enum.GetValues<GamePreset>())
{
    var game = GameFacade.CreatePreset(preset);
    var expected = preset switch
    {
        GamePreset.Normal => (Count: 7, Health: 60, Kind: ZombieKind.Walker),
        GamePreset.Nightmare => (Count: 10, Health: 90, Kind: ZombieKind.Walker),
        GamePreset.Horde => (Count: 15, Health: 60, Kind: ZombieKind.Walker),
        _ => (Count: 3, Health: 150, Kind: ZombieKind.Brute)
    };
    for (int i = 0; i < 60 && game.Session.Wave == 0; i++) game.Tick(0.1f);
    Check(game.Session.Wave == 1 && game.Session.EnemiesRemaining == expected.Count &&
        game.Session.Zombies[0].Kind == expected.Kind && game.Session.Zombies[0].Health == expected.Health,
        $"{preset} preset selects matching content and wave rules");
}
var standard = EntityFactory.BuildArchetype(ZombieKind.Walker, new NormalContent());
var armored = EntityFactory.BuildArchetype(ZombieKind.Walker, new NightmareContent());
Check(standard.Health == 60 && armored.Health == 90 && armored.Speed == 87.5f,
    "Director uses two concrete builders for distinct representations");
bool invalidBuild = false;
try { new NormalContent().WithSpeed(float.NaN).Build(); }
catch (InvalidOperationException) { invalidBuild = true; }
Check(invalidBuild, "Builder rejects non-finite properties");
var original = EntityFactory.BuildArchetype(ZombieKind.Walker);
var shallow = original.ShallowClone();
var deep = original.Clone();
Check(!ReferenceEquals(original, shallow) && ReferenceEquals(original.Attack, shallow.Attack) &&
    !ReferenceEquals(original.Attack, deep.Attack), "Prototype shallow shares and deep separates nested attack");
shallow.Attack.Damage = 77;
Check(original.Attack.Damage == 77 && deep.Attack.Damage == 10, "Nested mutation demonstrates clone distinction");
var spawned = factory.CreateZombie(Vector2.Zero, ZombieKind.Walker, 4);
spawned.Attack.Damage = 123;
var nextSpawn = factory.CreateZombie(Vector2.Zero, ZombieKind.Walker, 1);
Check(nextSpawn.Attack.Damage == 10 && nextSpawn.Health == 60 && nextSpawn.Speed == 70,
    "Factory clones templates before per-spawn mutation and wave scaling");
foreach (var kind in Enum.GetValues<ZombieKind>())
{
    var template = EntityFactory.BuildArchetype(kind);
    var tougher = EntityFactory.BuildArchetype(kind, new NightmareContent());
    Check(template.Kind == kind && tougher.Kind == kind &&
        tougher.Health == (int)(template.Health * 1.5f) && tougher.Speed == template.Speed * 1.25f &&
        tougher.Attack.Damage == template.Attack.Damage,
        $"Shared {kind} recipe preserves both content families");
}
IWeapon decorated = new MagazineSizeBonus(new FireRateBonus(new DamageBonus(WeaponStats.Pistol, 5), 0.5f), 2);
Check(decorated.Damage == 40 && Math.Abs(decorated.FireCooldown - 0.09f) < 0.0001f &&
    decorated.MagazineSize == 14, "Three decoration levels compose independently");
// Shared immutable defaults must remain unchanged when one player's weapon is decorated.
var weaponSession = new GameSession(2);
weaponSession.Players[0].PistolGear = decorated;
Check(weaponSession.Players[1].PistolGear.Damage == 35 && WeaponStats.Pistol.Damage == 35 &&
    WeaponStats.Pistol.MagazineSize == 12 && WeaponStats.Shotgun.Damage == 25,
    "Decorating one player leaves shared weapon defaults and the other player unchanged");
var weaponJson = System.Text.Json.JsonSerializer.Serialize(weaponSession.CaptureSnapshot());
var weaponReplica = new GameSession(2);
weaponReplica.ApplySnapshot(System.Text.Json.JsonSerializer.Deserialize<SessionSnapshot>(weaponJson)!);
Check(WeaponStats.From(weaponReplica.Players[0].PistolGear) == WeaponStats.From(decorated) &&
    WeaponStats.From(weaponReplica.Players[1].PistolGear) == WeaponStats.Pistol &&
    WeaponStats.From(weaponReplica.Players[0].ShotgunGear) == WeaponStats.Shotgun,
    "Weapon stats preserve all base and decorated properties across the JSON snapshot boundary");
Check(events.Count(e => e.Name == "WaveCleared") == 1, "WaveCleared is published exactly once");
Check(shooter.PistolGear.MagazineSize == 13 && shooter.PistolGear.Damage == 40,
    "Wave completion applies three actual weapon decorators");
var undo = new GameSession();
var up = undo.Players[0];
up.PistolAmmo = 2;
undo.EnqueueCommand(new ReloadCommand(up.Id));
undo.Update(dt, empty);
Check(undo.UndoLast(up.Id) && !up.IsReloading && up.PistolAmmo == 2, "Undo reload restores state without giving ammo");
Check(!undo.UndoLast(up.Id), "Undo is consumed once");
up.WeaponTime = 15;
undo.EnqueueCommand(new SelectWeaponCommand(up.Id, WeaponKind.Shotgun));
undo.Update(dt, empty);
undo.EnqueueCommand(new UndoCommand(up.Id));
undo.Update(dt, empty);
Check(up.Weapon == WeaponKind.Pistol, "Queued undo restores previous weapon");
undo.EnqueueCommand(new ReloadCommand(up.Id));
undo.Update(dt, empty);
for (int i = 0; i < 80; i++) undo.Update(dt, empty);
Check(!undo.UndoLast(up.Id) && up.PistolAmmo == 12, "Completed reload cannot be undone");
undo.EnqueueCommand(new SelectWeaponCommand(up.Id, WeaponKind.Shotgun));
undo.Update(dt, new Dictionary<int, PlayerInput> { [up.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(!undo.UndoLast(up.Id), "Undo rejected after firing changes state");
var textOutput = new List<string>();
INotificationOutput output = new RecordingOutput(textOutput);
new WaveNotification(output).Show(new("WaveStarted", Value: 3));
new PickupNotification(output).Show(new("PickupCollected", Value: (int)PickupKind.Health));
Check(textOutput.SequenceEqual(new[] { "WAVE 3", "HEALTH +35" }), "Both Bridge abstractions format notifications through the output contract");
var oldOut = Console.Out;
using (var capture = new StringWriter())
{
    Console.SetOut(capture);
    try
    {
        await using var outputServer = new GameServer(0);
        new WaveNotification(outputServer).Show(new("WaveStarted", Value: 3));
        new PickupNotification(outputServer).Show(new("PickupCollected", Value: (int)PickupKind.Health));
    }
    finally { Console.SetOut(oldOut); }
    Check(capture.ToString().Contains("WAVE 3") && capture.ToString().Contains("HEALTH +35"),
        "Server host is the console Bridge implementation for both abstractions");
}
var facade = new GameFacade(mode: new TestMode());
facade.SetInput(facade.Session.Players[0].Id, new(Vector2.UnitX, Vector2.UnitX, true));
facade.Tick(dt);
Check(facade.Session.Players[0].Position.X > 640 && facade.Session.Bullets.Count == 1,
    "Facade coordinates input, commands and simulation");
var replica = new GameSession();
int remoteEvents = 0;
replica.EventOccurred += _ => remoteEvents++;
var snapshot = facade.Snapshot();
replica.ApplySnapshot(snapshot);
replica.ApplySnapshot(snapshot);
Check(replica.Wave == facade.Session.Wave && replica.Players[0].Position == facade.Session.Players[0].Position &&
    remoteEvents == snapshot.Events.Length, "Snapshot reproduces state and deduplicates Observer events");

var pelletSession = new GameSession();
var pelletPlayer = pelletSession.Players[0];
pelletPlayer.PistolGear = new WeaponStats(WeaponKind.Pistol, 20, 0.2f, 3, 12, 1.2f);
pelletSession.Update(dt, new Dictionary<int, PlayerInput> { [pelletPlayer.Id] = new(Vector2.Zero, Vector2.UnitX, true) });
Check(pelletSession.Bullets.Count == 3, "Fire respects arbitrary weapon pellet count");
foreach (var kind in new[] { ZombieKind.Runner, ZombieKind.Brute })
{
    var rewards = new GameSession(mode: new TestMode(kind));
    rewards.Update(dt, empty);
    rewards.Zombies[0].Position = rewards.Players[0].Position + new Vector2(45, 0);
    rewards.Zombies[0].Health = 1;
    int expected = rewards.Zombies[0].XpReward;
    rewards.Update(0.1f, new Dictionary<int, PlayerInput> { [rewards.Players[0].Id] = new(Vector2.Zero, Vector2.UnitX, true) });
    Check(rewards.Score == expected, $"{kind} score uses archetype reward");
}
facade.Session.Players[0].PistolAmmo = 1;
facade.Queue(new ReloadCommand(facade.Session.Players[0].Id));
facade.ClearInputs();
facade.Tick(dt);
Check(!facade.Session.Players[0].IsReloading, "Disconnect cleanup discards queued commands");

// Actual loopback integration: two independently connected clients and one authority.
async Task Until(Func<bool> condition, string message)
{
    var deadline = DateTime.UtcNow.AddSeconds(5);
    while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(20);
    Check(condition(), message);
}
await using (var server = new GameServer(0))
{
    server.Start();
    using var c1 = await NetworkGameClient.ConnectAsync("127.0.0.1", server.Port);
    await Task.Delay(100);
    Check(c1.Latest.Snapshot.Wave == 0 && c1.Latest.Snapshot.ConnectedPlayers == 1, "Server waits for second client");
    var c2 = await NetworkGameClient.ConnectAsync("127.0.0.1", server.Port);
    try
    {
        await Until(() => c1.Latest.Snapshot.ConnectedPlayers == 2, "Both clients share one match");
        Check(c1.PlayerId != c2.PlayerId, "Server assigns distinct player identities");
        using (var extra = new System.Net.Sockets.TcpClient())
        {
            await extra.ConnectAsync("127.0.0.1", server.Port);
            using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            bool rejected = false;
            try { rejected = await extra.GetStream().ReadAsync(new byte[1], limit.Token) == 0; }
            catch (IOException) { rejected = true; }
            Check(rejected, "Third connection is rejected");
        }
        float initial = c1.Latest.Snapshot.Players[0].X;
        c1.SetInput(new(Vector2.UnitX, Vector2.UnitX, true));
        await Until(() => c2.Latest.Snapshot.Players[0].X > initial + 10, "Remote client observes authoritative movement");
        c1.SetInput(default);
        await Until(() => c1.Latest.Snapshot.Players[0].PistolAmmo < 12, "Server handles held fire");
        Check(c2.Latest.Snapshot.Players[1].X == 690 && c2.Latest.Snapshot.Players[1].PistolAmmo == 12,
            "Input affects only connection-owned player");
        c1.Queue(NetworkAction.Reload);
        await Until(() => c2.Latest.Snapshot.Players[0].ReloadRemaining > 0, "Discrete reload travels through network");
        c1.Queue(NetworkAction.Undo);
        await Until(() => c2.Latest.Snapshot.Players[0].ReloadRemaining == 0 &&
            c2.Latest.Snapshot.Players[0].PistolAmmo < 12, "Network undo cancels reload without ammo gain");
    }
    finally { c2.Dispose(); }
    await Until(() => c1.Latest.Snapshot.ConnectedPlayers == 1, "Disconnect returns match to waiting state");
    var pausedTime = c1.Latest.Snapshot.Intermission;
    await Task.Delay(100);
    Check(c1.Latest.Snapshot.Intermission == pausedTime, "Missing player pauses server simulation");
    using (var reconnected = await NetworkGameClient.ConnectAsync("127.0.0.1", server.Port))
    {
        Check(reconnected.PlayerId != c1.PlayerId, "Reconnect claims vacant player slot");
        await Until(() => c1.Latest.Snapshot.ConnectedPlayers == 2, "Reconnected slot is visible to remaining client");
    }
    await Until(() => c1.Latest.Snapshot.ConnectedPlayers == 1, "Reconnected client disconnects cleanly");
    using var malformed = new System.Net.Sockets.TcpClient();
    await malformed.ConnectAsync("127.0.0.1", server.Port);
    using var badLimit = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    await WireProtocol.ReadAsync<ServerFrame>(malformed.GetStream(), badLimit.Token);
    await WireProtocol.WriteAsync(malformed.GetStream(), new ClientFrame(0, 0, 1, 0, false,
        new[] { (NetworkAction)999 }), badLimit.Token);
    bool invalidRejected = false;
    try { invalidRejected = await malformed.GetStream().ReadAsync(new byte[1], badLimit.Token) == 0; }
    catch (IOException) { invalidRejected = true; }
    Check(invalidRejected && c1.Error is null, "Malformed action disconnects only offending client");
}

const int cloneIterations = 100000;
static (double Milliseconds, long Bytes) MeasureClones(ZombieArchetype template, bool deepCopy, int count)
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    long before = GC.GetAllocatedBytesForCurrentThread();
    ZombieArchetype? last = null;
    for (int i = 0; i < count; i++) last = deepCopy ? template.Clone() : template.ShallowClone();
    long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
    watch.Stop();
    GC.KeepAlive(last);
    return (watch.Elapsed.TotalMilliseconds, bytes);
}
var shallowCost = MeasureClones(original, false, cloneIterations);
var deepCost = MeasureClones(original, true, cloneIterations);
Console.WriteLine($"PROTOTYPE COST ({cloneIterations} copies; illustrative, not a benchmark)");
Console.WriteLine($"shallow: {shallowCost.Milliseconds:F3} ms, {shallowCost.Bytes} bytes");
Console.WriteLine($"deep: {deepCost.Milliseconds:F3} ms, {deepCost.Bytes} bytes");
Check(deepCost.Bytes > shallowCost.Bytes, "Deep clone allocates an independent nested object");

// Address samples are diagnostic values only, valid inside a no-GC region.
// They are not hashes or persistent identities and are never dereferenced.
if (GC.TryStartNoGCRegion(16 * 1024 * 1024))
{
    try
    {
        static string Address(object value) => "0x" + System.Runtime.CompilerServices.Unsafe.As<object, nuint>(ref value).ToString("X");
        Console.WriteLine("PROTOTYPE ADDRESS REPORT (one process, GC suspended)");
        Console.WriteLine($"original={Address(original)} attack={Address(original.Attack)} damage={original.Attack.Damage}");
        Console.WriteLine($"shallow ={Address(shallow)} attack={Address(shallow.Attack)} damage={shallow.Attack.Damage}");
        Console.WriteLine($"deep    ={Address(deep)} attack={Address(deep.Attack)} damage={deep.Attack.Damage}");
        GC.KeepAlive(original); GC.KeepAlive(shallow); GC.KeepAlive(deep);
    }
    finally { GC.EndNoGCRegion(); }
}
else throw new Exception("Could not suspend GC for address demonstration.");
Console.WriteLine($"{passed} checks passed.");

sealed class TestMode(ZombieKind kind = ZombieKind.Walker) : IGameMode
{
    public float IntermissionSeconds => 0;
    public int EnemyCount(int wave, int playerCount) => 1;
    public float SpawnInterval(int wave) => 1;
    public ZombieKind ChooseZombie(int wave, Random random) => kind;
}

sealed class RecordingOutput(List<string> messages) : INotificationOutput
{
    public void Write(string text) => messages.Add(text);
}
