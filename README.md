# Zombie Shooter

C# / MonoGame top-down survival shooter with solo, local co-op, and a local
TCP server for two clients. Graphics and text are drawn procedurally; no asset
pipeline or external artwork is needed.

## Run

Requires .NET 8 (or a compatible newer SDK) and an OpenGL desktop. Restore needs
NuGet access or cached packages.

```sh
dotnet build ZombieShooter.sln -m:1
dotnet run --project src/ZombieShooter --no-build
```

If mouse aiming is stuck on Linux, prefix the run command with `SDL_VIDEODRIVER=x11`.
Run headless regression checks, including local TCP integration tests:

```sh
dotnet run --project tests/ZombieShooter.Core.Tests
```

For network play, start the server, then run the client command in two terminals:

```sh
dotnet run --project src/ZombieShooter.Server -- 7777 normal
dotnet run --project src/ZombieShooter -- --connect 127.0.0.1 7777
```

Server modes: `normal`, `horde`, `brute`. The server listens on
loopback, waits for both players, and pauses when either disconnects. A replacement
client takes the vacant slot. Restart the server and clients for a new match.

## Controls

| Action | P1 / network player | Local P2 |
|---|---|---|
| Move | WASD | Arrows / left stick |
| Aim / fire | Mouse / left button | IJKL / Enter or right Ctrl; right stick / trigger |
| Reload | E | O / controller X |
| Pistol / shotgun | 1 / 2 | 9 / 0; LB / RB |
| Undo last valid command | Z | N |
| Pause / restart | P / R | Shared |
| Solo / co-op | F2 | Starts a new local session |
| Mode | F3 | Normal → Horde → Brute; starts a new session |
| Exit | Escape | Shared |

P, R, F2 and F3 apply only to local games. In network play the server chooses the
mode. Losing focus pauses local play and clears outgoing network input.

Kill each wave, collect health and temporary shotguns, and survive. Reload is
manual; reserve ammo is unlimited. Surviving players receive three stacked weapon
upgrades after each wave; a fallen partner revives if the other survives. The
match ends when everyone dies. The high score lasts for the process lifetime.

## Code and coursework

- `ZombieShooter.Core`: simulation, design patterns, snapshots and TCP networking.
- `ZombieShooter`: desktop input, drawing and HUD.
- `ZombieShooter.Server`: server entry point.
- `ZombieShooter.Core.Tests`: regression tests and the Prototype experiment.

[Requirements and pattern mapping](docs/PROJEKTO_APRASAS.md) record the coursework
criteria and current implementation. Each pattern follows the GoF structure used
in the course slides.

`ZombieDirector` runs one recipe per zombie kind on a `ZombieBuilder`; the
resulting archetypes are Prototype templates cloned on each spawn. Modes differ
only by wave Strategy (`NormalMode`, `HordeMode`, `BruteMode`), and
`GameFacade.CreatePreset` exposes Normal, Horde and Brute to desktop and server.

The desktop game and server also implement `INotificationOutput`: the same hosts
are Facade clients, Observer subscribers, and the two Bridge output implementations.
`WeaponStats` supplies immutable pistol/shotgun defaults and snapshot weapon data;
`IWeapon` is the common contract for `Weapon` and `WeaponDecorator`.
The three bonuses wrap `IWeapon`; operations delegate through every layer.
`Shoot(player, factory, bullets)` and `Reload(player)` receive context explicitly;
additional overloads carry effective stats through the chain so no bonus is lost.
Player retains ammo and timers. Commands receive the session in `Execute(session)`
and `Undo(session)`; undo is restricted to the session of successful execution.
[Diagram alignment notes](docs/DIAGRAM_ALIGNMENT.md) describe necessary corrections
to the supplied full diagram and the retained runtime details.

[Current class diagram](docs/uml/01-current-classes.svg)
([PlantUML](docs/uml/01-current-classes.puml)) ·
[Use cases](docs/uml/00-use-cases.puml) ·
[Observer sequence (PlantUML)](docs/uml/02-observer-sequence.puml) ·
[Prototype experiment](docs/PROTOTYPE_REPORT.md)

The class diagram includes all source-declared types under `src/`, with fields,
properties, events, constructors, and methods at every visibility level. Records,
enums, transport types, and drawing helpers are included. Inherited members are
shown on their base types; partial classes are merged. C# types, parameter defaults,
accessor visibility, and member modifiers are retained. Captured primary-constructor
parameters are marked explicitly. Arrows focus on inheritance, interface
implementations, ownership, stored-object references, and selected pattern
interactions. Parameter/return-type dependencies and enum attribute arrows are
omitted because the member signatures already show those types. All classes and
members remain in the diagram. Associations show multiplicities at both ends,
including optional references and collection bounds; inheritance and dependencies
have no multiplicities. Multiplicities describe initialized valid states, with
repeated field slots (such as pistol/shotgun) counted separately. The SVG is large and should be viewed with zoom.

Compiler-generated record methods/backing fields, implicit constructors, top-level
entry-point statements, local variables/functions, and test code are outside this
class model. External base types are shown without their members.

Regenerate declarations and structural links with the SDK-based generator (no
additional NuGet packages), then render with PlantUML. Explicit ownership and
behavioral links are maintained in `docs/uml/class-relationships.puml.inc`:

```sh
dotnet run --project tools/UmlGenerator
python3 tools/render-uml.py
```

For a standalone PlantUML jar, use `python3 tools/render-uml.py --jar /path/to/plantuml.jar`.
The renderer adds a solid white SVG canvas so text and arrows remain readable in
dark image viewers.

Desktop rendering and physical controller input require manual validation.
