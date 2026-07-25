using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>A fast, fragile enemy that rewards 50 points.</summary>
public sealed class RusherEnemy : Enemy
{
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
