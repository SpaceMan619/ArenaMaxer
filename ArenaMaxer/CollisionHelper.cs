using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>contains reusable collision and pickup-range calculations.</summary>
public static class CollisionHelper
{
    // checks whether two rectangular hitboxes overlap.
    public static bool Intersects(Rectangle first, Rectangle second) => first.Intersects(second);

    // checks a circular range with squared distance for less work.
    public static bool IsWithinDistance(Vector2 first, Vector2 second, float maximumDistance) =>
        MathUtilities.DistanceSquared(first, second) <= maximumDistance * maximumDistance;
}
