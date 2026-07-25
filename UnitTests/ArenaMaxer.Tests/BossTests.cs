using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace ArenaMaxer.Tests;

public sealed class BossTests
{
    [Test]
    public void Boss_StartsWithFinalBattleStatistics()
    {
        BossEnemy boss = new(Vector2.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(boss.MaximumHealth, Is.EqualTo(520));
            Assert.That(boss.ScoreValue, Is.EqualTo(1000));
            Assert.That(boss.Size, Is.EqualTo(100));
        });
    }

    [Test]
    public void Boss_FiresAtItsConfiguredInterval()
    {
        BossEnemy boss = new(Vector2.Zero);

        Assert.That(boss.TryFire(BossEnemy.FireInterval - 0.01f), Is.False);
        Assert.That(boss.TryFire(0.01f), Is.True);
        Assert.That(boss.TryFire(0f), Is.False);
    }

    [Test]
    public void EnemyProjectile_MovesTowardItsDirection()
    {
        EnemyProjectile projectile = new(Vector2.Zero, Vector2.UnitX, BossEnemy.ProjectileDamage);

        projectile.Update(0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(projectile.Position.X, Is.EqualTo(EnemyProjectile.Speed * 0.5f).Within(0.001f));
            Assert.That(projectile.Damage, Is.EqualTo(BossEnemy.ProjectileDamage));
        });
    }

    [Test]
    public void Boss_SpawnsReinforcementsAtItsConfiguredInterval()
    {
        BossEnemy boss = new(Vector2.Zero);

        Assert.That(boss.TrySpawnMinions(BossEnemy.MinionSpawnInterval - 0.01f), Is.False);
        Assert.That(boss.TrySpawnMinions(0.01f), Is.True);
        Assert.That(BossEnemy.MinionsPerSpawn, Is.EqualTo(2));
    }
}
