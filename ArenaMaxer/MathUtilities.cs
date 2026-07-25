using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>Provides testable vector calculations used by movement, detection, and steering.</summary>
public static class MathUtilities
{
    public static float Distance(Vector2 first, Vector2 second) => Vector2.Distance(first, second);

    public static float DistanceSquared(Vector2 first, Vector2 second) => Vector2.DistanceSquared(first, second);

    public static Vector2 Direction(Vector2 from, Vector2 to)
    {
        Vector2 difference = to - from;
        return difference == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(difference);
    }

    public static float Dot(Vector2 first, Vector2 second)
    {
        if (first == Vector2.Zero || second == Vector2.Zero)
            return 0f;
        return Vector2.Dot(Vector2.Normalize(first), Vector2.Normalize(second));
    }

    /// <summary>Returns the Z component of the cross product for two 2D vectors.</summary>
    public static float Cross(Vector2 first, Vector2 second) =>
        first.X * second.Y - first.Y * second.X;

    public static Vector2 RotateTowards(Vector2 current, Vector2 target, float maximumRadians, float turnSide)
    {
        if (target == Vector2.Zero)
            return current;
        if (current == Vector2.Zero)
            return Vector2.Normalize(target);

        Vector2 normalizedCurrent = Vector2.Normalize(current);
        Vector2 normalizedTarget = Vector2.Normalize(target);
        float dot = Math.Clamp(Vector2.Dot(normalizedCurrent, normalizedTarget), -1f, 1f);
        float angle = MathF.Acos(dot);
        if (angle <= maximumRadians)
            return normalizedTarget;

        float sign = Math.Sign(turnSide);
        if (sign == 0f)
            sign = 1f;
        float rotation = Math.Min(angle, Math.Max(0f, maximumRadians)) * sign;
        float cosine = MathF.Cos(rotation);
        float sine = MathF.Sin(rotation);
        return Vector2.Normalize(new Vector2(
            normalizedCurrent.X * cosine - normalizedCurrent.Y * sine,
            normalizedCurrent.X * sine + normalizedCurrent.Y * cosine));
    }
}
