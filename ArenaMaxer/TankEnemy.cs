using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>a slow, durable enemy that deals more damage and rewards extra points.</summary>
public sealed class TankEnemy : Enemy
{
    // creates the heavy enemy using its larger health, damage, and size.
    public TankEnemy(Vector2 position)
        : base(
            position,
            maximumHealth: 30,
            contactDamage: DifficultyCalculator.ContactDamage(25),
            speed: DifficultyCalculator.EnemySpeed(62f),
            size: 54,
            scoreValue: 150)
    {
    }
}
