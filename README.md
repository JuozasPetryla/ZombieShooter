# Zombie Shooter

A playable C# / MonoGame top-down survival shooter. The arena and characters use
procedural shapes and a built-in bitmap font, so no external art is required.

## Build and run

Requires .NET 8 SDK (or a compatible newer SDK), a graphical desktop with OpenGL,
and internet access for the first dependency restore. MonoGame 3.8.4.1 DesktopGL
supports Windows, Linux, and macOS; see its [platform setup instructions](https://docs.monogame.net/articles/getting_started/)
for native dependencies.

```sh
dotnet restore ZombieShooter.sln
dotnet tool restore
dotnet build ZombieShooter.sln --no-restore
dotnet run --project src/ZombieShooter --no-build
```

Run the headless regression checks without opening a window:

```sh
dotnet run --project tests/ZombieShooter.Core.Tests
```

This is a dependency-free console test harness: failed assertions exit with an
error. It checks movement, boundaries, invalid inputs, swept projectile hits,
wave progression, events, death, co-op revival, session isolation, cooldowns,
weapon expiration, both pickup effects, and command/reload/weapon-selection behavior.

## Controls and rules

| Action | Control |
| --- | --- |
| Player 1 move | WASD |
| Player 1 aim / fire | Mouse / hold left button |
| Player 1 reload / select pistol or shotgun | E / 1 or 2 |
| Player 2 move / aim / fire | Arrow keys / IJKL / Enter or right Ctrl |
| Player 2 reload / select pistol or shotgun | O / 9 or 0 |
| Player 2 with a connected controller | Left stick / right stick / right trigger |
| Player 2 controller reload / select pistol or shotgun | X / left or right shoulder |
| Pause | P (also freezes when the window loses focus) |
| Restart | R |
| Switch solo / two-player local co-op | F2 (starts a new session) |
| Exit | Escape |

Hold fire for continuous shooting. The pistol has 12 rounds and a 1.2-second
reload; the shotgun has 6 shells and a 1.8-second reload. Reserve ammo is unlimited,
but empty magazines require manual reload. Switching cancels reload and preserves
each magazine. The shotgun timer continues while the pistol is selected. Zombies pursue the
nearest living player. Contact costs 10 health with a short damage cooldown.
Kill all enemies to advance; waves get larger and spawn faster. Runners join
from wave 3, brutes from wave 5. Kills award shared score and sometimes drop a
health kit (+35, capped at 100) or a shotgun (15 seconds). Health kits remain
available when the touching player is already healthy. Items expire after 18
seconds. Co-op doubles the wave size; a fallen player returns with 50 health at
the next wave if their teammate survives. There is no friendly fire. The run
ends when every player falls.

## Design patterns

Use the earliest pattern that solves a real requirement; the numbered list is
not an implementation checklist.

| Pattern | Decision and implementation |
| --- | --- |
| 01 Singleton | Deliberately omitted. One current session does not require global access. Independent sessions are useful for tests, restarts, and future server matches. |
| 02 Factory | `EntityFactory` centralizes entity construction, session-local IDs, and zombie variants/stats. This is a simple factory, not the inheritance-based GoF Factory Method. |
| 03 Abstract Factory | Defer until modes/themes need matching families of enemies, weapons, and pickups. A single family does not yet justify it. |
| 04 Strategy | `IGameMode` and `SurvivalMode` define wave size, composition, and pacing; `GameSession` accepts a replacement strategy. |
| 05 Observer | `GameSession.EventOccurred` publishes combat and wave events. The MonoGame HUD subscribes without adding rendering dependencies to the core. |
| 09 Command | `IGameCommand`, `ReloadCommand`, and `SelectWeaponCommand` queue discrete actions; `GameSession` validates and executes them in FIFO order on the next tick. |

See [implementation status and pattern locations](docs/IMPLEMENTATION.md) for the
complete pattern inventory, Command participants, execution rules, and limitations.

Builder and Prototype become useful for configurable loadouts or content-defined
enemy templates. Decorator can represent stackable item effects when those rules
exist. Command currently handles local queued actions; timestamped input, replays,
and network transport are not implemented.

## Architecture and future work

- `src/ZombieShooter.Core`: pure .NET simulation with `System.Numerics`; owns
  entities, combat, pickups, waves, seeded randomness, and game rules. Public
  collections are read-only views and entity mutation is internal.
- `src/ZombieShooter`: MonoGame host, device input, procedural rendering, and HUD.
  Runs the simulation at a fixed 60 Hz. Pause is a host responsibility.
- `tests/ZombieShooter.Core.Tests`: headless checks with a small alternate mode.

Every player has a stable ID within a session. The host translates devices into
`PlayerInput` values keyed by ID. A future authoritative server can feed the same
boundary, but online multiplayer is **not implemented**: it still needs transport,
input validation/rate limiting, tick and match IDs, snapshots, interpolation,
join/disconnect handling, and potentially prediction/reconciliation. Seeded
randomness helps reproduce tests; this is not a cross-platform deterministic
lockstep guarantee.

The first prototype uses an open arena and direct zombie pursuit. Maps with
obstacles will need collision geometry and pathfinding; large hordes will need
spatial indexing. Broader modes may need lifecycle/victory policies beyond the
current wave strategy. Weapon definitions and richer item effects can be added
when the weapon roster grows. Art, audio, menus, saving, and online play remain
future work.
