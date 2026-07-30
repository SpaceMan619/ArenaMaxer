using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>a player-fired attack that travels in a normalized direction.</summary>
public sealed class Projectile
{
    public const int DefaultDamage = 10;
    public const float Speed = 620f;
    public const float MaximumLifetime = 1.8f;
    public const int Size = 10;

    private float _remainingLifetime = MaximumLifetime;

    // validates and stores the projectile's starting values.
    public Projectile(Vector2 startPosition, Vector2 direction, int damage = DefaultDamage)
    {
        if (direction == Vector2.Zero)
            throw new ArgumentException("Projectile direction cannot be zero.", nameof(direction));
        if (damage <= 0)
            throw new ArgumentOutOfRangeException(nameof(damage), "Projectile damage must be positive.");
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

    // moves the projectile and reduces its remaining lifetime.
    public void Update(float deltaTime)
    {
        float safeDeltaTime = Math.Max(0f, deltaTime);
        Position += Direction * Speed * safeDeltaTime;
        _remainingLifetime -= safeDeltaTime;
    }
}
