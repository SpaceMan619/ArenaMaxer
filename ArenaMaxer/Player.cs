using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>Stores and updates player state independently from rendering.</summary>
public sealed class Player
{
    public const int StartingMaximumHealth = 100;
    public const int DefaultSize = 42;
    public const float MovementSpeed = 260f;
    public const float ShotCooldown = 0.22f;

    private float _shotTimer;

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

    /// <summary>Moves the player at a frame-rate-independent speed and keeps them inside the arena.</summary>
    public void Move(Vector2 input, float deltaTime, Rectangle arena)
    {
        if (input != Vector2.Zero)
        {
            input.Normalize();
            FacingDirection = input;
            Position += input * MovementSpeed * Math.Max(deltaTime, 0f);
        }

        float halfSize = Size / 2f;
        Position = new Vector2(
            MathHelper.Clamp(Position.X, arena.Left + halfSize, arena.Right - halfSize),
            MathHelper.Clamp(Position.Y, arena.Top + halfSize, arena.Bottom - halfSize));
    }

    public void Update(float deltaTime) =>
        _shotTimer = Math.Max(0f, _shotTimer - Math.Max(deltaTime, 0f));

    /// <summary>Returns true only when a shot is allowed, then begins the cooldown.</summary>
    public bool TryShoot()
    {
        if (_shotTimer > 0f || !IsAlive)
            return false;

        _shotTimer = ShotCooldown;
        return true;
    }

    /// <summary>Applies non-negative damage while preventing health from dropping below zero.</summary>
    public void TakeDamage(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
        Health = Math.Max(0, Health - amount);
    }

    /// <summary>Restores non-negative health without exceeding the maximum.</summary>
    public void Heal(int amount)
    {
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Healing cannot be negative.");
        Health = Math.Min(MaximumHealth, Health + amount);
    }

    /// <summary>Applies one of the permanent upgrades offered between waves.</summary>
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

    /// <summary>Reports whether an upgrade can still improve the player.</summary>
    public bool CanApplyUpgrade(UpgradeType upgrade) => upgrade switch
    {
        UpgradeType.MaxHealth => true,
        UpgradeType.DoubleShot => ProjectileCount < 2,
        UpgradeType.TripleShot => ProjectileCount < 3,
        UpgradeType.BulletDamage => true,
        _ => throw new ArgumentOutOfRangeException(nameof(upgrade))
    };
}
