using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>The final purple arena guardian. It fires aimed projectiles during the boss battle.</summary>
public sealed class BossEnemy : Enemy
{
    public const float FireInterval = 1.05f;
    public const int ProjectileDamage = 14;

    private float _fireTimer = FireInterval;

    public BossEnemy(Vector2 position)
        : base(
            position,
            maximumHealth: 260,
            contactDamage: DifficultyCalculator.ContactDamage(30),
            speed: DifficultyCalculator.EnemySpeed(42f),
            size: 100,
            scoreValue: 1000)
    {
    }

    /// <summary>Updates the firing timer and reports when a projectile should be created.</summary>
    public bool TryFire(float deltaTime)
    {
        _fireTimer -= Math.Max(0f, deltaTime);
        if (_fireTimer > 0f)
            return false;

        _fireTimer = FireInterval;
        return true;
    }
}
