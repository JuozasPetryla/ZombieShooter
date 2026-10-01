# Implementation status

## Playable features

- Solo survival and two-player local co-op, keyboard/mouse and optional P2 controller.
- Fixed-tick movement, aiming, held fire, arena boundaries, swept bullet collisions.
- Walker, runner, and brute zombies pursuing the nearest living player.
- Increasing waves, intermissions, shared score, health/damage cooldown, game over.
- Health and temporary shotgun pickups, expiration, co-op revival at wave boundaries.
- Pistol and shotgun magazines, manual reload, and selection of owned weapons.
- Pause/focus freeze, restart, procedural rendering, bitmap HUD including ammo/reload.

## Pattern inventory

| Pattern | Status | Location and role |
| --- | --- | --- |
| 01 Singleton | Implemented | [HighScores](../src/ZombieShooter/HighScores.cs) keeps the best score across restarts in the host; matches still own their own state. |
| 02 Factory | Implemented as Simple Factory | [EntityFactory](../src/ZombieShooter.Core/Entities.cs) creates entities, assigns IDs, configures zombie stats. This is not GoF Factory Method. |
| 03 Abstract Factory | Implemented | [IContentFactory](../src/ZombieShooter.Core/ContentFactories.cs) creates matching EntityFactory + IGameMode families (Normal, Nightmare); selected by the host with F3 and injected into GameSession. |
| 04 Strategy | Implemented | [IGameMode / SurvivalMode](../src/ZombieShooter.Core/SurvivalMode.cs) choose wave size, pacing, and composition; injected into GameSession. |
| 05 Observer | Implemented with C# events | [GameSession](../src/ZombieShooter.Core/GameSession.cs) publishes EventOccurred; [ZombieShooterGame](../src/ZombieShooter/ZombieShooterGame.cs) subscribes for HUD notices. |
| 06 Builder | Not implemented | Possible future configurable matches/loadouts. |
| 07 Prototype | Not implemented | Possible future cloning of content templates. |
| 08 Decorator | Not implemented | Possible future stackable weapon behaviors. |
| 09 Command | Implemented | [IGameCommand, ReloadCommand, SelectWeaponCommand](../src/ZombieShooter.Core/Commands/IGameCommand.cs) encapsulate discrete player intent. |
| 10 Adapter | Not implemented as a formal pattern | The host translates device input directly; there are no interchangeable adapter classes. |
| 11 Facade | Not implemented as a separate pattern | Possible future multiplayer host/join interface. |
| 12 Bridge | Not implemented | Only needed if independent implementation families emerge. |

## Command participants and flow

- **Client:** `ZombieShooterGame.Update` creates immutable command records on key/button press edges, so holding reload/selection does not enqueue every frame.
- **Command interface:** `IGameCommand` exposes `PlayerId` and `Execute(GameSession)`.
- **Concrete commands:** `ReloadCommand` and `SelectWeaponCommand`; contain intent data, not input-device or rendering references.
- **Invoker:** `GameSession` owns a FIFO queue; `EnqueueCommand` accepts up to 64 pending commands. Its return value reports queue acceptance, not execution success.
- **Receiver:** `GameSession.TryReload` and `TrySelectWeapon` validate and change player state. `Execute` returns whether the action was applied. The local queue discards this result; there is no network acknowledgment protocol.

```text
Key/button press -> command -> session queue
                                    |
Next positive tick: timers -> FIFO execution -> movement/aim/fire -> combat
                                    |
                         validated receiver method
```

A zero-duration startup tick retains commands without executing them. Commands
are processed in arrival order; commands enqueued during execution wait until
the next tick. The queue is session-local and intended for one simulation thread.
Game over clears pending commands and rejects new ones. Restart replaces the
whole session. While paused or unfocused the host neither collects commands nor
advances simulation; it still updates previous device states to avoid stale presses.

Examples:

```csharp
session.EnqueueCommand(new SelectWeaponCommand(playerId, WeaponKind.Pistol));
session.EnqueueCommand(new ReloadCommand(playerId));
// The next Update selects the pistol, then starts its reload if necessary.
```

Movement, aiming, and held fire continue through `PlayerInput`. They are continuous
state, separate from the discrete command queue. Pause and restart remain host
controls. There is no undo stack, replay recorder, tick scheduling, serialization,
or online transport. A network integration must decode allowed command data,
validate connection ownership of player IDs, add match/tick/sequence IDs, handle
duplicate/late input, and marshal to the simulation thread. Do not deserialize
arbitrary executable command types from the network.

## Weapon behavior

| Weapon | Magazine | Reload duration | Access |
| --- | --- | --- | --- |
| Pistol | 12 rounds | 1.2 seconds | Always available |
| Shotgun | 6 shells (one shell per five-pellet shot) | 1.8 seconds | 15 seconds after pickup |

Reserve ammunition is unlimited. Empty magazines require a manual reload;
there is no automatic reload. Reload blocks firing but permits movement/aiming.
A full magazine, an existing reload, an unknown/dead player, or game over rejects
reload requests. Invalid/unowned weapon selections are rejected. Selecting the
already selected weapon is a no-op and does not cancel its reload.

Switching to another weapon cancels reload without granting ammunition; magazine
counts are retained separately. Switching preserves the fire cooldown. Shotgun
availability counts down even when the pistol is selected. Expiry switches an
equipped shotgun to pistol and cancels its reload. A shotgun pickup equips it,
refills its magazine, resets its 15-second timer, and cancels any current reload.
Timers run for living players only, preserving the existing co-op death behavior.
Commands are checked against state at execution time, after weapon expiry/timers.

Weapons still use `WeaponKind` and branches in `GameSession.Fire`; there is no
`IWeapon` hierarchy or Decorator implementation yet.

## Verification and remaining work

The console harness in [tests](../tests/ZombieShooter.Core.Tests/Program.cs) covers
the original gameplay plus deferred command execution, FIFO order, player targeting,
reload completion/rejection, ammo consumption, empty/reloading fire suppression,
switch cancellation, shotgun expiry, invalid commands, game over, and queue limits.
Run `dotnet run --project tests/ZombieShooter.Core.Tests`.

Manual desktop checks: fire to empty, press E, verify reload progress and restored
ammo; collect shotgun and switch with 1/2; cancel a reload by switching; try P2
O/9/0 and controller X/LB/RB; verify actions do not accumulate during pause.

Future work: weapon behavior classes, stackable item effects, obstacles/collision
and pathfinding, additional modes, network authority/snapshots/interpolation,
replay infrastructure, art/audio, menus, persistence, and large-horde optimization.
