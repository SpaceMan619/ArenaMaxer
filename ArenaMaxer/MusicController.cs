using Microsoft.Xna.Framework.Media;
using System;
using System.IO;

namespace ArenaMaxer;

/// <summary>Controls the optional local soundtrack, section transitions, fades, and looping.</summary>
public sealed class MusicController : IDisposable
{
    private enum MusicSection
    {
        None,
        Menu,
        Gameplay,
        GameOver
    }

    private Song _theme;
    private MusicSection _section;
    private float _fadeElapsed;
    private float _fadeDuration;
    private float _fadeStart;
    private float _fadeTarget;
    private bool _isAvailable;

    public bool IsAvailable => _isAvailable;
    public string LastError { get; private set; } = string.Empty;

    /// <summary>Loads a music file without allowing missing or unsupported audio to crash the game.</summary>
    public bool TryLoad(string path)
    {
        if (!File.Exists(path))
            return false;

        try
        {
            _theme = Song.FromUri("ArenaMaxer Theme", new Uri(Path.GetFullPath(path)));
            if (_theme.Duration <= TimeSpan.Zero)
            {
                LastError = "MonoGame could not decode the music file.";
                _theme.Dispose();
                return false;
            }
            MediaPlayer.IsRepeating = false;
            _isAvailable = true;
            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            _isAvailable = false;
            return false;
        }
    }

    public void StartMenu()
    {
        if (!_isAvailable)
            return;

        _section = MusicSection.Menu;
        PlayFrom(TimeSpan.Zero, AudioTimeline.MenuVolume);
    }

    public void StartGameplay()
    {
        if (!_isAvailable)
            return;

        _section = MusicSection.Gameplay;
        PlayFrom(TimeSpan.FromSeconds(AudioTimeline.GameplayStartSeconds), 0f);
        BeginFade(0f, AudioTimeline.GameplayVolume, AudioTimeline.GameplayFadeSeconds);
    }

    public void EnterGameOver()
    {
        if (!_isAvailable)
            return;

        _section = MusicSection.GameOver;
        BeginFade(MediaPlayer.Volume, AudioTimeline.GameOverVolume, 1.2f);
    }

    /// <summary>Pauses the current soundtrack position when gameplay is paused.</summary>
    public void Pause()
    {
        if (_isAvailable && MediaPlayer.State == MediaState.Playing)
            MediaPlayer.Pause();
    }

    /// <summary>Resumes the same soundtrack position after gameplay continues.</summary>
    public void Resume()
    {
        if (_isAvailable && MediaPlayer.State == MediaState.Paused)
            MediaPlayer.Resume();
    }

    public void Update(float deltaTime)
    {
        if (!_isAvailable)
            return;

        try
        {
            UpdateFade(Math.Max(0f, deltaTime));

            if (_section == MusicSection.Menu)
            {
                if (MediaPlayer.State == MediaState.Stopped
                    || MediaPlayer.PlayPosition.TotalSeconds >= AudioTimeline.MenuLoopEndSeconds)
                {
                    PlayFrom(TimeSpan.Zero, AudioTimeline.MenuVolume);
                }
            }
            else if ((_section == MusicSection.Gameplay || _section == MusicSection.GameOver)
                && MediaPlayer.State == MediaState.Stopped)
            {
                float volume = _section == MusicSection.GameOver
                    ? AudioTimeline.GameOverVolume
                    : AudioTimeline.GameplayVolume;
                PlayFrom(TimeSpan.FromSeconds(AudioTimeline.GameplayStartSeconds), volume);
            }
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            DisableAudio();
        }
    }

    private void BeginFade(float start, float target, float duration)
    {
        _fadeElapsed = 0f;
        _fadeDuration = duration;
        _fadeStart = start;
        _fadeTarget = target;
        MediaPlayer.Volume = start;
    }

    private void UpdateFade(float deltaTime)
    {
        if (_fadeElapsed >= _fadeDuration)
            return;

        _fadeElapsed += deltaTime;
        MediaPlayer.Volume = AudioTimeline.FadeVolume(
            _fadeElapsed,
            _fadeDuration,
            _fadeStart,
            _fadeTarget);
    }

    private void PlayFrom(TimeSpan position, float volume)
    {
        MediaPlayer.Volume = volume;
        MediaPlayer.Play(_theme, position);
    }

    private void DisableAudio()
    {
        _isAvailable = false;
        try
        {
            MediaPlayer.Stop();
        }
        catch (Exception)
        {
            // Audio is optional; a platform audio failure should not stop gameplay.
        }
    }

    public void Dispose()
    {
        if (_isAvailable)
        {
            try
            {
                MediaPlayer.Stop();
            }
            catch (Exception)
            {
                // The audio device may already have been released during shutdown.
            }
        }

        _theme?.Dispose();
    }
}
