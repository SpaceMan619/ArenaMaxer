# ArenaMaxer

```text
      .-=-=-=-=-=-=-=-=-=-.
       A R E N A M A X E R
      '-=-=-=-=-=-=-=-=-=-'
          SURVIVE. ADAPT. WIN.
```

ArenaMaxer is a fast-paced 2D survival game built with C# and MonoGame. Defend
the arena against an escalating enemy swarm, collect health power-ups, and chase
a new high score.

Current release: **v1.0**

## Gameplay

Two enemy classes create different threats:

- **Rushers** are quick, fragile, and dangerous in groups.
- **Tanks** move slowly but absorb three shots and inflict heavy contact damage.

Enemy numbers and composition scale as the survival timer advances. Points are
awarded for defeating enemies, collecting power-ups, and surviving each second.
Each wave has a fixed enemy quota. Combat pauses for an upgrade only after every
enemy in that quota has been removed. The player can choose permanent Max Health,
Double Shot, or Bullet Damage upgrades. Clearing wave four opens a Boss Prep
choice, where Triple Shot can be selected for the final battle. Defeat the purple
guardian in wave five to secure the arena. The highest score is saved locally
between sessions.

## Controls

| Action | Input |
|---|---|
| Move | WASD or Arrow Keys |
| Shoot | Space |
| Pause / resume | Escape or Enter during a run |
| Start / restart | Enter or Play button |
| View credits | C or Credits button on the main screen |
| Choose wave upgrade | Click a card or press 1, 2, or 3 |
| Quit | Escape from the main screen |

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

Current result: **15 passed, 0 failed**.

## Technical highlights

- Object-oriented enemy hierarchy with `RusherEnemy`, `TankEnemy`, and `BossEnemy`
- Testable gameplay logic separated from MonoGame rendering
- Vector-based player, enemy, and projectile movement
- Distance-based detection and collection
- Dot-product facing checks and cross-product steering
- Linear interpolation for health, danger, and screen transitions
- Wave-based algebraic difficulty scaling
- Paused between-wave upgrade selection with persistent player statistics
- Final boss battle with aimed projectiles, Rusher reinforcements, and a Victory screen
- Defensive validation and high-score file exception handling
- Section-based soundtrack playback with timed fades and looping
- Original code-generated pulse/noise arcade sound effects
- XML documentation embedded in the C# source

## Credits

- Game developed by **Project Future**
- Theme music: **ArenaMaxer Theme** (`ThemeMusic.ogg`), supplied by Project Future
- Original procedural sound effects generated in C#
- Built with C# and MonoGame

## Documentation

- [Application Design Document](docs/APPLICATION_DESIGN.md)
- [Testing Strategy](docs/TESTING_STRATEGY.md)
