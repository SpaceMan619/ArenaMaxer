using Microsoft.Xna.Framework;

namespace ArenaMaxer;

/// <summary>Contains reusable collision and pickup-range calculations.</summary>
public static class CollisionHelper
{
    public static bool Intersects(Rectangle first, Rectangle second) => first.Intersects(second);

    public static bool IsWithinDistance(Vector2 first, Vector2 second, float maximumDistance) =>
        MathUtilities.DistanceSquared(first, second) <= maximumDistance * maximumDistance;
}
