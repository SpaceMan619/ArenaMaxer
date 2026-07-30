using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>a fast, fragile enemy that rewards fifty points.</summary>
public sealed class RusherEnemy : Enemy
{
    // creates the light enemy using its balanced health, damage, and speed.
    public RusherEnemy(Vector2 position)
        : base(
            position,
            maximumHealth: 10,
            contactDamage: DifficultyCalculator.ContactDamage(12),
            speed: DifficultyCalculator.EnemySpeed(128f),
            size: 34,
            scoreValue: 50)
    {
    }
}
