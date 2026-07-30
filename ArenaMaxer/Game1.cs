using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace ArenaMaxer;

/// <summary>coordinates the game's states, input, collisions, audio, and drawing.</summary>
public sealed class Game1 : Game
{
    public const int ScreenWidth = 1024;
    public const int ScreenHeight = 600;
    private const int FinalBossWave = 5;
    private static readonly Rectangle ArenaBounds = new(20, 76, ScreenWidth - 40, ScreenHeight - 108);
    private static readonly Rectangle PlayButton = new(ScreenWidth / 2 - 110, 340, 220, 58);
    private static readonly Rectangle CreditsButton = new(ScreenWidth / 2 - 110, 410, 220, 58);
    private static readonly Rectangle CreditsBackButton = new(ScreenWidth / 2 - 110, 468, 220, 58);
    private static readonly Rectangle EndingPlayButton = new(270, 430, 220, 58);
    private static readonly Rectangle EndingMenuButton = new(534, 430, 220, 58);
    private static readonly Rectangle PauseResumeButton = new(ScreenWidth / 2 - 110, 314, 220, 58);
    private static readonly Rectangle PauseMenuButton = new(ScreenWidth / 2 - 110, 388, 220, 58);
    private static readonly Rectangle HealthUpgradeCard = new(92, 226, 260, 210);
    private static readonly Rectangle DoubleShotUpgradeCard = new(382, 226, 260, 210);
    private static readonly Rectangle DamageUpgradeCard = new(672, 226, 260, 210);
    private static readonly Color Ink = new(7, 10, 24);
    private static readonly Color Shadow = new(2, 4, 12);
    private static readonly Color PanelDark = new(14, 22, 46);
    private static readonly Color PanelMid = new(25, 39, 72);
    private static readonly Color ArenaFloor = new(20, 31, 55);
    private static readonly Color ArenaTile = new(25, 38, 66);
    private static readonly Color Cyan = new(48, 216, 220);
    private static readonly Color Blue = new(53, 138, 238);
    private static readonly Color Gold = new(255, 203, 71);
    private static readonly Color Crimson = new(235, 64, 82);
    private static readonly Color SoftWhite = new(224, 237, 244);

    private readonly GraphicsDeviceManager _graphics;
    private readonly Random _random = new();
    private readonly List<Enemy> _enemies = new();
    private readonly List<Projectile> _projectiles = new();
    private readonly List<EnemyProjectile> _enemyProjectiles = new();
    private readonly List<PowerUp> _powerUps = new();
    private readonly ScoreManager _scoreManager = new();

    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private ArcadeSoundBank _sounds = null!;
    private MusicController _music = null!;
    private Player _player = null!;
    private BossEnemy _boss = null!;
    private GameState _state = GameState.Start;
    private GameState _stateBeforePause = GameState.Playing;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private float _spawnTimer;
    private float _powerUpTimer;
    private float _survivalScoreTimer;
    private float _elapsedSurvivalTime;
    private float _displayedHealth = Player.StartingMaximumHealth;
    private float _screenFade;
    private float _dangerTint;
    private int _wave = 1;
    private int _spawnNumber;
    private int _enemiesSpawnedThisWave;
    private int _highScore;
    private bool _isBossPreparation;
    private float _bossContactCooldown;
    private string _statusMessage = string.Empty;
    private float _statusTimer;

