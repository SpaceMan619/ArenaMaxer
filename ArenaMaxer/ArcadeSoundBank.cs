using Microsoft.Xna.Framework.Audio;
using System;

namespace ArenaMaxer;

/// <summary>generates the game's short arcade sound effects in memory.</summary>
public sealed class ArcadeSoundBank : IDisposable
{
    private const int SampleRate = 22050;
    private SoundEffect _fire;
    private SoundEffect _enemyHit;
    private SoundEffect _enemyDefeat;
    private SoundEffect _playerDamage;
    private SoundEffect _pickup;
    private SoundEffect _waveStart;
    private SoundEffect _gameOver;

    // creates each sound once so playback does not rebuild audio during the game.
    public ArcadeSoundBank()
    {
        try
        {
            _fire = CreateFire();
            _enemyHit = CreateEnemyHit();
            _enemyDefeat = CreateEnemyDefeat();
            _playerDamage = CreatePlayerDamage();
            _pickup = CreatePickup();
            _waveStart = CreateWaveStart();
            _gameOver = CreateGameOver();
        }
        catch (Exception)
        {
            Dispose();
        }
    }

    // plays the short sound used when the player fires.
    public void PlayFire() => _fire?.Play(0.34f, 0f, 0f);
    // plays the impact sound for a successful enemy hit.
    public void PlayEnemyHit() => _enemyHit?.Play(0.42f, 0f, 0f);
    // plays the sound used when an enemy is defeated.
    public void PlayEnemyDefeat() => _enemyDefeat?.Play(0.46f, 0f, 0f);
    // plays the heavier warning sound for player damage.
    public void PlayPlayerDamage() => _playerDamage?.Play(0.52f, 0f, 0f);
    // plays the rising sound for collecting health.
    public void PlayPickup() => _pickup?.Play(0.44f, 0f, 0f);
    // plays the short signal used when a wave begins.
    public void PlayWaveStart() => _waveStart?.Play(0.46f, 0f, 0f);
    // plays the falling sound used at game over.
    public void PlayGameOver() => _gameOver?.Play(0.54f, 0f, 0f);

    // creates a quick descending pulse for the player's shot.
    private static SoundEffect CreateFire()
    {
        uint noise = 0xA341316Cu;
        return Build(0.085f, (time, progress) =>
        {
            float frequency = Mix(880f, 330f, progress);
            float voice = Pulse(time, frequency, 0.32f) * 0.72f + NextNoise(ref noise) * 0.18f;
            return voice * PercussiveEnvelope(progress, 0.04f, 2.8f);
        });
    }

    // creates a noisy impact for an enemy taking damage.
    private static SoundEffect CreateEnemyHit()
    {
        uint noise = 0xC8013EA4u;
        return Build(0.075f, (time, progress) =>
        {
            float crunch = NextNoise(ref noise) * 0.70f;
            float body = Pulse(time, 145f, 0.46f) * 0.35f;
            return (crunch + body) * PercussiveEnvelope(progress, 0.015f, 3.4f);
        });
    }

    // creates a short descending phrase for an enemy defeat.
    private static SoundEffect CreateEnemyDefeat()
    {
        float[] notes = { 659.25f, 493.88f, 329.63f };
        uint noise = 0xAD90777Du;
        return Build(0.24f, (time, progress) =>
        {
            int noteIndex = Math.Min(notes.Length - 1, (int)(progress * notes.Length));
            float pulse = Pulse(time, notes[noteIndex], 0.40f) * 0.62f;
            float tail = NextNoise(ref noise) * 0.15f * progress;
            return (pulse + tail) * PercussiveEnvelope(progress, 0.025f, 1.8f);
        });
    }

    // creates a low noisy hit sound for player damage.
    private static SoundEffect CreatePlayerDamage()
    {
        uint noise = 0x7E95761Eu;
        return Build(0.18f, (time, progress) =>
        {
            float frequency = progress < 0.45f ? 164.81f : 110f;
            float body = Pulse(time, frequency, 0.47f) * 0.58f;
            float crunch = NextNoise(ref noise) * 0.32f;
            return (body + crunch) * PercussiveEnvelope(progress, 0.02f, 2.2f);
        });
    }

