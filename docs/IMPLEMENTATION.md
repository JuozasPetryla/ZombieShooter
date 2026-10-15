# Implementation status

The current specification, controls, acceptance criteria, UML sources, pattern inventory and demonstration instructions are in [PROJEKTO_APRASAS.md](PROJEKTO_APRASAS.md). The root README was intentionally preserved and contains historical statements that no longer match the implementation.

Implemented patterns: thread-safe Singleton; Factory Method with Walker/Runner/Brute products; two Abstract Factory families with three product types each; four game-mode Strategies; event-based Observer; two concrete Builders; deep/shallow Prototype; three stacked weapon Decorators; reversible Commands; keyboard/controller Adapters; GameFacade with three subsystems used by desktop and server; notification Bridge with two abstractions and two output implementations.

Projects:

- `ZombieShooter.Core`: simulation, patterns, snapshot DTOs, framed TCP transport, server and network client.
- `ZombieShooter`: MonoGame presentation, input adapters and HUD.
- `ZombieShooter.Server`: local authoritative two-player server executable.
- `ZombieShooter.Core.Tests`: headless regression, pattern and real loopback integration checks, Prototype report. Compiles the production HighScores source directly to test it without requiring a graphics runtime.

Run `dotnet build ZombieShooter.sln`, then `dotnet run --project tests/ZombieShooter.Core.Tests`.

The network prototype supports one local two-player match. It waits for both connections, pauses on disconnect and allows the vacant slot to reconnect. Start a fresh server and clients for a new network match. Client prediction, interpolation, accounts, persistence, obstacle maps, audio and multi-match hosting are outside the current implementation.

Manual validation still required: actual desktop input/rendering, controller hardware, screenshots and instructor demonstration. Historical submission dates and individual student authorship cannot be established by code changes.
