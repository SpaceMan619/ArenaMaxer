using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>builds symmetric projectile volleys for single, double, and triple shot.</summary>
public static class AttackPattern
{
    // builds the correct spread for the player's current projectile count.
    public static Vector2[] CreateDirections(Vector2 facingDirection, int projectileCount)
    {
        if (facingDirection == Vector2.Zero)
            throw new ArgumentException("Facing direction cannot be zero.", nameof(facingDirection));

        Vector2 facing = Vector2.Normalize(facingDirection);
        return Math.Clamp(projectileCount, 1, 3) switch
        {
            1 => new[] { facing },
            2 => new[] { Rotate(facing, -0.11f), Rotate(facing, 0.11f) },
            _ => new[] { Rotate(facing, -0.17f), facing, Rotate(facing, 0.17f) }
        };
    }

    // rotates one direction by a small angle using sine and cosine.
    private static Vector2 Rotate(Vector2 direction, float radians)
    {
        float cosine = MathF.Cos(radians);
        float sine = MathF.Sin(radians);
        return Vector2.Normalize(new Vector2(
            direction.X * cosine - direction.Y * sine,
            direction.X * sine + direction.Y * cosine));
    }
}