    // creates a rising three-note phrase for a pickup.
    private static SoundEffect CreatePickup()
    {
        float[] notes = { 523.25f, 659.25f, 783.99f };
        return Build(0.20f, (time, progress) =>
        {
            int noteIndex = Math.Min(notes.Length - 1, (int)(progress * notes.Length));
            float voice = Pulse(time, notes[noteIndex], 0.35f) * 0.55f
                + Triangle(time, notes[noteIndex] * 0.5f) * 0.22f;
            return voice * PercussiveEnvelope(progress, 0.04f, 1.5f);
        });
    }

    // creates a rising phrase that announces a new wave.
    private static SoundEffect CreateWaveStart()
    {
        float[] notes = { 392f, 523.25f, 659.25f, 783.99f };
        return Build(0.32f, (time, progress) =>
        {
            int noteIndex = Math.Min(notes.Length - 1, (int)(progress * notes.Length));
            float voice = Pulse(time, notes[noteIndex], 0.40f) * 0.52f
                + Triangle(time, notes[noteIndex] * 0.5f) * 0.20f;
            return voice * PercussiveEnvelope(progress, 0.035f, 1.2f);
        });
    }

    // creates the longer falling phrase used at game over.
    private static SoundEffect CreateGameOver()
    {
        float[] notes = { 392f, 311.13f, 246.94f, 196f };
        return Build(0.82f, (time, progress) =>
        {
            int noteIndex = Math.Min(notes.Length - 1, (int)(progress * notes.Length));
            float voice = Pulse(time, notes[noteIndex], 0.44f) * 0.42f
                + Triangle(time, notes[noteIndex] * 0.5f) * 0.32f;
            return voice * PercussiveEnvelope(progress, 0.025f, 1.15f);
        });
    }

    // converts a voice formula into mono pcm samples MonoGame can play.
    private static SoundEffect Build(float durationSeconds, Func<float, float, float> voice)
    {
        int sampleCount = (int)(SampleRate * durationSeconds);
        byte[] pcm = new byte[sampleCount * sizeof(short)];

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            float time = sampleIndex / (float)SampleRate;
            float progress = sampleIndex / (float)Math.Max(1, sampleCount - 1);
            float sample = Math.Clamp(voice(time, progress), -1f, 1f) * 0.72f;
            short value = (short)(sample * short.MaxValue);
            pcm[sampleIndex * 2] = (byte)(value & 0xff);
            pcm[sampleIndex * 2 + 1] = (byte)((value >> 8) & 0xff);
        }

        return new SoundEffect(pcm, SampleRate, AudioChannels.Mono);
    }

    // creates a square-like wave with an adjustable duty cycle.
    private static float Pulse(float time, float frequency, float dutyCycle)
    {
        float phase = time * frequency - MathF.Floor(time * frequency);
        return phase < dutyCycle ? 1f : -1f;
    }

    // creates a smoother triangle wave from the current phase.
    private static float Triangle(float time, float frequency)
    {
        float phase = time * frequency - MathF.Floor(time * frequency);
        return 1f - 4f * MathF.Abs(phase - 0.5f);
    }

    // creates repeatable pseudo-random noise for crunchy effects.
    private static float NextNoise(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return (state / (float)uint.MaxValue) * 2f - 1f;
    }

    // fades a sound in quickly and then lets it decay.
    private static float PercussiveEnvelope(float progress, float attackPortion, float decayPower)
    {
        float attack = attackPortion <= 0f ? 1f : Math.Min(1f, progress / attackPortion);
        float decay = MathF.Pow(Math.Max(0f, 1f - progress), decayPower);
        return attack * decay;
    }

    // blends between two values by the requested amount.
    private static float Mix(float start, float end, float amount) => start + (end - start) * amount;

    // releases every generated sound when the game closes.
    public void Dispose()
    {
        _fire?.Dispose();
        _enemyHit?.Dispose();
        _enemyDefeat?.Dispose();
        _playerDamage?.Dispose();
        _pickup?.Dispose();
        _waveStart?.Dispose();
        _gameOver?.Dispose();
    }
}
