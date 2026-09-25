# Zombie Shooter

Minimal C# / MonoGame scaffold for a 2D top-down zombie shooter. It opens an
empty, dark 1280×720 window. Press **Escape** or close the window to exit.
There are no gameplay systems or assets yet.

## Requirements

- .NET 8 SDK (or a newer SDK capable of targeting .NET 8).
- A graphical desktop with OpenGL support.
- Internet access for the first NuGet restore.

The project uses MonoGame 3.8.4.1 and its DesktopGL backend for Windows, Linux,
and macOS. See the [official platform setup instructions](https://docs.monogame.net/articles/getting_started/)
for OS-specific native dependencies.

## Build and run

Run from the repository root:

```sh
dotnet restore ZombieShooter.sln
dotnet tool restore
dotnet build ZombieShooter.sln --no-restore
dotnet run --project src/ZombieShooter
```

You can also open `ZombieShooter.sln` in an IDE with C# support.

## Layout

```text
ZombieShooter.sln
.config/dotnet-tools.json   Local MonoGame content builder tool
src/ZombieShooter/
  ZombieShooter.csproj      Framework and content builder dependencies
  Program.cs               Application entry point
  ZombieShooterGame.cs      Window setup and the basic update/draw loop
  Content/Content.mgcb      Empty MonoGame content pipeline
```

When assets are introduced, register them in `Content/Content.mgcb`; the content
builder runs as part of the project build. Scene management, rendering helpers,
input mapping, entities, and combat can be added when gameplay development starts.
