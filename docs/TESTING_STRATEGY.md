# ArenaMaxer — Testing Strategy

## 1. Approach

The project uses NUnit in a separate `ArenaMaxer.Tests` project. Tests focus on
deterministic gameplay and mathematical logic rather than MonoGame drawing.

The code was designed for testability by moving health, movement, collision,
difficulty, score, and vector calculations out of `Game1` and into small classes
that do not require a graphics device.

## 2. Areas covered

| Area | Examples verified |
|---|---|
| Player health | Initial health, damage floor, healing limit, invalid damage |
| Movement | Normalized diagonal speed and arena boundaries |
| Attacking | Shooting cooldown |
| Projectiles | Direction, speed, damage, and elapsed-time movement |
| Enemies | Rusher durability and Tank durability |
| Distance | Pythagorean distance and pickup radius |
| Vectors | Direction normalization |
| Dot product | Target-in-front result |
| Cross product | Left/right sign |
| Collision | Overlapping and separated rectangles |
| Difficulty | Wave timing, decreasing spawn interval, minimum limit |
| Score | Kill, pickup, and survival rewards |
| Power-ups | Correct healing effect |
| Audio timing | Menu/gameplay boundary and clamped fade interpolation |
| Wave upgrades | Maximum health, multishot cap, bullet damage, attack spread |

## 3. Edge cases

Tests include:

- Damage larger than remaining health
- Negative damage
- Healing beyond maximum health
- Diagonal movement normalization
- Movement far outside arena boundaries
- Shooting during and after cooldown
- Collision at valid and invalid distances
- Wave completion only after the full enemy quota is spawned and removed
- Very high waves reaching the minimum spawn interval
- A Tank surviving the first two projectile hits
- Double Shot refusing a fourth simultaneous projectile

## 4. Independence and repeatability

Every test creates its own objects and does not depend on execution order. Random
enemy positions, graphics, keyboard state, saved user files, and real elapsed time
are excluded from unit tests.

## 5. Current result

Command:

```bash
dotnet test ArenaMaxer.slnx
```

Latest verified result:

```text
Passed: 39
Failed: 0
Skipped: 0
```

## 6. Manual gameplay verification

Manual playtesting verifies:

1. Play button and Enter both start the game.
2. WASD and arrow keys move the player.
3. Space fires in the last movement direction.
4. Rusher dies in one hit.
5. Tank dies in three hits.
6. Enemy contact reduces health.
7. Green power-up restores health.
8. Score, time, wave, and health are readable.
9. Spawn speed and Tank frequency increase over time.
10. Game Over appears at zero health and restart works.
11. High score remains after closing and reopening the game.
12. Escape closes the game.
13. Combat pauses only after every enemy in the current wave is defeated or
    removed, and the three upgrade cards accept mouse or number-key selection.
14. The soundtrack plays 0:00–0:39 on the menu, begins gameplay from 0:39 with
    a 3.5-second fade, and loops gameplay without replaying the intro.
