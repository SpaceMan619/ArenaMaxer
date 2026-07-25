# ArenaMaxer

ArenaMaxer is a top-down 2D survival game built with C# and MonoGame. The
player must survive increasingly difficult waves, defeat two enemy types, collect
health power-ups, and achieve the highest possible score.

Version 1 deliberately uses clean geometric placeholder graphics. These can be
replaced with animated sprites later without changing the tested gameplay logic.

## Features

- Start screen with a clickable Play button
- WASD and arrow-key movement
- Directional projectile attacks
- Fast Rusher enemies and durable Tank enemies
- Continuous spawning with wave-based difficulty scaling
- Player health and animated health bar
- Health power-ups
- Score rewards for enemy defeats, pickups, and survival
- Persistent local high score with safe file-error handling
- Game Over screen and restart
- Practical use of distance, vectors, algebra, dot product, cross product, and Lerp
- Separate NUnit project containing 23 automated tests

## How to play

- **Move:** WASD or Arrow Keys
- **Shoot:** Space
- **Start/restart:** Click Play or press Enter
- **Quit:** Escape

The player shoots in the last movement direction. Red Rushers are fast but weak.
Purple Tanks are slower, deal more damage, and require three hits. Green power-ups
restore health.

## How to run

Requirements:

- .NET SDK 9 or newer
- A desktop environment supported by MonoGame DesktopGL

From this repository's solution folder:

```bash
dotnet restore
dotnet run --project ArenaMaxer/ArenaMaxer.csproj
```

## How to run the tests

```bash
dotnet test ArenaMaxer.slnx
```

Current result: **23 passed, 0 failed**.

## Project documentation

- [Application Design Document](docs/APPLICATION_DESIGN.md)
- [Testing Strategy](docs/TESTING_STRATEGY.md)
- [Submission Checklist](docs/SUBMISSION_CHECKLIST.md)

Important classes and public methods include XML documentation comments embedded
directly in the C# source. XML documentation-file generation is enabled in the
main project configuration.
