using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>lists the collectible effects supported by the game.</summary>
public enum PowerUpType
{
    Health
}

/// <summary>stores a collectible that applies an effect when picked up.</summary>
public sealed class PowerUp
{
    public const int DefaultSize = 30;
    public const int HealthRestored = 25;

    // stores the pickup position and effect type.
    public PowerUp(Vector2 position, PowerUpType type)
    {
        Position = position;
        Type = type;
    }

    public Vector2 Position { get; }
    public PowerUpType Type { get; }
    public int Size => DefaultSize;
    public Rectangle Bounds => new(
        (int)(Position.X - Size / 2f),
        (int)(Position.Y - Size / 2f),
        Size,
        Size);

    // applies the effect that belongs to this pickup type.
    public void ApplyTo(Player player)
    {
        if (Type == PowerUpType.Health)
            player.Heal(HealthRestored);
    }
}
