using System;

namespace ArenaMaxer;

/// <summary>Centralizes wave and spawning algebra so difficulty rules are testable.</summary>
public static class DifficultyCalculator
{
    public const float SecondsPerWave = 20f;
    public const float MinimumSpawnInterval = 0.38f;

    public static int WaveForTime(float elapsedSeconds) =>
        1 + (int)(Math.Max(0f, elapsedSeconds) / SecondsPerWave);

    public static float SpawnInterval(int wave)
    {
        int safeWave = Math.Max(1, wave);
        return Math.Max(MinimumSpawnInterval, 1.35f - (safeWave - 1) * 0.11f);
    }

    public static bool ShouldSpawnTank(int spawnNumber, int wave)
    {
        int frequency = Math.Max(3, 7 - Math.Max(1, wave));
        return spawnNumber > 0 && spawnNumber % frequency == 0;
    }
}
