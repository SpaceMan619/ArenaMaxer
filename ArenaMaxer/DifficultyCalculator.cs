using System;

namespace ArenaMaxer;

/// <summary>centralizes wave and spawning algebra so difficulty rules are testable.</summary>
public static class DifficultyCalculator
{
    // keeps enemy speed and damage at ninety percent of their base values.
    public const float GameplayDifficulty = 0.9f;

    public const float MinimumSpawnInterval = 0.475f;

    // returns the enemy quota required to clear a wave.
    // this starts manageable and gets four enemies heavier each wave.
    public static int EnemiesRequiredForWave(int wave) =>
        11 + Math.Max(1, wave) * 4;

    // requires the full quota to spawn and the arena to be empty.
    public static bool IsWaveComplete(int enemiesSpawned, int activeEnemies, int requiredEnemies) =>
        enemiesSpawned >= requiredEnemies && activeEnemies == 0;

    // shortens the spawn delay by wave while respecting a safe minimum.
    public static float SpawnInterval(int wave)
    {
        // the lower limit stops later waves from becoming impossible.
        int safeWave = Math.Max(1, wave);
        float originalInterval = 1.35f - (safeWave - 1) * 0.11f;
        return Math.Max(MinimumSpawnInterval, originalInterval / GameplayDifficulty);
    }

    // applies the shared balance multiplier to an enemy's base speed.
    public static float EnemySpeed(float originalSpeed) =>
        Math.Max(0f, originalSpeed) * GameplayDifficulty;

    // applies the shared balance multiplier to contact damage.
    public static int ContactDamage(int originalDamage) =>
        Math.Max(1, (int)MathF.Round(
            Math.Max(0, originalDamage) * GameplayDifficulty,
            MidpointRounding.AwayFromZero));

    // increases tank frequency as the player reaches later waves.
    public static bool ShouldSpawnTank(int spawnNumber, int wave)
    {
        int frequency = Math.Max(3, 7 - Math.Max(1, wave));
        return spawnNumber > 0 && spawnNumber % frequency == 0;
    }
}
