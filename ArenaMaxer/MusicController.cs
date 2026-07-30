using Microsoft.Xna.Framework.Media;
using System;
using System.IO;

namespace ArenaMaxer;

/// <summary>controls the optional soundtrack, section transitions, fades, and looping.</summary>
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

    // loads the music without letting an audio problem crash the game.
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

    // starts and loops the opening section used by menus.
    public void StartMenu()
    {
        if (!_isAvailable)
            return;

        _section = MusicSection.Menu;
        PlayFrom(TimeSpan.Zero, AudioTimeline.MenuVolume);
    }

    // jumps to the gameplay section and begins its fade-in.
    public void StartGameplay()
    {
        if (!_isAvailable)
            return;

        _section = MusicSection.Gameplay;
        PlayFrom(TimeSpan.FromSeconds(AudioTimeline.GameplayStartSeconds), 0f);
        BeginFade(0f, AudioTimeline.GameplayVolume, AudioTimeline.GameplayFadeSeconds);
    }

    // lowers the soundtrack slightly for an ending screen.
    public void EnterGameOver()
    {
        if (!_isAvailable)
            return;

        _section = MusicSection.GameOver;
        BeginFade(MediaPlayer.Volume, AudioTimeline.GameOverVolume, 1.2f);
    }

    // pauses the soundtrack at its current position.
    public void Pause()
    {
        if (_isAvailable && MediaPlayer.State == MediaState.Playing)
            MediaPlayer.Pause();
    }

    // resumes the soundtrack from the paused position.
    public void Resume()
    {
        if (_isAvailable && MediaPlayer.State == MediaState.Paused)
            MediaPlayer.Resume();
    }

    // updates volume fades and restarts the correct loop when needed.
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

    // stores the values needed to interpolate a volume change.
    private void BeginFade(float start, float target, float duration)
    {
        _fadeElapsed = 0f;
        _fadeDuration = duration;
        _fadeStart = start;
        _fadeTarget = target;
        MediaPlayer.Volume = start;
    }

    // advances an active fade using the shared timeline calculation.
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

    // starts the theme at a chosen time and volume.
    private void PlayFrom(TimeSpan position, float volume)
    {
        MediaPlayer.Volume = volume;
        MediaPlayer.Play(_theme, position);
    }

    // shuts audio down after an unexpected playback failure.
    private void DisableAudio()
    {
        _isAvailable = false;
        try
        {
            MediaPlayer.Stop();
        }
        catch (Exception)
        {
            // audio is optional, so a platform failure should not stop gameplay.
        }
    }

    // stops playback and releases the loaded song during shutdown.
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
                // the audio device may already be released during shutdown.
            }
        }

        _theme?.Dispose();
    }
}
