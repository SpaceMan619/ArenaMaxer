namespace ArenaMaxer;

/// <summary>Represents the currently active screen and permitted game behaviour.</summary>
public enum GameState
{
    Start,
    Credits,
    Playing,
    UpgradeSelection,
    BossBattle,
    Paused,
    GameOver,
    Victory
}
