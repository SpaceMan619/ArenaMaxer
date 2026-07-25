using System;

namespace ArenaMaxer;

/// <summary>Centralizes wave and spawning algebra so difficulty rules are testable.</summary>
public static class DifficultyCalculator
{
    /// <summary>
    /// Global balance multiplier. Enemies retain 90% of their original speed and damage.
    /// </summary>
    public const float GameplayDifficulty = 0.9f;

    public const float MinimumSpawnInterval = 0.475f;

    /// <summary>Returns the number of enemies that must be removed to clear a wave.</summary>
    // this starts manageable and gets four enemies heavier each wave.
    public static int EnemiesRequiredForWave(int wave) =>
        11 + Math.Max(1, wave) * 4;

    /// <summary>Reports whether every enemy required by a wave has spawned and been removed.</summary>
    public static bool IsWaveComplete(int enemiesSpawned, int activeEnemies, int requiredEnemies) =>
        enemiesSpawned >= requiredEnemies && activeEnemies == 0;

    public static float SpawnInterval(int wave)
    {
        // the lower limit stops later waves from becoming impossible.
        int safeWave = Math.Max(1, wave);
        float originalInterval = 1.35f - (safeWave - 1) * 0.11f;
        return Math.Max(MinimumSpawnInterval, originalInterval / GameplayDifficulty);
    }

    public static float EnemySpeed(float originalSpeed) =>
        Math.Max(0f, originalSpeed) * GameplayDifficulty;

    public static int ContactDamage(int originalDamage) =>
        Math.Max(1, (int)MathF.Round(
            Math.Max(0, originalDamage) * GameplayDifficulty,
            MidpointRounding.AwayFromZero));

    public static bool ShouldSpawnTank(int spawnNumber, int wave)
    {
        int frequency = Math.Max(3, 7 - Math.Max(1, wave));
        return spawnNumber > 0 && spawnNumber % frequency == 0;
    }
}
