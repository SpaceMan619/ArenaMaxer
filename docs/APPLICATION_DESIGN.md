# ArenaMaxer - Application Design Document

## 1. Idea and scope

ArenaMaxer is a top-down survival game. The player moves around a bounded
arena, shoots in the last movement direction, collects health pickups, and tries
to survive increasingly difficult waves. I kept the scope deliberately small:
there are a few enemy types, one final guardian, and upgrades that change how a
run feels without adding a second game inside the first one.

For this version I kept the visuals as geometric pixel-art forms so I could
focus on the gameplay and the code structure:

- blue square: player
- red square: Rusher
- purple square: Tank or guardian
- yellow square: projectile
- green cross: health pickup

This also leaves the door open for a later sprite pass. The entity dimensions
and collision rectangles are separate from the drawing code, so replacing the
shapes with sprites would not require rewriting movement, health, collisions,
score, or difficulty.

## 2. Architecture

`Game1` is the coordinator. It owns the MonoGame lifecycle, reads input, updates
the current state, handles collisions, and draws the screen. The other classes
hold the rules that would otherwise make `Game1` difficult to follow.

```text
Game1
├── Player
├── List<Enemy>
│   ├── RusherEnemy
│   ├── TankEnemy
│   └── BossEnemy
├── List<Projectile>
├── List<EnemyProjectile>
├── List<PowerUp>
├── AttackPattern
├── ScoreManager
├── DifficultyCalculator
├── CollisionHelper
├── MathUtilities
├── MusicController
├── ArcadeSoundBank
└── HighScoreStorage
```

The most useful split was moving deterministic calculations into small helpers.
For example, `MathUtilities` and `DifficultyCalculator` can be tested without
opening a MonoGame window.

## 3. Object-oriented design

### Encapsulation

`Player.Health`, `Player.MaximumHealth`, and enemy health have private setters.
Other classes cannot assign random values directly. They must use methods such
as `TakeDamage`, `Heal`, or `ApplyUpgrade`, where the limits are checked.

The same idea applies to score: score changes go through `ScoreManager` instead
of being edited from random places in the game loop.

### Inheritance and abstraction

`Enemy` is an abstract base class. It contains the shared health, contact
damage, movement, collision bounds, and steering behaviour. `RusherEnemy`,
`TankEnemy`, and `BossEnemy` then provide their own statistics and special
behaviour.

### Polymorphism

All enemy types can be stored in a `List<Enemy>`. The update and collision loops
work with the base type, so I do not need a separate copy of each loop for each
enemy class.

### Responsibility of each class

- `Game1`: lifecycle, input, coordination, and drawing
- `Player`: movement, health, shooting, and upgrades
- `Enemy`: shared enemy state and steering
- `Projectile` and `EnemyProjectile`: movement and lifetime
- `PowerUp`: collectible effect
- `CollisionHelper`: rectangle and distance checks
- `MathUtilities`: reusable vector calculations
- `DifficultyCalculator`: wave quotas and spawn formulas
- `ScoreManager`: scoring rules
- `HighScoreStorage`: safe local score persistence
- `MusicController`: soundtrack sections, fades, pauses, and looping
- `ArcadeSoundBank`: short arcade feedback sounds

If I add another enemy, I can derive it from `Enemy`, give it different
statistics or update behaviour, and leave the main enemy loops alone.

## 4. Data structures and enums

The active enemies, projectiles, enemy projectiles, and pickups are stored in
`List<T>` collections. Their sizes change constantly, and the game needs to
update and draw each active object every frame. Reverse indexed loops also make
it safe to remove an object after a collision.

An array would need a fixed size or extra empty slots. A linked list could work,
but it would add complexity without helping much at the small object counts in
this game. A list is easier to read and is a good fit for the update loop.

`GameState`, `PowerUpType`, and `UpgradeType` are enums. They replace magic
numbers or string comparisons with named options such as `Paused`, `Victory`,
`Health`, and `TripleShot`.

## 5. Mathematics used in the game

### Distance

`Vector2.Distance` is used when the actual distance is useful. Squared distance
is used for range checks such as enemy detection and health pickups, because it
avoids a square root when I only need to know whether something is inside a
radius.

### Direction and vectors

The movement input is converted into a direction vector and normalized. This
stops diagonal movement from being faster than horizontal movement. Projectile
movement follows the basic update:

```text
new position = current position + direction x speed x delta time
```

Enemies use a normalized vector from their position towards the player.

### Algebra

The game uses simple calculations for health, upgrades, scoring, and difficulty.
The important part for me was seeing where each calculation affects play:

```text
health = max(0, health - damage)
health = min(maximum health, health + healing)
spawn interval = max(0.475, (1.35 - (wave - 1) x 0.11) / 0.9)
```

The permanent upgrades add 25 maximum health, one projectile, or 5 bullet
damage. Score comes from defeated enemies, pickups, and surviving over time.

### Dot product

An enemy compares its forward vector with the direction to the player. A
positive dot product means the player is generally in front; a negative result
means the player is behind. The result helps decide how strongly the enemy
turns.

### Cross product

The 2D cross-product sign tells the enemy which side of its current direction
contains the player. One sign means turn clockwise and the other means turn
counter-clockwise.

### Linear interpolation

Lerp is used in three visible places:

1. The displayed health bar smoothly follows the real health.
2. Start, pause, Game Over, and Victory overlays fade in and out.
3. The low-health danger tint gradually appears instead of switching on sharply.

## 6. Difficulty and wave progression

Wave one requires 15 enemies. Each later normal wave adds four more. The upgrade
screen does not appear when the last enemy is merely spawned; it appears only
when the full quota has spawned and the active-enemy list is empty.

The `0.9` balance multiplier makes the enemy speed and contact-damage settings
slightly more forgiving. Spawn intervals still become shorter, down to a
minimum value, and Tanks appear more often later on.

After wave four, Boss Prep offers Triple Shot for the final fight. Wave five
contains the guardian, whose aimed projectiles must be dodged. It also creates a
pair of Rusher reinforcements every seven seconds. Defeating the guardian ends
the game with the Victory screen.

## 7. UI and game logic

`Game1` reads keyboard and mouse input and turns it into simple commands. A
movement vector goes to `Player.Move`. A new Space press calls `TryShoot`. A
selected card becomes an `UpgradeType` and is passed to `ApplyUpgrade`.

The UI reads public values such as health, score, wave, and survival time. It
does not directly change health or calculate damage. This keeps rendering and
game rules separate while still allowing the UI to show the current state.

## 8. Error handling

Player and enemy methods reject negative damage or healing. Projectile
constructors reject zero directions and non-positive damage. Invalid upgrades
are rejected, and the UI disables upgrades that have reached their limit.

`HighScoreStorage` handles missing files, invalid saved values, unavailable
folders, permission errors, and other I/O errors without crashing the game.

## 9. Audio and future visual work

The soundtrack treats 0:00-0:39 as the menu section. Starting the game jumps to
0:39 and fades into gameplay volume over 3.5 seconds. When the track ends, the
gameplay section loops from 0:39 instead of replaying the menu intro.

Shooting, impacts, defeats, damage, pickups, wave starts, and Game Over use
short generated arcade sounds. This keeps the sound design consistent without
adding a large collection of external effect files.

If I continue the project, I would replace the geometric player with directional
sprites and a small walking animation. The existing entity sizes and collision
rectangles mean that this would be a visual change, not a rewrite of the rules.
