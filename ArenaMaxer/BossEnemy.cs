using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>the final purple guardian that fires projectiles and summons rushers.</summary>
public sealed class BossEnemy : Enemy
{
    public const float FireInterval = 0.7f;
    public const float MinionSpawnInterval = 7f;
    public const int MinionsPerSpawn = 2;
    public const int ProjectileDamage = 14;

    private float _fireTimer = FireInterval;
    private float _minionSpawnTimer = MinionSpawnInterval;

    // creates the boss with its larger health, damage, size, and score value.
    public BossEnemy(Vector2 position)
        : base(
            position,
            maximumHealth: 520,
            contactDamage: DifficultyCalculator.ContactDamage(30),
            speed: DifficultyCalculator.EnemySpeed(42f),
            size: 100,
            scoreValue: 1000)
    {
    }

    // counts down and reports when the boss should fire.
    public bool TryFire(float deltaTime)
    {
        // a tiny tolerance stops floating point rounding from delaying a shot by one frame.
        _fireTimer -= Math.Max(0f, deltaTime);
        if (_fireTimer > 0.0001f)
            return false;

        _fireTimer = FireInterval;
        return true;
    }

    // counts down and reports when the boss should summon rushers.
    public bool TrySpawnMinions(float deltaTime)
    {
        // the boss gets help in bursts, so the fight has pressure without endless spawns.
        _minionSpawnTimer -= Math.Max(0f, deltaTime);
        if (_minionSpawnTimer > 0.0001f)
            return false;

        _minionSpawnTimer = MinionSpawnInterval;
        return true;
    }
}
