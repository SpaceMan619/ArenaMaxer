using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace ArenaMaxer.Tests;

public sealed class UpgradeTests
{
    [Test]
    public void MaxHealthUpgrade_IncreasesMaximumAndRestoresHealth()
    {
        Player player = new(Vector2.Zero);
        player.TakeDamage(40);

        player.ApplyUpgrade(UpgradeType.MaxHealth);

        Assert.Multiple(() =>
        {
            Assert.That(player.MaximumHealth, Is.EqualTo(125));
            Assert.That(player.Health, Is.EqualTo(85));
        });
    }

    [Test]
    public void DoubleShotUpgrade_AddsAProjectileAndStopsAtTwo()
    {
        Player player = new(Vector2.Zero);
        player.ApplyUpgrade(UpgradeType.DoubleShot);

        Assert.Multiple(() =>
        {
            Assert.That(player.ProjectileCount, Is.EqualTo(2));
            Assert.That(player.CanApplyUpgrade(UpgradeType.DoubleShot), Is.False);
            Assert.That(() => player.ApplyUpgrade(UpgradeType.DoubleShot), Throws.InvalidOperationException);
        });
    }

    [Test]
    public void BulletDamageUpgrade_IncreasesProjectileDamage()
    {
        Player player = new(Vector2.Zero);

        player.ApplyUpgrade(UpgradeType.BulletDamage);

        Assert.That(player.ProjectileDamage, Is.EqualTo(Projectile.DefaultDamage + 5));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void AttackPattern_CreatesRequestedNumberOfNormalizedDirections(int projectileCount)
    {
        Vector2[] directions = AttackPattern.CreateDirections(Vector2.UnitY, projectileCount);

        Assert.That(directions, Has.Length.EqualTo(projectileCount));
        Assert.That(directions, Has.All.Matches<Vector2>(direction =>
            MathF.Abs(direction.Length() - 1f) < 0.001f));
    }

    [Test]
    public void Projectile_UsesUpgradedDamageValue()
    {
        Projectile projectile = new(Vector2.Zero, Vector2.UnitX, 25);
        Assert.That(projectile.Damage, Is.EqualTo(25));
    }

    [TestCase(9, 1, 10, false)]
    [TestCase(10, 1, 10, false)]
    [TestCase(10, 0, 10, true)]
    [TestCase(14, 0, 10, true)]
    public void WaveCompletion_RequiresEveryEnemyToBeRemoved(
        int enemiesSpawned, int activeEnemies, int requiredEnemies, bool expected)
    {
        Assert.That(DifficultyCalculator.IsWaveComplete(enemiesSpawned, activeEnemies, requiredEnemies),
            Is.EqualTo(expected));
    }
}
