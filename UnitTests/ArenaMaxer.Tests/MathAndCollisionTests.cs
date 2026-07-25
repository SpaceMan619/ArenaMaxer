using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace ArenaMaxer.Tests;

public sealed class MathAndCollisionTests
{
    [Test]
    public void Distance_UsesPythagoreanDistance()
    {
        float distance = MathUtilities.Distance(Vector2.Zero, new Vector2(3f, 4f));
        Assert.That(distance, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void Direction_ReturnsNormalizedVectorTowardTarget()
    {
        Vector2 direction = MathUtilities.Direction(Vector2.Zero, new Vector2(10f, 0f));
        Assert.That(direction, Is.EqualTo(Vector2.UnitX));
    }

    [Test]
    public void Dot_ReturnsPositiveValueForTargetInFront()
    {
        float result = MathUtilities.Dot(Vector2.UnitX, new Vector2(3f, 1f));
        Assert.That(result, Is.GreaterThan(0f));
    }

    [Test]
    public void Cross_IdentifiesTargetOnLeft()
    {
        float result = MathUtilities.Cross(Vector2.UnitX, new Vector2(0f, -1f));
        Assert.That(result, Is.LessThan(0f));
    }

    [Test]
    public void RectangleCollision_DetectsOverlapAndSeparation()
    {
        Rectangle first = new(0, 0, 20, 20);
        Assert.Multiple(() =>
        {
            Assert.That(CollisionHelper.Intersects(first, new Rectangle(10, 10, 20, 20)), Is.True);
            Assert.That(CollisionHelper.Intersects(first, new Rectangle(30, 30, 10, 10)), Is.False);
        });
    }

    [Test]
    public void PickupRange_UsesDistanceThreshold()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CollisionHelper.IsWithinDistance(Vector2.Zero, new Vector2(3f, 4f), 5f), Is.True);
            Assert.That(CollisionHelper.IsWithinDistance(Vector2.Zero, new Vector2(6f, 0f), 5f), Is.False);
        });
    }
}
