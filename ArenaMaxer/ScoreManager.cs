using System;

namespace ArenaMaxer;

/// <summary>owns score calculations for enemy defeats, pickups, and survival.</summary>
public sealed class ScoreManager
{
    public int Score { get; private set; }

    // adds the score value of a defeated enemy.
    public void AddEnemyDefeat(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        Score += value;
    }

    // rewards the player for collecting a power-up.
    public void AddPowerUpPickup() => Score += 25;

    // rewards survival with more points during later waves.
    public void AddSurvivalSecond(int wave) => Score += Math.Max(1, wave);

    // clears the score when a new run begins.
    public void Reset() => Score = 0;
}
