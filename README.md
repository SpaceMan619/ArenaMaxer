# ArenaMaxer

ArenaMaxer is a fast-paced 2D survival game built with C# and MonoGame. Defend
the arena against an escalating enemy swarm, collect health power-ups, and chase
a new high score.

## Gameplay

Two enemy classes create different threats:

- **Rushers** are quick, fragile, and dangerous in groups.
- **Tanks** move slowly but absorb three shots and inflict heavy contact damage.

Enemy numbers and composition scale as the survival timer advances. Points are
awarded for defeating enemies, collecting power-ups, and surviving each second.
The highest score is saved locally between sessions.

## Controls

| Action | Input |
|---|---|
| Move | WASD or Arrow Keys |
| Shoot | Space |
| Start / restart | Enter or Play button |
| Quit | Escape |

The player fires in the last movement direction. Green power-ups restore health.

## Running the game

### Requirements

- .NET SDK 9 or newer
- A desktop environment supported by MonoGame DesktopGL

Clone the repository and run:

```bash
dotnet restore
dotnet run --project ArenaMaxer/ArenaMaxer.csproj
```

In Visual Studio Code, open the repository folder and select
**Run and Debug → Run ArenaMaxer**.

## Automated tests

The separate NUnit project tests health, damage, movement, projectiles, collision
helpers, scoring, enemy durability, power-ups, vector mathematics, and difficulty
scaling.

```bash
dotnet test ArenaMaxer.slnx
```

Current result: **24 passed, 0 failed**.

## Technical highlights

- Object-oriented enemy hierarchy with `RusherEnemy` and `TankEnemy`
- Testable gameplay logic separated from MonoGame rendering
- Vector-based player, enemy, and projectile movement
- Distance-based detection and collection
- Dot-product facing checks and cross-product steering
- Linear interpolation for health, danger, and screen transitions
- Wave-based algebraic difficulty scaling
- Defensive validation and high-score file exception handling
- XML documentation embedded in the C# source

## Documentation

- [Application Design Document](docs/APPLICATION_DESIGN.md)
- [Testing Strategy](docs/TESTING_STRATEGY.md)