    // configures the game window and content folder before loading begins.
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "ArenaMaxer v1.1";
    }

    // creates graphics resources, loads audio, and resets the first run.
    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _sounds = new ArcadeSoundBank();
        _music = new MusicController();
        if (_music.TryLoad(GetMusicPath()))
            _music.StartMenu();
        else
            Console.Error.WriteLine($"ArenaMaxer music unavailable: {_music.LastError}");

        string highScorePath = GetHighScorePath();
        _highScore = HighScoreStorage.Load(highScorePath);
        ResetGame();
        _state = GameState.Start;
    }

    // reads input and updates only the logic allowed by the current game state.
    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();
        float deltaTime = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);

        bool enterPressed = IsNewKeyPress(keyboard, Keys.Enter);
        bool escapePressed = IsNewKeyPress(keyboard, Keys.Escape);
        bool clicked = mouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released;
        bool clickedPlay = clicked && PlayButton.Contains(mouse.Position);
        bool clickedCredits = clicked && CreditsButton.Contains(mouse.Position);
        bool clickedCreditsBack = clicked && CreditsBackButton.Contains(mouse.Position);
        bool clickedEndingPlay = clicked && EndingPlayButton.Contains(mouse.Position);
        bool clickedEndingMenu = clicked && EndingMenuButton.Contains(mouse.Position);
        bool clickedResume = clicked && PauseResumeButton.Contains(mouse.Position);
        bool clickedMenu = clicked && PauseMenuButton.Contains(mouse.Position);

        switch (_state)
        {
            case GameState.Start:
                _screenFade = MathHelper.Lerp(_screenFade, 1f, 5f * deltaTime);
                if (escapePressed)
                    Exit();
                else if (IsNewKeyPress(keyboard, Keys.C) || clickedCredits)
                {
                    _state = GameState.Credits;
                    _screenFade = 0f;
                }
                else if (enterPressed || clickedPlay)
                {
                    ResetGame();
                    _state = GameState.Playing;
                    _screenFade = 0f;
                    _music.StartGameplay();
                }
                break;
            case GameState.Credits:
                _screenFade = MathHelper.Lerp(_screenFade, 1f, 5f * deltaTime);
                if (escapePressed || enterPressed || clickedCreditsBack)
                {
                    _state = GameState.Start;
                    _screenFade = 0f;
                }
                break;
            case GameState.Playing:
                if (escapePressed)
                    PauseGame();
                else
                    UpdatePlaying(deltaTime, keyboard);
                break;
            case GameState.UpgradeSelection:
                if (escapePressed)
                    PauseGame();
                else
                    UpdateUpgradeSelection(deltaTime, keyboard, mouse);
                break;
            case GameState.BossBattle:
                if (escapePressed)
                    PauseGame();
                else
                    UpdateBossBattle(deltaTime, keyboard);
                break;
            case GameState.Paused:
                _screenFade = MathHelper.Lerp(_screenFade, 1f, 5f * deltaTime);
                if (escapePressed || enterPressed || clickedResume)
                    ResumeGame();
                else if (clickedMenu)
                    ReturnToMainMenu();
                break;
            case GameState.GameOver:
            case GameState.Victory:
                _screenFade = MathHelper.Lerp(_screenFade, 1f, 4f * deltaTime);
                if (escapePressed || clickedEndingMenu)
                    ReturnToMainMenu();
                else if (enterPressed || clickedEndingPlay)
                {
                    ResetGame();
                    _state = GameState.Playing;
                    _screenFade = 0f;
                    _music.StartGameplay();
                }
                break;
        }

        // freezing this timer keeps the fade exactly where it was when paused.
        if (_state != GameState.Paused)
            _music.Update(deltaTime);

        _previousKeyboard = keyboard;
        _previousMouse = mouse;
        base.Update(gameTime);
    }

    // remembers the active state before freezing gameplay and music.
    private void PauseGame()
    {
        // keep the exact game state so resume returns to the right place.
        _stateBeforePause = _state;
        _state = GameState.Paused;
        _screenFade = 0f;
        _music.Pause();
    }

    // returns to the exact state that was active before pausing.
    private void ResumeGame()
    {
        _state = _stateBeforePause;
        _screenFade = 0f;
        _music.Resume();
    }

    // resets the run and returns the soundtrack to its menu section.
    private void ReturnToMainMenu()
    {
        ResetGame();
        _state = GameState.Start;
        _screenFade = 0f;
        _music.StartMenu();
    }

    // updates one frame of a normal enemy wave.
    private void UpdatePlaying(float deltaTime, KeyboardState keyboard)
    {
        _elapsedSurvivalTime += deltaTime;
        UpdatePlayerControls(deltaTime, keyboard);

        UpdateSpawning(deltaTime);
        UpdateEnemies(deltaTime);
        UpdatePlayerProjectiles(deltaTime);

        HandleCollisions();
        RemoveExpiredProjectiles();
        UpdateScoring(deltaTime);

        if (DifficultyCalculator.IsWaveComplete(
            _enemiesSpawnedThisWave,
            _enemies.Count,
            DifficultyCalculator.EnemiesRequiredForWave(_wave)))
        {
            // wave four leads to boss prep instead of another normal wave.
            BeginUpgradeSelection(_wave == FinalBossWave - 1);
            return;
        }

        UpdateGameplayFeedback(deltaTime);
    }

    // freezes combat and prepares the three upgrade choices.
    private void BeginUpgradeSelection(bool isBossPreparation)
    {
        // gameplay stops here so the player can choose without taking damage.
        _state = GameState.UpgradeSelection;
        _isBossPreparation = isBossPreparation;
        _screenFade = 0f;
        _statusTimer = 0f;
        _projectiles.Clear();
    }

    // reads keyboard or mouse input from the upgrade screen.
    private void UpdateUpgradeSelection(float deltaTime, KeyboardState keyboard, MouseState mouse)
    {
        _screenFade = MathHelper.Lerp(_screenFade, 1f, 5f * deltaTime);
        bool clicked = mouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released;

        UpgradeType? selectedUpgrade = null;
        if (IsNewKeyPress(keyboard, Keys.D1) || IsNewKeyPress(keyboard, Keys.NumPad1)
            || (clicked && HealthUpgradeCard.Contains(mouse.Position)))
        {
            selectedUpgrade = UpgradeType.MaxHealth;
        }
        else if (IsNewKeyPress(keyboard, Keys.D2) || IsNewKeyPress(keyboard, Keys.NumPad2)
            || (clicked && DoubleShotUpgradeCard.Contains(mouse.Position)))
        {
            selectedUpgrade = _isBossPreparation ? UpgradeType.TripleShot : UpgradeType.DoubleShot;
        }
        else if (IsNewKeyPress(keyboard, Keys.D3) || IsNewKeyPress(keyboard, Keys.NumPad3)
            || (clicked && DamageUpgradeCard.Contains(mouse.Position)))
        {
            selectedUpgrade = UpgradeType.BulletDamage;
        }

        if (selectedUpgrade.HasValue && _player.CanApplyUpgrade(selectedUpgrade.Value))
            ApplyUpgradeAndStartNextWave(selectedUpgrade.Value);
    }

    // applies the chosen upgrade and starts the next normal or boss wave.
    private void ApplyUpgradeAndStartNextWave(UpgradeType upgrade)
    {
        _player.ApplyUpgrade(upgrade);
        if (_isBossPreparation)
        {
            // boss prep skips normal spawning and starts the final encounter.
            _wave = FinalBossWave;
            _boss = new BossEnemy(new Vector2(ScreenWidth / 2f, ArenaBounds.Top + 90f));
            _enemyProjectiles.Clear();
            _powerUps.Clear();
            _bossContactCooldown = 0f;
            _isBossPreparation = false;
            _displayedHealth = _player.Health;
            _statusMessage = "BOSS ONLINE";
            _statusTimer = 2f;
            _screenFade = 0f;
            _state = GameState.BossBattle;
            _sounds.PlayWaveStart();
            return;
        }

        _wave++;
        _spawnTimer = 1.2f;
        _powerUpTimer = 8f;
        _enemiesSpawnedThisWave = 0;
        _displayedHealth = _player.Health;
        _statusMessage = $"WAVE {_wave}";
        _statusTimer = 2f;
        _screenFade = 0f;
        _state = GameState.Playing;
        _sounds.PlayPickup();
        _sounds.PlayWaveStart();
    }

    // controls enemy quotas, spawn timing, and occasional health pickups.
    private void UpdateSpawning(float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f
            && _enemiesSpawnedThisWave < DifficultyCalculator.EnemiesRequiredForWave(_wave))
        {
            // the quota check makes sure a wave only contains its planned enemies.
            _spawnNumber++;
            _enemiesSpawnedThisWave++;
            _enemies.Add(CreateEnemy(_spawnNumber));
            _spawnTimer = DifficultyCalculator.SpawnInterval(_wave);
        }

        _powerUpTimer -= deltaTime;
        if (_powerUpTimer <= 0f && _powerUps.Count < 1)
        {
            Vector2 position = new(
                _random.Next(ArenaBounds.Left + 40, ArenaBounds.Right - 40),
                _random.Next(ArenaBounds.Top + 40, ArenaBounds.Bottom - 40));
            _powerUps.Add(new PowerUp(position, PowerUpType.Health));
            _powerUpTimer = 20f;
        }
    }

    // updates the boss, reinforcements, hostile shots, and player combat.
    private void UpdateBossBattle(float deltaTime, KeyboardState keyboard)
    {
        if (_boss is null)
        {
            EndVictory();
            return;
        }

        _elapsedSurvivalTime += deltaTime;
        UpdatePlayerControls(deltaTime, keyboard);

        _boss.Update(_player.Position, deltaTime);
        if (_boss.TryFire(deltaTime))
        {
            // purple boss shots are dodge-only; player bullets cannot remove them.
            Vector2 direction = MathUtilities.Direction(_boss.Position, _player.Position);
            if (direction != Vector2.Zero)
            {
                _enemyProjectiles.Add(new EnemyProjectile(_boss.Position, direction, BossEnemy.ProjectileDamage));
                _sounds.PlayEnemyHit();
            }
        }

        if (_boss.TrySpawnMinions(deltaTime))
        {
            // rusher pairs force the player to divide attention during the boss fight.
            for (int count = 0; count < BossEnemy.MinionsPerSpawn; count++)
                _enemies.Add(new RusherEnemy(RandomEdgePosition()));
            _statusMessage = "RUSHER REINFORCEMENTS";
            _statusTimer = 1.1f;
        }

        UpdateEnemies(deltaTime);
        UpdatePlayerProjectiles(deltaTime);
        foreach (EnemyProjectile projectile in _enemyProjectiles)
            projectile.Update(deltaTime);

        HandleCollisions();
        HandleBossCollisions(deltaTime);
        RemoveExpiredProjectiles();
        RemoveExpiredEnemyProjectiles();
        UpdateScoring(deltaTime);

        UpdateGameplayFeedback(deltaTime);
    }

    // shares player movement and firing between normal and boss waves.
    private void UpdatePlayerControls(float deltaTime, KeyboardState keyboard)
    {
        Vector2 movement = ReadMovement(keyboard);
        _player.Move(movement, deltaTime, ArenaBounds);
        _player.Update(deltaTime);
        TryFirePlayer(keyboard);
    }

    // gives every active enemy one movement update.
    private void UpdateEnemies(float deltaTime)
    {
        foreach (Enemy enemy in _enemies)
            enemy.Update(_player.Position, deltaTime);
    }

    // moves every projectile currently fired by the player.
    private void UpdatePlayerProjectiles(float deltaTime)
    {
        foreach (Projectile projectile in _projectiles)
            projectile.Update(deltaTime);
    }

    // smooths visual feedback and ends the run when health reaches zero.
    private void UpdateGameplayFeedback(float deltaTime)
    {
        if (_statusTimer > 0f)
            _statusTimer -= deltaTime;

        // these three lerps smooth the health bar, screen fade, and danger tint.
        _displayedHealth = MathHelper.Lerp(_displayedHealth, _player.Health, 8f * deltaTime);
        _screenFade = MathHelper.Lerp(_screenFade, 0f, 5f * deltaTime);
        float dangerTarget = _player.Health <= 30 ? 1f : 0f;
        _dangerTint = MathHelper.Lerp(_dangerTint, dangerTarget, 3f * deltaTime);

        if (!_player.IsAlive)
            EndGame();
    }

    // creates the current single, double, or triple-shot attack.
    private void TryFirePlayer(KeyboardState keyboard)
    {
        if (!IsNewKeyPress(keyboard, Keys.Space) || !_player.TryShoot())
            return;

        foreach (Vector2 direction in AttackPattern.CreateDirections(
            _player.FacingDirection,
            _player.ProjectileCount))
        {
            _projectiles.Add(new Projectile(_player.Position, direction, _player.ProjectileDamage));
        }
        _sounds.PlayFire();
    }

    // chooses a tank or rusher according to wave difficulty.
    private Enemy CreateEnemy(int spawnNumber)
    {
        Vector2 position = RandomEdgePosition();
        return DifficultyCalculator.ShouldSpawnTank(spawnNumber, _wave)
            ? new TankEnemy(position)
            : new RusherEnemy(position);
    }

    // selects a random position along one edge of the arena.
    private Vector2 RandomEdgePosition()
    {
        int side = _random.Next(4);
        return side switch
        {
            0 => new Vector2(ArenaBounds.Left + 10, _random.Next(ArenaBounds.Top, ArenaBounds.Bottom)),
            1 => new Vector2(ArenaBounds.Right - 10, _random.Next(ArenaBounds.Top, ArenaBounds.Bottom)),
            2 => new Vector2(_random.Next(ArenaBounds.Left, ArenaBounds.Right), ArenaBounds.Top + 10),
            _ => new Vector2(_random.Next(ArenaBounds.Left, ArenaBounds.Right), ArenaBounds.Bottom - 10)
        };
    }

    // resolves player, enemy, projectile, and power-up collisions.
    private void HandleCollisions()
    {
        for (int projectileIndex = _projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            // work backwards because a hit can remove this projectile from the list.
            Projectile projectile = _projectiles[projectileIndex];
            bool projectileHit = false;

            for (int enemyIndex = _enemies.Count - 1; enemyIndex >= 0; enemyIndex--)
            {
                Enemy enemy = _enemies[enemyIndex];
                if (!CollisionHelper.Intersects(projectile.Bounds, enemy.Bounds))
                    continue;

                projectileHit = true;
                bool defeated = enemy.TakeDamage(projectile.Damage);
                if (defeated)
                {
                    _scoreManager.AddEnemyDefeat(enemy.ScoreValue);
                    _enemies.RemoveAt(enemyIndex);
                    _sounds.PlayEnemyDefeat();
                }
                else
                {
                    _sounds.PlayEnemyHit();
                }
                break;
            }

            if (projectileHit)
                _projectiles.RemoveAt(projectileIndex);
        }

        for (int enemyIndex = _enemies.Count - 1; enemyIndex >= 0; enemyIndex--)
        {
            Enemy enemy = _enemies[enemyIndex];
            if (!CollisionHelper.Intersects(_player.Bounds, enemy.Bounds))
                continue;

            _player.TakeDamage(enemy.ContactDamage);
            _enemies.RemoveAt(enemyIndex);
            _sounds.PlayPlayerDamage();
            _statusMessage = $"-{enemy.ContactDamage} HEALTH";
            _statusTimer = 0.8f;
        }

        for (int powerUpIndex = _powerUps.Count - 1; powerUpIndex >= 0; powerUpIndex--)
        {
            PowerUp powerUp = _powerUps[powerUpIndex];
            float pickupDistance = (_player.Size + powerUp.Size) / 2f;
            if (!CollisionHelper.IsWithinDistance(_player.Position, powerUp.Position, pickupDistance))
                continue;

            powerUp.ApplyTo(_player);
            _scoreManager.AddPowerUpPickup();
            _powerUps.RemoveAt(powerUpIndex);
            _sounds.PlayPickup();
            _statusMessage = "+HEALTH";
            _statusTimer = 1.2f;
        }
    }

    // resolves dodge-only boss shots, boss damage, and contact damage.
    private void HandleBossCollisions(float deltaTime)
    {
        if (_boss is null)
            return;

        for (int projectileIndex = _projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
            // boss shots only collide with the player, not with player bullets.
            Projectile playerProjectile = _projectiles[projectileIndex];
            if (!CollisionHelper.Intersects(playerProjectile.Bounds, _boss.Bounds))
                continue;

            bool defeated = _boss.TakeDamage(playerProjectile.Damage);
            _projectiles.RemoveAt(projectileIndex);
            if (defeated)
            {
                _scoreManager.AddEnemyDefeat(_boss.ScoreValue);
                EndVictory();
                return;
            }
            _sounds.PlayEnemyHit();
        }

        _bossContactCooldown = Math.Max(0f, _bossContactCooldown - deltaTime);
        if (_bossContactCooldown <= 0f && CollisionHelper.Intersects(_player.Bounds, _boss.Bounds))
        {
            // contact has a short cooldown so touching the boss is not instant death.
            _player.TakeDamage(_boss.ContactDamage);
            _bossContactCooldown = 1f;
            _sounds.PlayPlayerDamage();
            _statusMessage = $"-{_boss.ContactDamage} HEALTH";
            _statusTimer = 0.8f;
        }

        for (int index = _enemyProjectiles.Count - 1; index >= 0; index--)
        {
            EnemyProjectile projectile = _enemyProjectiles[index];
            if (!CollisionHelper.Intersects(_player.Bounds, projectile.Bounds))
                continue;

            _player.TakeDamage(projectile.Damage);
            _enemyProjectiles.RemoveAt(index);
            _sounds.PlayPlayerDamage();
            _statusMessage = $"-{projectile.Damage} HEALTH";
            _statusTimer = 0.8f;
        }
    }

    // removes player projectiles after their lifetime ends.
    private void RemoveExpiredProjectiles()
    {
        for (int index = _projectiles.Count - 1; index >= 0; index--)
        {
            if (!_projectiles[index].IsActive || !ArenaBounds.Intersects(_projectiles[index].Bounds))
                _projectiles.RemoveAt(index);
        }
    }

    // removes hostile projectiles after their lifetime ends.
    private void RemoveExpiredEnemyProjectiles()
    {
        for (int index = _enemyProjectiles.Count - 1; index >= 0; index--)
        {
            if (!_enemyProjectiles[index].IsActive || !ArenaBounds.Intersects(_enemyProjectiles[index].Bounds))
                _enemyProjectiles.RemoveAt(index);
        }
    }

    // awards survival points once for each completed second.
    private void UpdateScoring(float deltaTime)
    {
        _survivalScoreTimer += deltaTime;
        while (_survivalScoreTimer >= 1f)
        {
            _scoreManager.AddSurvivalSecond(_wave);
            _survivalScoreTimer -= 1f;
        }
    }

    // saves the score and opens the game-over screen.
    private void EndGame()
    {
        _state = GameState.GameOver;
        _screenFade = 0f;
        _sounds.PlayGameOver();
        _music.EnterGameOver();
        if (_scoreManager.Score > _highScore)
        {
            _highScore = _scoreManager.Score;
            HighScoreStorage.Save(GetHighScorePath(), _highScore);
        }
    }

    // saves the score and opens the victory screen.
    private void EndVictory()
    {
        _state = GameState.Victory;
        _screenFade = 0f;
        _sounds.PlayWaveStart();
        _music.EnterGameOver();
        if (_scoreManager.Score > _highScore)
        {
            _highScore = _scoreManager.Score;
            HighScoreStorage.Save(GetHighScorePath(), _highScore);
        }
    }

    // rebuilds all temporary gameplay state for a fresh run.
    private void ResetGame()
    {
        _player = new Player(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f));
        _enemies.Clear();
        _projectiles.Clear();
        _enemyProjectiles.Clear();
        _powerUps.Clear();
        _boss = null;
        _scoreManager.Reset();
        _spawnTimer = DifficultyCalculator.SpawnInterval(1);
        _powerUpTimer = 12f;
        _survivalScoreTimer = 0f;
        _elapsedSurvivalTime = 0f;
        _displayedHealth = _player.MaximumHealth;
        _dangerTint = 0f;
        _wave = 1;
        _spawnNumber = 0;
        _enemiesSpawnedThisWave = 0;
        _isBossPreparation = false;
        _bossContactCooldown = 0f;
        _statusTimer = 0f;
        _statusMessage = string.Empty;
    }

    // draws the arena first and then the screen that matches the game state.
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Ink);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawArena();
        if (_state == GameState.Start)
            DrawStartScreen();
        else if (_state == GameState.Credits)
            DrawCreditsScreen();
        else
        {
            DrawEntities();
            DrawHud();
            if (_state == GameState.GameOver)
                DrawGameOverScreen();
            else if (_state == GameState.Victory)
                DrawVictoryScreen();
            else if (_state == GameState.Paused)
                DrawPauseScreen();
            else if (_state == GameState.UpgradeSelection)
                DrawUpgradeSelectionScreen();
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    // draws the tiled arena floor, borders, and corner markers.
    private void DrawArena()
    {
        DrawRectangle(new Rectangle(ArenaBounds.X + 6, ArenaBounds.Y + 6, ArenaBounds.Width, ArenaBounds.Height), Shadow);
        DrawRectangle(ArenaBounds, ArenaFloor);

        const int tileSize = 48;
        for (int y = ArenaBounds.Top; y < ArenaBounds.Bottom; y += tileSize)
        {
            for (int x = ArenaBounds.Left; x < ArenaBounds.Right; x += tileSize)
            {
                int column = (x - ArenaBounds.Left) / tileSize;
                int row = (y - ArenaBounds.Top) / tileSize;
                if ((column + row) % 2 == 0)
                {
                    int width = Math.Min(tileSize, ArenaBounds.Right - x);
                    int height = Math.Min(tileSize, ArenaBounds.Bottom - y);
                    DrawRectangle(new Rectangle(x, y, width, height), ArenaTile);
                }
            }
        }

        for (int x = ArenaBounds.Left + 96; x < ArenaBounds.Right; x += 96)
            DrawRectangle(new Rectangle(x, ArenaBounds.Top, 2, ArenaBounds.Height), new Color(31, 48, 79));
        for (int y = ArenaBounds.Top + 96; y < ArenaBounds.Bottom; y += 96)
            DrawRectangle(new Rectangle(ArenaBounds.Left, y, ArenaBounds.Width, 2), new Color(31, 48, 79));

        DrawBorder(ArenaBounds, 5, PanelMid);
        Rectangle innerBorder = ArenaBounds;
        innerBorder.Inflate(-5, -5);
        DrawBorder(innerBorder, 2, Cyan);
        DrawArenaCornerMarkers();

        if (_dangerTint > 0.01f)
            DrawRectangle(ArenaBounds, Crimson * (_dangerTint * 0.15f));
    }

    // draws every active player, enemy, projectile, and pickup.
    private void DrawEntities()
    {
        foreach (PowerUp powerUp in _powerUps)
        {
            DrawPixelBox(powerUp.Bounds, new Color(55, 213, 117), new Color(163, 255, 188));
            Rectangle vertical = new(powerUp.Bounds.Center.X - 3, powerUp.Bounds.Top + 5, 6, powerUp.Size - 10);
            Rectangle horizontal = new(powerUp.Bounds.Left + 5, powerUp.Bounds.Center.Y - 3, powerUp.Size - 10, 6);
            DrawRectangle(vertical, SoftWhite);
            DrawRectangle(horizontal, SoftWhite);
        }

        foreach (Projectile projectile in _projectiles)
        {
            Rectangle trail = new(projectile.Bounds.X + 3, projectile.Bounds.Y + 3,
                projectile.Bounds.Width, projectile.Bounds.Height);
            DrawRectangle(trail, new Color(135, 70, 28));
            DrawRectangle(projectile.Bounds, Gold);
            DrawBorder(projectile.Bounds, 2, new Color(255, 242, 164));
        }

        foreach (EnemyProjectile projectile in _enemyProjectiles)
            DrawPixelBox(projectile.Bounds, new Color(218, 83, 255), new Color(255, 173, 255));

        foreach (Enemy enemy in _enemies)
        {
            bool isTank = enemy is TankEnemy;
            Color colour = isTank ? new Color(146, 76, 210) : Crimson;
            if (enemy.HitFlash > 0f)
                colour = Color.White;
            DrawPixelBox(enemy.Bounds, colour, isTank ? new Color(220, 151, 255) : new Color(255, 150, 132));
            DrawEnemyDetails(enemy, isTank);
            DrawEnemyHealth(enemy);
        }

        if (_boss is not null)
        {
            Color bossColour = _boss.HitFlash > 0f ? Color.White : new Color(124, 58, 190);
            DrawPixelBox(_boss.Bounds, bossColour, new Color(228, 142, 255));
            Rectangle bossCore = new(_boss.Bounds.Center.X - 15, _boss.Bounds.Center.Y - 15, 30, 30);
            DrawRectangle(bossCore, Ink);
            DrawBorder(bossCore, 4, Gold);
            DrawEnemyHealth(_boss);
        }

        DrawPixelBox(_player.Bounds, Blue, new Color(143, 224, 255));
        Rectangle core = new(_player.Bounds.Center.X - 6, _player.Bounds.Center.Y - 6, 12, 12);
        DrawRectangle(core, Cyan);
        DrawPlayerFacingIndicator();
    }

    // draws a small marker showing the player's firing direction.
    private void DrawPlayerFacingIndicator()
    {
        Vector2 facing = _player.FacingDirection;
        Vector2 centre = _player.Position + facing * (_player.Size / 2f + 7f);
        Rectangle indicator = Math.Abs(facing.X) > Math.Abs(facing.Y)
            ? new Rectangle((int)centre.X - 9, (int)centre.Y - 3, 18, 6)
            : new Rectangle((int)centre.X - 3, (int)centre.Y - 9, 6, 18);
        DrawRectangle(new Rectangle(indicator.X + 3, indicator.Y + 3, indicator.Width, indicator.Height), Shadow);
        DrawRectangle(indicator, new Color(168, 241, 255));
    }

    // draws a health bar above enemies that need more than one hit.
    private void DrawEnemyHealth(Enemy enemy)
    {
        int width = enemy.Bounds.Width;
        Rectangle background = new(enemy.Bounds.Left, enemy.Bounds.Top - 9, width, 5);
        float percentage = enemy.Health / (float)enemy.MaximumHealth;
        Rectangle foreground = new(background.X, background.Y, (int)(background.Width * percentage), background.Height);
        DrawRectangle(background, Shadow);
        DrawRectangle(foreground, new Color(76, 230, 126));
    }

    // draws score, wave, time, health, and control information.
    private void DrawHud()
    {
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, 68), PanelDark);
        DrawRectangle(new Rectangle(0, 64, ScreenWidth, 4), Cyan);

        DrawStatPanel(new Rectangle(12, 10, 172, 44), "SCORE", _scoreManager.Score.ToString(), Gold);
        DrawStatPanel(new Rectangle(192, 10, 166, 44), "HIGH", _highScore.ToString(), SoftWhite);
        DrawStatPanel(new Rectangle(366, 10, 126, 44), _state == GameState.BossBattle ? "BOSS" : "WAVE",
            _wave.ToString(), Gold);
        DrawStatPanel(new Rectangle(500, 10, 160, 44), "TIME", $"{(int)_elapsedSurvivalTime}s", SoftWhite);

        Rectangle healthPanel = new(668, 10, 344, 44);
        DrawPanel(healthPanel, PanelMid, Crimson);
        Rectangle healthBackground = new(748, 20, 250, 24);
        const int barWidth = 250;
        int healthWidth = (int)(barWidth * Math.Clamp(_displayedHealth / _player.MaximumHealth, 0f, 1f));
        DrawRectangle(healthBackground, new Color(66, 22, 38));
        DrawRectangle(new Rectangle(healthBackground.X, healthBackground.Y, healthWidth, healthBackground.Height),
            _player.Health > 30 ? new Color(55, 213, 117) : Crimson);
        DrawBorder(healthBackground, 2, SoftWhite);
        _spriteBatch.DrawString(_font, "HP", new Vector2(684, 21), Gold);
        DrawCentredText($"{_player.Health}", healthBackground, SoftWhite);

        if (_statusTimer > 0f)
        {
            Rectangle statusPanel = new(ScreenWidth / 2 - 125, 88, 250, 42);
            DrawPanel(statusPanel, PanelDark, Gold);
            DrawCentredText(_statusMessage, statusPanel, Gold);
        }
        else if (_state == GameState.Playing)
        {
            int enemiesRemaining = DifficultyCalculator.EnemiesRequiredForWave(_wave) - _enemiesSpawnedThisWave
                + _enemies.Count;
            Rectangle statusPanel = new(ScreenWidth / 2 - 125, 88, 250, 42);
            DrawPanel(statusPanel, PanelDark, Cyan);
            DrawCentredText($"{enemiesRemaining} ENEMIES LEFT", statusPanel, SoftWhite);
        }
        else if (_state == GameState.BossBattle)
        {
            Rectangle statusPanel = new(ScreenWidth / 2 - 125, 88, 250, 42);
            DrawPanel(statusPanel, PanelDark, Crimson);
            DrawCentredText("DODGE THE BARRAGE", statusPanel, SoftWhite);
        }

        Rectangle controlsPanel = new(132, 570, 760, 28);
        DrawRectangle(controlsPanel, PanelDark);
        DrawBorder(controlsPanel, 2, PanelMid);
        DrawControlHint(new Rectangle(144, 572, 330, 24), "MOVE", "WASD / ARROWS");
        DrawRectangle(new Rectangle(480, 575, 2, 18), PanelMid);
        DrawControlHint(new Rectangle(490, 572, 210, 24), "FIRE", "SPACE");
        DrawRectangle(new Rectangle(706, 575, 2, 18), PanelMid);
        DrawControlHint(new Rectangle(716, 572, 164, 24), "PAUSE", "ESC");
    }

    // draws the paused choice between the three permanent upgrades.
    private void DrawUpgradeSelectionScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.76f * alpha));

        Rectangle panel = new(54, 112, 916, 382);
        DrawPanel(panel, PanelDark * alpha, Gold * alpha);
        DrawCentredText(_isBossPreparation ? "BOSS PREP" : $"WAVE {_wave} CLEAR",
            new Rectangle(0, 132, ScreenWidth, 46), Gold * alpha);
        DrawCentredText(_isBossPreparation ? "CHOOSE ONE UPGRADE FOR THE FINAL BATTLE" : "CHOOSE ONE PERMANENT UPGRADE",
            new Rectangle(0, 174, ScreenWidth, 30),
            SoftWhite * alpha);

        MouseState mouse = Mouse.GetState();
        DrawUpgradeCard(HealthUpgradeCard, "1  CORE BOOST", "+25 MAX HP", $"CURRENT {_player.MaximumHealth}",
            UpgradeType.MaxHealth, mouse, alpha);
        DrawUpgradeCard(DoubleShotUpgradeCard,
            _isBossPreparation ? "2  TRIPLE SHOT" : "2  DOUBLE SHOT",
            _isBossPreparation ? "3 PROJECTILES" : "+1 PROJECTILE",
            $"CURRENT {_player.ProjectileCount}",
            _isBossPreparation ? UpgradeType.TripleShot : UpgradeType.DoubleShot, mouse, alpha);
        DrawUpgradeCard(DamageUpgradeCard, "3  DAMAGE CHIP", "+5 BULLET DAMAGE", $"CURRENT {_player.ProjectileDamage}",
            UpgradeType.BulletDamage, mouse, alpha);

        DrawCentredText("CLICK A CARD OR PRESS 1 / 2 / 3", new Rectangle(0, 455, ScreenWidth, 28),
            new Color(151, 181, 205) * alpha);
    }

    // draws one upgrade card with its title, benefit, and current value.
    private void DrawUpgradeCard(Rectangle area, string title, string benefit, string current,
        UpgradeType upgrade, MouseState mouse, float alpha)
    {
        bool available = _player.CanApplyUpgrade(upgrade);
        bool hovered = available && area.Contains(mouse.Position);
        Color accent = !available ? PanelMid : hovered ? Cyan : Blue;
        Color text = available ? SoftWhite : new Color(105, 124, 151);

        DrawPanel(area, PanelMid * alpha, accent * alpha);
        DrawRectangle(new Rectangle(area.X + 12, area.Y + 12, area.Width - 24, 6), accent * alpha);
        DrawCentredText(title, new Rectangle(area.X + 8, area.Y + 38, area.Width - 16, 34),
            (available ? Gold : text) * alpha);
        DrawCentredText(available ? benefit : "MAXED", new Rectangle(area.X + 8, area.Y + 94, area.Width - 16, 32),
            text * alpha);
        DrawCentredText(current, new Rectangle(area.X + 8, area.Y + 140, area.Width - 16, 28),
            new Color(151, 181, 205) * alpha);
    }

    // draws the game title, objective, controls, and main buttons.
    private void DrawStartScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Ink * (0.76f * alpha));
        Rectangle menuPanel = new(178, 66, 668, 480);
        DrawPanel(menuPanel, PanelDark * alpha, Cyan * alpha);
        DrawRectangle(new Rectangle(190, 78, 644, 8), Gold * alpha);
        DrawRectangle(new Rectangle(190, 526, 644, 8), Blue * alpha);

        DrawCentredText("A R E N A M A X E R", new Rectangle(0, 112, ScreenWidth, 58), Cyan * alpha);
        DrawCentredText("SURVIVE  /  UPGRADE  /  DEFEND", new Rectangle(0, 174, ScreenWidth, 36), Gold * alpha);
        DrawCentredText("CLEAR FOUR WAVES AND DEFEAT THE FINAL GUARDIAN",
            new Rectangle(0, 224, ScreenWidth, 30), new Color(184, 204, 221) * alpha);
        DrawCentredText("MOVE: WASD OR ARROWS     FIRE: SPACE",
            new Rectangle(0, 265, ScreenWidth, 28), SoftWhite * alpha);
        DrawButton("PLAY", alpha);
        DrawButton("CREDITS", alpha, CreditsButton);
        DrawCentredText("GAME DEVELOPED BY PROJECT FUTURE", new Rectangle(0, 484, ScreenWidth, 24),
            new Color(151, 181, 205) * alpha);
        DrawCentredText("VERSION 1.1", new Rectangle(700, 510, 126, 24), Gold * alpha);
        if (!_music.IsAvailable)
        {
            DrawCentredText("MUSIC UNAVAILABLE", new Rectangle(198, 510, 260, 24), Crimson * alpha);
        }
    }

    // draws development, music, sound, and framework credits.
    private void DrawCreditsScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Ink * (0.82f * alpha));
        Rectangle panel = new(142, 58, 740, 488);
        DrawPanel(panel, PanelDark * alpha, Gold * alpha);
        DrawRectangle(new Rectangle(154, 70, 716, 8), Cyan * alpha);

        DrawCentredText("C R E D I T S", new Rectangle(0, 100, ScreenWidth, 48), Gold * alpha);
        DrawCentredText("GAME DEVELOPED BY PROJECT FUTURE", new Rectangle(0, 158, ScreenWidth, 30),
            SoftWhite * alpha);
        DrawCentredText("MUSIC", new Rectangle(0, 210, ScreenWidth, 28), Cyan * alpha);
        DrawCentredText("ARENAMAXER THEME  /  THEMEMUSIC.OGG", new Rectangle(0, 246, ScreenWidth, 26),
            SoftWhite * alpha);
        DrawCentredText("TRACK PROVIDED BY PROJECT FUTURE", new Rectangle(0, 280, ScreenWidth, 26),
            new Color(184, 204, 221) * alpha);
        DrawCentredText("SOUND EFFECTS", new Rectangle(0, 326, ScreenWidth, 28), Cyan * alpha);
        DrawCentredText("ORIGINAL PROCEDURAL AUDIO GENERATED IN C SHARP",
            new Rectangle(0, 360, ScreenWidth, 26), SoftWhite * alpha);
        DrawCentredText("BUILT WITH C SHARP AND MONOGAME", new Rectangle(0, 408, ScreenWidth, 26),
            new Color(184, 204, 221) * alpha);
        DrawButton("BACK", alpha, CreditsBackButton);
    }

    // draws the final score and navigation after the player loses.
    private void DrawGameOverScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.78f * alpha));
        Rectangle gameOverPanel = new(208, 82, 608, 450);
        DrawPanel(gameOverPanel, PanelDark * alpha, Crimson * alpha);
        DrawRectangle(new Rectangle(220, 124, 584, 8), Crimson * alpha);
        DrawCentredText("SYSTEM DOWN", new Rectangle(0, 165, ScreenWidth, 54), Crimson * alpha);
        DrawStatPanel(new Rectangle(314, 235, 190, 44), "SCORE", _scoreManager.Score.ToString(), SoftWhite * alpha);
        DrawStatPanel(new Rectangle(520, 235, 190, 44), "TIME", $"{(int)_elapsedSurvivalTime}s", SoftWhite * alpha);
        DrawStatPanel(new Rectangle(390, 292, 244, 44), "HIGH", _highScore.ToString(), Gold * alpha);
        DrawButton("PLAY AGAIN", alpha, EndingPlayButton);
        DrawButton("MAIN MENU", alpha, EndingMenuButton);
    }

    // draws resume and main-menu choices over frozen gameplay.
    private void DrawPauseScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.72f * alpha));
        Rectangle panel = new(282, 138, 460, 344);
        DrawPanel(panel, PanelDark * alpha, Cyan * alpha);
        DrawRectangle(new Rectangle(294, 150, 436, 8), Gold * alpha);
        DrawCentredText("SYSTEM PAUSED", new Rectangle(0, 190, ScreenWidth, 48), Cyan * alpha);
        DrawCentredText("MUSIC AND GAMEPLAY ARE ON HOLD", new Rectangle(0, 242, ScreenWidth, 28),
            SoftWhite * alpha);
        DrawButton("RESUME", alpha, PauseResumeButton);
        DrawButton("MAIN MENU", alpha, PauseMenuButton);
        DrawCentredText("ESC OR ENTER TO RESUME", new Rectangle(0, 456, ScreenWidth, 24),
            new Color(151, 181, 205) * alpha);
    }

    // draws the final score and navigation after defeating the boss.
    private void DrawVictoryScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Ink * (0.82f * alpha));
        Rectangle panel = new(112, 82, 800, 438);
        DrawPanel(panel, PanelDark * alpha, Gold * alpha);
        DrawRectangle(new Rectangle(126, 96, 772, 8), Cyan * alpha);
        DrawRectangle(new Rectangle(126, 496, 772, 8), Gold * alpha);
        DrawCentredText("ARENA SECURED", new Rectangle(0, 138, ScreenWidth, 58), Gold * alpha);
        DrawCentredText("FINAL GUARDIAN DEFEATED", new Rectangle(0, 200, ScreenWidth, 34),
            SoftWhite * alpha);
        DrawCentredText("THE ARENA IS YOURS", new Rectangle(0, 242, ScreenWidth, 30), Cyan * alpha);
        DrawStatPanel(new Rectangle(278, 300, 218, 48), "SCORE", _scoreManager.Score.ToString(), SoftWhite * alpha);
        DrawStatPanel(new Rectangle(528, 300, 218, 48), "TIME", $"{(int)_elapsedSurvivalTime}s", SoftWhite * alpha);
        DrawStatPanel(new Rectangle(390, 364, 244, 48), "HIGH", _highScore.ToString(), Gold * alpha);
        DrawButton("PLAY AGAIN", alpha, EndingPlayButton);
        DrawButton("MAIN MENU", alpha, EndingMenuButton);
    }

    // draws the default main-menu button area.
    private void DrawButton(string text, float alpha)
    {
        DrawButton(text, alpha, PlayButton);
    }

    // draws a clickable pixel button with hover feedback.
    private void DrawButton(string text, float alpha, Rectangle area)
    {
        MouseState mouse = Mouse.GetState();
        bool hovered = area.Contains(mouse.Position);
        Color buttonColour = hovered ? Cyan : Blue;
        DrawRectangle(new Rectangle(area.X + 7, area.Y + 7, area.Width, area.Height),
            Shadow * alpha);
        DrawRectangle(area, Gold * alpha);
        Rectangle buttonInner = area;
        buttonInner.Inflate(-4, -4);
        DrawRectangle(buttonInner, buttonColour * alpha);
        DrawBorder(buttonInner, 3, PanelDark * alpha);
        DrawCentredText($"> {text} <", buttonInner, Color.White * alpha);
    }

    // draws one labelled value inside a small hud panel.
    private void DrawStatPanel(Rectangle area, string label, string value, Color accent)
    {
        DrawPanel(area, PanelMid, accent);
        Vector2 labelSize = _font.MeasureString(label);
        Vector2 valueSize = _font.MeasureString(value);
        float textY = area.Y + (area.Height - Math.Max(labelSize.Y, valueSize.Y)) / 2f;
        _spriteBatch.DrawString(_font, label, new Vector2(area.X + 12, textY), accent);
        _spriteBatch.DrawString(_font, value, new Vector2(area.Right - 12 - valueSize.X, textY), SoftWhite);
    }

    // draws one compact key and action reminder.
    private void DrawControlHint(Rectangle area, string label, string value)
    {
        Vector2 labelSize = MeasureSpacedText(label);
        Vector2 valueSize = MeasureSpacedText(value);
        float totalWidth = labelSize.X + 18f + valueSize.X;
        float x = area.X + (area.Width - totalWidth) / 2f;
        float y = area.Y + (area.Height - Math.Max(labelSize.Y, valueSize.Y)) / 2f;
        DrawSpacedText(label, new Vector2(x, y), Gold);
        DrawSpacedText(value, new Vector2(x + labelSize.X + 18f, y), new Color(151, 181, 205));
    }

    // draws a shadowed panel with a coloured border.
    private void DrawPanel(Rectangle area, Color fill, Color accent)
    {
        DrawRectangle(new Rectangle(area.X + 4, area.Y + 4, area.Width, area.Height), Shadow);
        DrawRectangle(area, accent);
        Rectangle inner = area;
        inner.Inflate(-3, -3);
        DrawRectangle(inner, fill);
        DrawBorder(inner, 2, PanelMid);
    }

    // draws a smaller pixel-styled box used inside larger panels.
    private void DrawPixelBox(Rectangle area, Color fill, Color highlight)
    {
        DrawRectangle(new Rectangle(area.X + 4, area.Y + 4, area.Width, area.Height), Shadow);
        DrawRectangle(area, Ink);
        Rectangle inner = area;
        inner.Inflate(-3, -3);
        DrawRectangle(inner, fill);
        DrawRectangle(new Rectangle(inner.X, inner.Y, inner.Width, 3), highlight);
        DrawRectangle(new Rectangle(inner.X, inner.Y, 3, inner.Height), highlight);
    }

    // draws the extra markings that distinguish rushers from tanks.
    private void DrawEnemyDetails(Enemy enemy, bool isTank)
    {
        Rectangle bounds = enemy.Bounds;
        if (isTank)
        {
            DrawRectangle(new Rectangle(bounds.Left + 8, bounds.Top + 12, bounds.Width - 16, 7), Ink);
            DrawRectangle(new Rectangle(bounds.Left + 8, bounds.Bottom - 19, bounds.Width - 16, 7), Ink);
        }
        else
        {
            DrawRectangle(new Rectangle(bounds.Center.X - 9, bounds.Center.Y - 3, 5, 5), Gold);
            DrawRectangle(new Rectangle(bounds.Center.X + 4, bounds.Center.Y - 3, 5, 5), Gold);
        }
    }

    // draws decorative markers in all four arena corners.
    private void DrawArenaCornerMarkers()
    {
        const int length = 20;
        const int thickness = 4;
        DrawRectangle(new Rectangle(ArenaBounds.Left + 8, ArenaBounds.Top + 8, length, thickness), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Left + 8, ArenaBounds.Top + 8, thickness, length), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Right - 8 - length, ArenaBounds.Top + 8, length, thickness), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Right - 12, ArenaBounds.Top + 8, thickness, length), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Left + 8, ArenaBounds.Bottom - 12, length, thickness), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Left + 8, ArenaBounds.Bottom - 8 - length, thickness, length), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Right - 8 - length, ArenaBounds.Bottom - 12, length, thickness), Gold);
        DrawRectangle(new Rectangle(ArenaBounds.Right - 12, ArenaBounds.Bottom - 8 - length, thickness, length), Gold);
    }

    // measures text and places it in the centre of a rectangle.
    private void DrawCentredText(string text, Rectangle area, Color colour)
    {
        Vector2 textSize = MeasureSpacedText(text);
        Vector2 position = new(
            area.X + (area.Width - textSize.X) / 2f,
            area.Y + (area.Height - textSize.Y) / 2f);
        DrawSpacedText(text, position, colour);
    }

    // measures text while including the custom space between letters.
    private Vector2 MeasureSpacedText(string text)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return Vector2.Zero;

        const float wordGap = 12f;
        float textWidth = 0f;
        float textHeight = 0f;
        foreach (string word in words)
        {
            Vector2 wordSize = _font.MeasureString(word);
            textWidth += wordSize.X;
            textHeight = Math.Max(textHeight, wordSize.Y);
        }
        textWidth += wordGap * (words.Length - 1);
        return new Vector2(textWidth, textHeight);
    }

    // draws each character separately to create the arcade letter spacing.
    private void DrawSpacedText(string text, Vector2 position, Color colour)
    {
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        const float wordGap = 12f;
        foreach (string word in words)
        {
            _spriteBatch.DrawString(_font, word, position, colour);
            position.X += _font.MeasureString(word).X + wordGap;
        }
    }

    // fills one rectangle using the shared one-pixel texture.
    private void DrawRectangle(Rectangle rectangle, Color colour) =>
        _spriteBatch.Draw(_pixel, rectangle, colour);

    // draws a rectangular border using four thin filled rectangles.
    private void DrawBorder(Rectangle rectangle, int thickness, Color colour)
    {
        DrawRectangle(new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), colour);
        DrawRectangle(new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), colour);
        DrawRectangle(new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), colour);
        DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), colour);
    }

    // reports a key only on the frame when it changes from up to down.
    private bool IsNewKeyPress(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    // converts keyboard controls into a two-dimensional movement vector.
    private static Vector2 ReadMovement(KeyboardState keyboard)
    {
        Vector2 movement = Vector2.Zero;
        if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
            movement.Y -= 1f;
        if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
            movement.Y += 1f;
        if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            movement.X -= 1f;
        if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            movement.X += 1f;
        return movement;
    }

    // returns the safe local path used for the saved high score.
    private static string GetHighScorePath()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArenaMaxer");
        return Path.Combine(directory, "highscore.txt");
    }

    // returns the copied soundtrack path beside the built game files.
    private static string GetMusicPath() => Path.Combine(
        AppContext.BaseDirectory,
        "Content",
        "Audio",
        "ThemeMusic.ogg");

    // releases graphics, sound, and music resources when the game closes.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _music?.Dispose();
            _sounds?.Dispose();
            _pixel?.Dispose();
        }

        base.Dispose(disposing);
    }
}
