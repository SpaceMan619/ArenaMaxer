using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>a hostile projectile that the player must avoid during the boss battle.</summary>
public sealed class EnemyProjectile
{
    public const float Speed = 310f;
    public const float MaximumLifetime = 4f;
    public const int Size = 16;

    private float _remainingLifetime = MaximumLifetime;

    // validates and stores the starting direction and damage.
    public EnemyProjectile(Vector2 startPosition, Vector2 direction, int damage)
    {
        if (direction == Vector2.Zero)
            throw new ArgumentException("Enemy projectile direction cannot be zero.", nameof(direction));
        if (damage <= 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Enemy projectile damage must be positive.");

        Position = startPosition;
        Direction = Vector2.Normalize(direction);
        Damage = damage;
    }

    public Vector2 Position { get; private set; }
    public Vector2 Direction { get; }
    public int Damage { get; }
    public bool IsActive => _remainingLifetime > 0f;
    public Rectangle Bounds => new(
        (int)(Position.X - Size / 2f),
        (int)(Position.Y - Size / 2f),
        Size,
        Size);

    // moves the projectile and reduces how long it can remain active.
    public void Update(float deltaTime)
    {
        float safeDeltaTime = Math.Max(0f, deltaTime);
        Position += Direction * Speed * safeDeltaTime;
        _remainingLifetime -= safeDeltaTime;
    }
}
