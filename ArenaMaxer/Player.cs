using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>stores and updates player state independently from rendering.</summary>
public sealed class Player
{
    public const int StartingMaximumHealth = 100;
    public const int DefaultSize = 42;
    public const float MovementSpeed = 260f;
    public const float ShotCooldown = 0.22f;

    private float _shotTimer;

    // creates a healthy player with one standard projectile.
    public Player(Vector2 startPosition)
    {
        Position = startPosition;
        FacingDirection = new Vector2(0f, -1f);
        MaximumHealth = StartingMaximumHealth;
        Health = MaximumHealth;
        ProjectileDamage = Projectile.DefaultDamage;
        ProjectileCount = 1;
    }

    public Vector2 Position { get; private set; }
    public Vector2 FacingDirection { get; private set; }
    public int Health { get; private set; }
    public int MaximumHealth { get; private set; }
    public int ProjectileDamage { get; private set; }
    public int ProjectileCount { get; private set; }
    public int Size => DefaultSize;
    public bool IsAlive => Health > 0;
    public Rectangle Bounds => new(
        (int)(Position.X - Size / 2f),
        (int)(Position.Y - Size / 2f),
        Size,
        Size);

    // moves the player using delta time and keeps them inside the arena.
    public void Move(Vector2 input, float deltaTime, Rectangle arena)
    {
        if (input != Vector2.Zero)
        {
            // normalize first so diagonal movement is not faster than straight movement.
            input.Normalize();
            FacingDirection = input;
            Position += input * MovementSpeed * Math.Max(deltaTime, 0f);
        }

        float halfSize = Size / 2f;
        Position = new Vector2(
            MathHelper.Clamp(Position.X, arena.Left + halfSize, arena.Right - halfSize),
            MathHelper.Clamp(Position.Y, arena.Top + halfSize, arena.Bottom - halfSize));
    }

    // reduces the shot cooldown once per frame.
    public void Update(float deltaTime) =>
        _shotTimer = Math.Max(0f, _shotTimer - Math.Max(deltaTime, 0f));

    // returns true when the cooldown allows another shot.
    public bool TryShoot()
    {
        // the cooldown keeps one key press from becoming a wall of bullets.
        if (_shotTimer > 0f || !IsAlive)
            return false;

        _shotTimer = ShotCooldown;
        return true;
    }

    // applies valid damage without letting health fall below zero.
    public void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
        Health = Math.Max(0, Health - amount);
    }

    // restores valid health without exceeding the current maximum.
    public void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Healing cannot be negative.");
        Health = Math.Min(MaximumHealth, Health + amount);
    }

    // applies one permanent upgrade chosen between waves.
    public void ApplyUpgrade(UpgradeType upgrade)
    {
        if (!CanApplyUpgrade(upgrade))
            throw new InvalidOperationException("That upgrade is already at its maximum level.");

        switch (upgrade)
        {
            case UpgradeType.MaxHealth:
                MaximumHealth += 25;
                Health = Math.Min(MaximumHealth, Health + 25);
                break;
            case UpgradeType.DoubleShot:
                ProjectileCount = Math.Min(2, ProjectileCount + 1);
                break;
            case UpgradeType.TripleShot:
                ProjectileCount = 3;
                break;
            case UpgradeType.BulletDamage:
                ProjectileDamage += 5;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(upgrade));
        }
    }

    // checks whether the selected upgrade has reached its limit.
    public bool CanApplyUpgrade(UpgradeType upgrade) => upgrade switch
    {
        UpgradeType.MaxHealth => true,
        UpgradeType.DoubleShot => ProjectileCount < 2,
        UpgradeType.TripleShot => ProjectileCount < 3,
        UpgradeType.BulletDamage => true,
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade))
    };
}
