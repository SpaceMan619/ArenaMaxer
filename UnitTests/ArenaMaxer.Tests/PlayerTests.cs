using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace ArenaMaxer.Tests;

public sealed class PlayerTests
{
    [Test]
    public void NewPlayer_StartsWithMaximumHealth()
    {
        Player player = new(Vector2.Zero);
        Assert.That(player.Health, Is.EqualTo(player.MaximumHealth));
    }

    [Test]
    public void TakeDamage_ReducesHealthWithoutGoingBelowZero()
    {
        Player player = new(Vector2.Zero);
        player.TakeDamage(150);
        Assert.That(player.Health, Is.Zero);
        Assert.That(player.IsAlive, Is.False);
    }

    [Test]
    public void TakeDamage_WithNegativeAmount_Throws()
    {
        Player player = new(Vector2.Zero);
        Assert.That(() => player.TakeDamage(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Heal_DoesNotExceedMaximumHealth()
    {
        Player player = new(Vector2.Zero);
        player.TakeDamage(10);
        player.Heal(50);
        Assert.That(player.Health, Is.EqualTo(player.MaximumHealth));
    }

    [Test]
    public void Move_WithDiagonalInput_UsesNormalizedSpeed()
    {
        Player player = new(new Vector2(500f, 300f));
        Rectangle largeArena = new(0, 0, 2000, 2000);

        player.Move(new Vector2(1f, 1f), 1f, largeArena);

        float travelled = Vector2.Distance(new Vector2(500f, 300f), player.Position);
        Assert.That(travelled, Is.EqualTo(Player.MovementSpeed).Within(0.01f));
    }

    [Test]
    public void Move_ClampsPlayerInsideArena()
    {
        Player player = new(new Vector2(50f, 50f));
        Rectangle arena = new(0, 0, 100, 100);

        player.Move(new Vector2(-1f, -1f), 10f, arena);

        Assert.That(player.Bounds.Left, Is.GreaterThanOrEqualTo(arena.Left));
        Assert.That(player.Bounds.Top, Is.GreaterThanOrEqualTo(arena.Top));
    }

    [Test]
    public void TryShoot_EnforcesCooldown()
    {
        Player player = new(Vector2.Zero);

        Assert.That(player.TryShoot(), Is.True);
        Assert.That(player.TryShoot(), Is.False);
        player.Update(Player.ShotCooldown);
        Assert.That(player.TryShoot(), Is.True);
    }
}
