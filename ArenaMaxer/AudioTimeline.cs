using System;

namespace ArenaMaxer;

/// <summary>Defines the timing rules used to transition and loop the game soundtrack.</summary>
public static class AudioTimeline
{
    public const float MenuLoopEndSeconds = 39f;
    public const float GameplayStartSeconds = 39f;
    public const float GameplayFadeSeconds = 3.5f;
    public const float MenuVolume = 0.28f;
    public const float GameplayVolume = 0.42f;
    public const float GameOverVolume = 0.20f;

    public static float FadeVolume(float elapsedSeconds, float durationSeconds, float start, float target)
    {
        if (durationSeconds <= 0f)
            return target;

        float amount = Math.Clamp(elapsedSeconds / durationSeconds, 0f, 1f);
        return start + (target - start) * amount;
    }
}
