using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>A slow, durable enemy that deals more damage and rewards 150 points.</summary>
public sealed class TankEnemy : Enemy
{
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
