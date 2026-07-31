# ArenaMaxer

```text
      .-=-=-=-=-=-=-=-=-=-.
       A R E N A M A X E R
      '-=-=-=-=-=-=-=-=-=-'
          SURVIVE. ADAPT. WIN.
```

I built ArenaMaxer as a small 2D survival game in C# and MonoGame. The basic
loop is easy to understand: move, shoot, survive the next wave, choose an
upgrade, and try to reach the guardian with enough health left to win.

Current release: **v1.2**

## How the game works

Rushers are quick and fragile. Tanks are slower, but a standard projectile takes
three hits to bring one down. Later waves increase both the number of enemies and
the chance of seeing Tanks.

Each wave has a quota. The upgrade screen waits until that quota has spawned and
the arena is empty, so enemies do not disappear just because the timer reached a
new wave. Normal waves offer Max Health, Double Shot, or Bullet Damage. After
wave four, Boss Prep offers Triple Shot for the final fight. The purple guardian
shoots back and sends Rusher reinforcements into the arena. Defeating it shows
the Victory screen.

## Controls

| Action | Input |
|---|---|
| Move | WASD or Arrow Keys |
| Shoot | Space |
| Pause / resume | Escape or Enter during a run |
| Start / restart | Enter or the Play button |
| View credits | C or the Credits button on the main screen |
| Choose an upgrade | Click a card or press 1, 2, or 3 |
| Quit | Escape from the main screen |

The player fires in the last movement direction. Green power-ups restore
health.

## Running the game

### Requirements

- .NET SDK 9 or newer
- A desktop environment supported by MonoGame DesktopGL

From the repository folder:

```bash
dotnet restore
dotnet run --project ArenaMaxer/ArenaMaxer.csproj
```

In Visual Studio Code, open the repository and use **Run and Debug -> Run
ArenaMaxer**.

## Tests

The separate NUnit project contains 15 tests for the rules that are easiest to
break while changing the game: health limits, movement, shooting cooldowns,
projectile movement, enemy durability, wave completion, pickup distance, dot
and cross products, upgrades, and boss timing.

```bash
dotnet test ArenaMaxer.slnx
```

Latest result: **15 passed, 0 failed**.

## Technical highlights

- `Enemy` is an abstract base class. `RusherEnemy`, `TankEnemy`, and `BossEnemy`
  reuse it while changing their own statistics and behaviour.
- Player, enemy, projectile, collision, score, difficulty, and audio rules live
  in separate classes, rather than being packed into `Game1`.
- Movement and targeting use `Vector2`, distance checks, normalization, dot
  products, and cross products.
- Lerp is used for the health bar, screen fades, and the low-health danger tint.
- Difficulty scales through wave quotas, spawn timing, and enemy composition.
- The game pauses between waves for permanent upgrades and supports a separate
  Boss Prep choice.
- The soundtrack has menu and gameplay sections, fades between states, and loops
  from the gameplay start point.
- Arcade sound effects are generated in code for shooting, impacts, pickups,
  waves, damage, and Game Over.
- C# XML documentation is included in the source.

## Credits

- Game developed by **Project Future**
- Theme music: **ArenaMaxer Theme** (`ThemeMusic.ogg`), supplied by Project Future
- Original procedural sound effects generated in C#
- Built with C# and MonoGame

## Project documents

- [Application Design Document](docs/APPLICATION_DESIGN.md)
- [Testing Strategy](docs/TESTING_STRATEGY.md)
