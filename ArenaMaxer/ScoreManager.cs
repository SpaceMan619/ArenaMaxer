using System;

namespace ArenaMaxer;

/// <summary>Owns score calculations for enemy defeats, pickups, and survival.</summary>
public sealed class ScoreManager
{
    public int Score { get; private set; }

    public void AddEnemyDefeat(int value)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(nameof(value));
        Score += value;
    }

    public void AddPowerUpPickup() => Score += 25;

    public void AddSurvivalSecond(int wave) => Score += Math.Max(1, wave);

    public void Reset() => Score = 0;
}
