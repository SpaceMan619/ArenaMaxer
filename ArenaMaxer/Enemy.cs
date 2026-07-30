using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>defines shared state and steering behaviour for all enemy types.</summary>
public abstract class Enemy
{
    // validates and stores the statistics shared by every enemy type.
    protected Enemy(Vector2 position, int maximumHealth, int contactDamage, float speed, int size, int scoreValue)
    {
        Position = position;
        MaximumHealth = maximumHealth;
        Health = maximumHealth;
        ContactDamage = contactDamage;
        Speed = speed;
        Size = size;
        ScoreValue = scoreValue;
        Forward = Vector2.UnitY;
    }

    public Vector2 Position { get; protected set; }
    public Vector2 Forward { get; protected set; }
    public int MaximumHealth { get; }
    public int Health { get; private set; }
    public int ContactDamage { get; }
    public float Speed { get; }
    public int Size { get; }
    public int ScoreValue { get; }
    public float HitFlash { get; private set; }
    public float DetectionRadius { get; init; } = 1200f;
    public Rectangle Bounds => new(
        (int)(Position.X - Size / 2f),
        (int)(Position.Y - Size / 2f),
        Size,
        Size);

    // steers toward the player using dot and cross products to turn smoothly.
    public virtual void Update(Vector2 playerPosition, float deltaTime)
    {
        HitFlash = Math.Max(0f, HitFlash - deltaTime);
        float distance = MathUtilities.Distance(Position, playerPosition);
        if (distance <= 0.001f || distance > DetectionRadius)
            return;

        Vector2 desiredDirection = MathUtilities.Direction(Position, playerPosition);
        // dot says how aligned the enemy is; cross says which side the player is on.
        float alignment = MathUtilities.Dot(Forward, desiredDirection);
        float turnSide = MathUtilities.Cross(Forward, desiredDirection);
        float maximumTurn = alignment < 0f ? 4.5f * deltaTime : 7f * deltaTime;
        Forward = MathUtilities.RotateTowards(Forward, desiredDirection, maximumTurn, turnSide);
        Position += Forward * Speed * Math.Max(deltaTime, 0f);
    }

    // applies damage and reports whether this hit defeated the enemy.
    public bool TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");

        Health = Math.Max(0, Health - amount);
        HitFlash = 0.09f;
        return Health == 0;
    }
}
