using Microsoft.Xna.Framework;
using NUnit.Framework;

namespace ArenaMaxer.Tests;

/// <summary>contains fifteen tests for the game's core rules and mathematics.</summary>
public sealed class ArenaMaxerCoreTests
{
    [Test]
    // checks the constructor gives a new player full health.
    public void NewPlayer_StartsAtMaximumHealth()
    {
        Player player = new(Vector2.Zero);
        Assert.That(player.Health, Is.EqualTo(player.MaximumHealth));
    }

    [Test]
    // checks large damage values cannot create negative health.
    public void Damage_NeverReducesHealthBelowZero()
    {
        Player player = new(Vector2.Zero);
        player.TakeDamage(150);
        Assert.That(player.Health, Is.Zero);
    }

    [Test]
    // checks healing stops at the player's current maximum health.
    public void Healing_DoesNotExceedMaximumHealth()
    {
        Player player = new(Vector2.Zero);
        player.TakeDamage(10);
        player.Heal(50);
        Assert.That(player.Health, Is.EqualTo(player.MaximumHealth));
    }

    [Test]
    // checks normalization removes the diagonal speed advantage.
    public void DiagonalMovement_IsNormalizedToPlayerSpeed()
    {
        Player player = new(new Vector2(500f, 300f));
        player.Move(new Vector2(1f, 1f), 1f, new Rectangle(0, 0, 2000, 2000));
        Assert.That(Vector2.Distance(new Vector2(500f, 300f), player.Position),
            Is.EqualTo(Player.MovementSpeed).Within(0.01f));
    }

    [Test]
    // checks another shot is blocked until the cooldown ends.
    public void Shooting_UsesItsCooldown()
    {
        Player player = new(Vector2.Zero);
        Assert.That(player.TryShoot(), Is.True);
        Assert.That(player.TryShoot(), Is.False);
        player.Update(Player.ShotCooldown);
        Assert.That(player.TryShoot(), Is.True);
    }

    [Test]
    // checks projectile movement uses direction, speed, and time.
    public void Projectile_MovesByDirectionSpeedAndTime()
    {
        Projectile projectile = new(Vector2.Zero, Vector2.UnitX);
        projectile.Update(0.5f);
        Assert.That(projectile.Position.X, Is.EqualTo(Projectile.Speed * 0.5f).Within(0.001f));
    }

    [Test]
    // checks one standard projectile defeats the weaker enemy.
    public void Rusher_DiesFromOneStandardShot()
    {
        RusherEnemy rusher = new(Vector2.Zero);
        Assert.That(rusher.TakeDamage(Projectile.DefaultDamage), Is.True);
    }

    [Test]
    // checks the tank survives two standard hits and falls on the third.
    public void Tank_RequiresThreeStandardShots()
    {
        TankEnemy tank = new(Vector2.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(tank.TakeDamage(Projectile.DefaultDamage), Is.False);
            Assert.That(tank.TakeDamage(Projectile.DefaultDamage), Is.False);
            Assert.That(tank.TakeDamage(Projectile.DefaultDamage), Is.True);
        });
    }

    [Test]
    // checks each normal wave increases its enemy quota by four.
    public void WaveQuota_IncreasesFromFifteenEnemies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DifficultyCalculator.EnemiesRequiredForWave(1), Is.EqualTo(15));
            Assert.That(DifficultyCalculator.EnemiesRequiredForWave(2), Is.EqualTo(19));
        });
    }

    [Test]
    // checks an upgrade waits until the final active enemy is removed.
    public void WaveCompletion_RequiresNoActiveEnemies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DifficultyCalculator.IsWaveComplete(15, 1, 15), Is.False);
            Assert.That(DifficultyCalculator.IsWaveComplete(15, 0, 15), Is.True);
        });
    }

    [Test]
    // checks distance and pickup range follow the three-four-five triangle.
    public void DistanceAndPickupRange_UseThePythagoreanRule()
    {
        Assert.Multiple(() =>
        {
            Assert.That(MathUtilities.Distance(Vector2.Zero, new Vector2(3f, 4f)), Is.EqualTo(5f));
            Assert.That(CollisionHelper.IsWithinDistance(Vector2.Zero, new Vector2(3f, 4f), 5f), Is.True);
        });
    }

    [Test]
    // checks a positive dot product recognizes a target in front.
    public void DotProduct_RecognizesAForwardTarget()
    {
        Assert.That(MathUtilities.Dot(Vector2.UnitX, new Vector2(3f, 1f)), Is.GreaterThan(0f));
    }

    [Test]
    // checks the cross-product sign recognizes the chosen side.
    public void CrossProduct_RecognizesTheLeftSide()
    {
        Assert.That(MathUtilities.Cross(Vector2.UnitX, new Vector2(0f, -1f)), Is.LessThan(0f));
    }

    [Test]
    // checks boss preparation can unlock all three projectiles.
    public void TripleShot_IsUnlockedForBossPreparation()
    {
        Player player = new(Vector2.Zero);
        player.ApplyUpgrade(UpgradeType.TripleShot);
        Assert.That(player.ProjectileCount, Is.EqualTo(3));
    }

    [Test]
    // checks the boss timers produce shots and reinforcement pairs.
    public void Boss_TimesItsShotsAndRusherReinforcements()
    {
        BossEnemy boss = new(Vector2.Zero);
        Assert.Multiple(() =>
        {
            Assert.That(boss.TryFire(BossEnemy.FireInterval), Is.True);
            Assert.That(boss.TrySpawnMinions(BossEnemy.MinionSpawnInterval), Is.True);
            Assert.That(BossEnemy.MinionsPerSpawn, Is.EqualTo(2));
        });
    }
}
