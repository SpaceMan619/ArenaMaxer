using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>A player-fired attack that travels in a normalized direction.</summary>
public sealed class Projectile
{
    public const int DefaultDamage = 10;
    public const float Speed = 620f;
    public const float MaximumLifetime = 1.8f;
    public const int Size = 10;

    private float _remainingLifetime = MaximumLifetime;

    public Projectile(Vector2 startPosition, Vector2 direction)
    {
        if (direction == Vector2.Zero)
            throw new ArgumentException("Projectile direction cannot be zero.", nameof(direction));
        Position = startPosition;
        Direction = Vector2.Normalize(direction);
    }

    public Vector2 Position { get; private set; }
    public Vector2 Direction { get; }
    public int Damage => DefaultDamage;
    public bool IsActive => _remainingLifetime > 0f;
    public Rectangle Bounds => new(
        (int)(Position.X - Size / 2f),
        (int)(Position.Y - Size / 2f),
        Size,
        Size);

    public void Update(float deltaTime)
    {
        float safeDeltaTime = Math.Max(0f, deltaTime);
        Position += Direction * Speed * safeDeltaTime;
        _remainingLifetime -= safeDeltaTime;
    }
}
