using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace ArenaMaxer.Tests;

public sealed class GameplayLogicTests
{
    [Test]
    public void Projectile_MovesUsingDirectionSpeedAndTime()
    {
        Projectile projectile = new(Vector2.Zero, Vector2.UnitX);
        projectile.Update(0.5f);
        Assert.That(projectile.Position.X, Is.EqualTo(Projectile.Speed * 0.5f).Within(0.001f));
    }

    [Test]
    public void Rusher_IsDefeatedByOneStandardProjectile()
    {
        RusherEnemy enemy = new(Vector2.Zero);
        bool defeated = enemy.TakeDamage(Projectile.DefaultDamage);
        Assert.That(defeated, Is.True);
    }

    [Test]
    public void Tank_RequiresThreeStandardProjectileHits()
    {
        TankEnemy enemy = new(Vector2.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(enemy.TakeDamage(Projectile.DefaultDamage), Is.False);
            Assert.That(enemy.TakeDamage(Projectile.DefaultDamage), Is.False);
            Assert.That(enemy.TakeDamage(Projectile.DefaultDamage), Is.True);
        });
    }

    [TestCase(0f, 1)]
    [TestCase(24.99f, 1)]
    [TestCase(25f, 2)]
    [TestCase(75f, 4)]
    public void WaveForTime_CalculatesExpectedWave(float elapsedSeconds, int expectedWave)
    {
        Assert.That(DifficultyCalculator.WaveForTime(elapsedSeconds), Is.EqualTo(expectedWave));
    }

    [Test]
    public void SpawnInterval_DecreasesButNeverBelowMinimum()
    {
        float waveOne = DifficultyCalculator.SpawnInterval(1);
        float waveFive = DifficultyCalculator.SpawnInterval(5);
        float waveOneHundred = DifficultyCalculator.SpawnInterval(100);

        Assert.Multiple(() =>
        {
            Assert.That(waveFive, Is.LessThan(waveOne));
            Assert.That(waveOneHundred, Is.EqualTo(DifficultyCalculator.MinimumSpawnInterval));
        });
    }

    [Test]
    public void GameplayDifficulty_ReducesEnemySpeedAndDamageByTwentyPercent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DifficultyCalculator.EnemySpeed(100f), Is.EqualTo(80f));
            Assert.That(DifficultyCalculator.ContactDamage(25), Is.EqualTo(20));
        });
    }

    [Test]
    public void ScoreManager_AddsDefeatPickupAndSurvivalRewards()
    {
        ScoreManager score = new();
        score.AddEnemyDefeat(50);
        score.AddPowerUpPickup();
        score.AddSurvivalSecond(3);
        Assert.That(score.Score, Is.EqualTo(78));
    }

    [Test]
    public void HealthPowerUp_RestoresExpectedHealth()
    {
        Player player = new(Vector2.Zero);
        player.TakeDamage(50);
        PowerUp powerUp = new(Vector2.Zero, PowerUpType.Health);

        powerUp.ApplyTo(player);

        Assert.That(player.Health, Is.EqualTo(75));
    }
}
