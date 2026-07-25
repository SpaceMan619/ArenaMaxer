using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>Supported collectible effects.</summary>
public enum PowerUpType
{
    Health
}

/// <summary>A collectible that applies an effect when the player reaches its pickup range.</summary>
public sealed class PowerUp
{
    public const int DefaultSize = 30;
    public const int HealthRestored = 25;

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

    public void ApplyTo(Player player)
    {
        if (Type == PowerUpType.Health)
            player.Heal(HealthRestored);
    }
}
