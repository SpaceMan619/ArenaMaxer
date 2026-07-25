using System;

namespace ArenaMaxer;

/// <summary>Centralizes wave and spawning algebra so difficulty rules are testable.</summary>
public static class DifficultyCalculator
{
    /// <summary>
    /// Global balance multiplier. Version 1.1 reduces hostile speed, contact damage,
    /// and spawn frequency to 80% of their original values.
    /// </summary>
    public const float GameplayDifficulty = 0.8f;

    public const float SecondsPerWave = 25f;
    public const float MinimumSpawnInterval = 0.475f;

    public static int WaveForTime(float elapsedSeconds) =>
        1 + (int)(Math.Max(0f, elapsedSeconds) / SecondsPerWave);

    public static float SpawnInterval(int wave)
    {
        int safeWave = Math.Max(1, wave);
        float originalInterval = 1.35f - (safeWave - 1) * 0.11f;
        return Math.Max(MinimumSpawnInterval, originalInterval / GameplayDifficulty);
    }

    public static float EnemySpeed(float originalSpeed) =>
        Math.Max(0f, originalSpeed) * GameplayDifficulty;

    public static int ContactDamage(int originalDamage) =>
        Math.Max(1, (int)MathF.Round(Math.Max(0, originalDamage) * GameplayDifficulty));

    public static bool ShouldSpawnTank(int spawnNumber, int wave)
    {
        int frequency = Math.Max(3, 7 - Math.Max(1, wave));
        return spawnNumber > 0 && spawnNumber % frequency == 0;
    }
}
