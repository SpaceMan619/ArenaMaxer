# ArenaMaxer — Application Design Document

## 1. Game idea

ArenaMaxer is a top-down survival game. The player moves inside a bounded
arena, fires projectiles in the last movement direction, defeats enemies, collects
health power-ups, and attempts to survive increasingly difficult waves.

The current visual design uses geometric pixel-art forms:

- Blue square: player
- Red square: fast Rusher enemy
- Purple square: slow Tank enemy
- Yellow square: projectile
- Green cross: health power-up

This separates gameplay development from final sprite production. Sprites can
later replace the shape-drawing code without changing entity movement, health,
collision, score, or difficulty logic.

## 2. Architecture

The project separates game coordination and rendering from testable gameplay
rules.

```text
Game1
├── Player
├── List<Enemy>
│   ├── RusherEnemy
│   └── TankEnemy
├── List<Projectile>
├── List<PowerUp>
├── ScoreManager
├── DifficultyCalculator
├── CollisionHelper
├── MathUtilities
├── MusicController
├── ArcadeSoundBank
└── HighScoreStorage
```

`Game1` owns the MonoGame lifecycle and coordinates input, update, collision, and
drawing. Entity classes own their state and behaviour. Static helpers contain
deterministic calculations, allowing them to be tested without opening a game
window.

## 3. Object-oriented design

### Encapsulation

Player and enemy health have private setters. Health can only change through
validated `TakeDamage` and `Heal` methods. Score can only change through the
`ScoreManager`. This prevents unrelated classes from assigning invalid state.

### Inheritance and abstraction

`Enemy` is an abstract base class that defines shared health, damage, movement,
collision bounds, and steering. `RusherEnemy` and `TankEnemy` configure different
statistics while reusing the common behaviour.

### Polymorphism

Both enemy types are stored in `List<Enemy>`. The game updates and collides with
them through the base type, without requiring separate collections or duplicated
loops.

### Single responsibility

- `Game1`: game lifecycle, coordination, and drawing
- `Player`: player state and movement
- `Enemy`: shared enemy state and steering
- `Projectile`: projectile movement and lifetime
- `PowerUp`: collectible effect
- `CollisionHelper`: collision and pickup-range checks
- `MathUtilities`: reusable vector calculations
- `DifficultyCalculator`: wave and spawn formulas
- `ScoreManager`: score rules
- `HighScoreStorage`: safe file persistence
- `MusicController`: soundtrack sections, transitions, fades, and looping
- `ArcadeSoundBank`: original generated arcade feedback effects

### Open/closed design

A new enemy can be added by deriving from `Enemy` and providing different
statistics or overriding `Update`. Existing collision and rendering collections do
not need to be redesigned.

## 4. Data structures

The game uses `List<Enemy>`, `List<Projectile>`, and `List<PowerUp>`.

Lists were chosen because:

- The number of active objects changes continuously.
- Objects must be updated and drawn sequentially each frame.
- The expected object count is small enough that indexed iteration is efficient.
- Reverse indexed loops allow objects to be safely removed after collisions.

Arrays would have a fixed size and require unused positions or resizing. A linked
list would add complexity and provide little benefit for the small number of
entities used here.

`GameState` and `PowerUpType` enums replace unclear numeric or string values with
named states. This makes screen transitions and collectible effects easier to
read and safer to extend.

## 5. Mathematics in gameplay

### Distance

`Vector2.Distance` and squared distance are used for enemy detection and health
pickup range. Squared distance avoids an unnecessary square root when only a
range comparison is needed.

### Direction and vectors

The player input vector is normalized so diagonal movement is not faster than
horizontal or vertical movement. Projectiles move using:

```text
new position = current position + direction × speed × delta time
```

Enemies calculate a normalized direction from their position to the player.

### Algebra

- `health = max(0, health - damage)`
- `health = min(maximum health, health + healing)`
- Spawn interval decreases by `0.11` seconds per wave.
- A lower limit prevents impossible spawn speeds.
- Score combines enemy rewards, power-up rewards, and wave-scaled survival points.

### Dot product

An enemy compares its forward vector to the desired direction with a dot product.
A positive result means the target is generally in front; a negative result means
the enemy must make a larger turn. This affects the enemy turn speed.

### Cross product

The 2D cross-product value determines which side of the enemy's forward direction
contains the player. Its sign selects clockwise or counter-clockwise rotation.

### Linear interpolation

Lerp is used in three different visual systems:

1. The displayed health bar smoothly approaches actual health.
2. Start and Game Over overlays smoothly fade.
3. A red danger tint smoothly appears when health is low.

## 6. Difficulty progression

Each wave lasts 25 seconds. A global `0.8` balance multiplier reduces enemy
movement speed, contact damage, and spawn frequency by 20%. The spawn interval
uses:

```text
max(0.475, (1.35 - (wave - 1) × 0.11) / 0.8)
```

Tank enemies also appear more frequently as the wave increases. This increases
difficulty through both spawn frequency and enemy composition.

## 7. UI and game logic communication

Input is read in `Game1` and converted into simple commands:

- A movement vector is passed to `Player.Move`.
- A new Space press asks `Player.TryShoot`.
- A successful attack creates a `Projectile`.

The UI reads public state such as player health, score, wave, and survival time.
It does not directly calculate damage or change health. This keeps display code
separate from business rules.

## 8. Error handling

Player and enemy methods reject negative damage or healing with clear exceptions.
Projectile construction rejects a zero direction. High-score loading and saving
handle missing files, invalid content, unavailable folders, permission errors, and
I/O errors without crashing the game.

## 9. Future visual upgrade

Version 2 can introduce directional player sprites and two-frame walking
animations. Entity dimensions and collision rectangles already exist separately
from textures, so graphics can be replaced while retaining consistent gameplay.

## 10. Audio design

The soundtrack controller treats 0:00–0:39 as the menu section. Starting play
jumps to 0:39 and fades to gameplay volume over 3.5 seconds. When the track ends,
gameplay restarts at 0:39 so the menu section is not repeated during a run.

Shooting, impact, defeat, damage, pickup, wave, and Game Over effects are generated
from short pulse, triangle, and noise voices. This produces consistent arcade
feedback without relying on third-party sound-effect files.
