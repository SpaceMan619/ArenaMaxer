using Microsoft.Xna.Framework;
using System;

namespace ArenaMaxer;

/// <summary>provides testable vector calculations used by movement, detection, and steering.</summary>
public static class MathUtilities
{
    // gets the straight-line distance between two positions.
    public static float Distance(Vector2 first, Vector2 second) => Vector2.Distance(first, second);

    // compares distances without the extra square-root calculation.
    public static float DistanceSquared(Vector2 first, Vector2 second) => Vector2.DistanceSquared(first, second);

    // creates a unit vector that points from one position to another.
    public static Vector2 Direction(Vector2 from, Vector2 to)
    {
        Vector2 difference = to - from;
        // a zero vector has no direction, so it must not be normalized.
        return difference == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(difference);
    }

    // measures whether two directions face together, sideways, or apart.
    public static float Dot(Vector2 first, Vector2 second)
    {
        if (first == Vector2.Zero || second == Vector2.Zero)
            return 0f;
        // normalization makes the answer stay between negative one and one.
        return Vector2.Dot(Vector2.Normalize(first), Vector2.Normalize(second));
    }

    // returns a signed value that tells whether the second vector is left or right.
    public static float Cross(Vector2 first, Vector2 second) =>
        first.X * second.Y - first.Y * second.X;

    // rotates one direction toward another without exceeding the allowed turn angle.
    public static Vector2 RotateTowards(Vector2 current, Vector2 target, float maximumRadians, float turnSide)
    {
        // these checks avoid invalid normalization and give a sensible starting direction.
        if (target == Vector2.Zero)
            return current;
        if (current == Vector2.Zero)
            return Vector2.Normalize(target);

        Vector2 normalizedCurrent = Vector2.Normalize(current);
        Vector2 normalizedTarget = Vector2.Normalize(target);
        // clamping protects acos from tiny floating-point errors outside its valid range.
        float dot = Math.Clamp(Vector2.Dot(normalizedCurrent, normalizedTarget), -1f, 1f);
        float angle = MathF.Acos(dot);
        if (angle <= maximumRadians)
            return normalizedTarget;

        // the cross-product sign decides whether this rotation goes left or right.
        float sign = Math.Sign(turnSide);
        if (sign == 0f)
            sign = 1f;
        float rotation = Math.Min(angle, Math.Max(0f, maximumRadians)) * sign;
        float cosine = MathF.Cos(rotation);
        float sine = MathF.Sin(rotation);
        // this is the standard two-dimensional rotation formula.
        return Vector2.Normalize(new Vector2(
            normalizedCurrent.X * cosine - normalizedCurrent.Y * sine,
            normalizedCurrent.X * sine + normalizedCurrent.Y * cosine));
    }
}
