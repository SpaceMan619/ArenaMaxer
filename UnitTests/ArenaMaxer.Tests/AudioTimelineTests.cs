using NUnit.Framework;

namespace ArenaMaxer.Tests;

public sealed class AudioTimelineTests
{
    [Test]
    public void MenuAndGameplaySections_MeetAtThirtyNineSeconds()
    {
        Assert.That(AudioTimeline.MenuLoopEndSeconds, Is.EqualTo(AudioTimeline.GameplayStartSeconds));
    }

    [TestCase(0f, 0f)]
    [TestCase(1.75f, 0.21f)]
    [TestCase(3.5f, 0.42f)]
    [TestCase(10f, 0.42f)]
    public void FadeVolume_InterpolatesAndClamps(float elapsedSeconds, float expectedVolume)
    {
        float volume = AudioTimeline.FadeVolume(
            elapsedSeconds,
            AudioTimeline.GameplayFadeSeconds,
            0f,
            AudioTimeline.GameplayVolume);

        Assert.That(volume, Is.EqualTo(expectedVolume).Within(0.001f));
    }
}
