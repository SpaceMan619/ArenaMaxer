# ArenaMaxer - Testing Strategy

## 1. Approach

I used NUnit in a separate `ArenaMaxer.Tests` project. I did not try to unit-test
the drawing code. Instead, the tests target rules and calculations where a wrong
answer can directly change the way the game plays.

Health, movement, projectiles, collision helpers, difficulty, and vector maths
are kept in small classes that can be created without a graphics device. That
lets me run the tests without starting a game window.

There are **15 independent tests** in
`UnitTests/ArenaMaxer.Tests/ArenaMaxerCoreTests.cs`.

## 2. What is covered

| Area | What is checked |
|---|---|
| Player health | Starting health, damage floor, and healing limit |
| Movement | Diagonal movement is normalized |
| Shooting | The firing cooldown blocks an early second shot |
| Projectiles | Direction, speed, and elapsed-time movement |
| Enemies | Rusher and Tank durability |
| Wave rules | Quota growth and completion only after all active enemies are gone |
| Distance | Pythagorean distance and pickup radius |
| Dot product | A target in front produces a positive result |
| Cross product | The sign identifies the chosen side |
| Upgrades | Triple Shot unlocks during Boss Prep |
| Boss timing | Boss shots and two-Rusher reinforcement timing |

## 3. How one test works

The tests follow the usual three-step pattern:

1. **Arrange:** create the objects and starting values.
2. **Act:** call the method being tested.
3. **Assert:** compare the actual result with the expected result.

For example, `Damage_NeverReducesHealthBelowZero` creates a player, applies 150
damage, and checks that health is zero rather than negative. If the assertion
fails, NUnit reports the test name and the expected-versus-actual result.

## 4. Edge cases

I included cases that are easy to get wrong during normal play:

- damage larger than the player's remaining health
- healing beyond maximum health
- diagonal input accidentally moving faster
- shooting before the cooldown has finished
- a Rusher dying to one standard shot
- a Tank surviving two shots and dying on the third
- a wave waiting for the last active enemy to be removed
- Triple Shot being available for Boss Prep
- boss reinforcements appearing at the configured interval

## 5. Independence and repeatability

Every test creates its own objects and does not rely on another test running
first. There is no graphics device, keyboard input, random enemy position, real
save file, or open game window involved. That keeps the result repeatable.

## 6. Test command and result

Run the tests with:

```bash
dotnet test ArenaMaxer.slnx
```

Latest verified result:

```text
Passed: 15
Failed: 0
Skipped: 0
```

## 7. Manual gameplay checks

Automated tests do not replace actually playing the game, so I also checked the
main flow manually:

- Play and Enter start the game.
- WASD and arrow keys move the player, and Space fires in the last movement
  direction.
- Rushers, Tanks, contact damage, health pickups, score, time, and wave labels
  behave as expected.
- Enemy speed and Tank frequency increase over time.
- Game Over, restart, high-score persistence, and the Victory screen work.
- A wave does not end until the full quota has spawned and all active enemies
  are gone.
- Upgrade cards work with the mouse and number keys.
- The menu soundtrack uses 0:00-0:39, gameplay starts at 0:39 with a fade, and
  the gameplay section loops correctly.
- The boss fires dodge-only projectiles and summons Rusher pairs every seven
  seconds.
- Escape pauses the game and music, Resume returns to the same game state, and
  Main Menu returns to the start screen. Escape quits only from the main menu.
