using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace ArenaMaxer;

/// <summary>
/// Coordinates ArenaMaxer's game states, input, entity updates, collisions, and drawing.
/// Gameplay rules live in separate classes so they can be tested without a graphics device.
/// </summary>
public sealed class Game1 : Game
{
    public const int ScreenWidth = 1024;
    public const int ScreenHeight = 600;
    private static readonly Rectangle ArenaBounds = new(20, 76, ScreenWidth - 40, ScreenHeight - 108);
    private static readonly Rectangle PlayButton = new(ScreenWidth / 2 - 110, 355, 220, 58);
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
    private readonly List<PowerUp> _powerUps = new();
    private readonly ScoreManager _scoreManager = new();

    private SpriteBatch _spriteBatch = null!;
    private SpriteFont _font = null!;
    private Texture2D _pixel = null!;
    private Player _player = null!;
    private GameState _state = GameState.Start;
    private KeyboardState _previousKeyboard;
    private MouseState _previousMouse;
    private float _spawnTimer;
    private float _powerUpTimer;
    private float _survivalScoreTimer;
    private float _elapsedSurvivalTime;
    private float _displayedHealth = Player.MaximumHealth;
    private float _screenFade;
    private float _dangerTint;
    private int _wave = 1;
    private int _spawnNumber;
    private int _highScore;
    private string _statusMessage = string.Empty;
    private float _statusTimer;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = ScreenWidth,
            PreferredBackBufferHeight = ScreenHeight
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "ArenaMaxer";
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("DefaultFont");
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        string highScorePath = GetHighScorePath();
        _highScore = HighScoreStorage.Load(highScorePath);
        ResetGame();
        _state = GameState.Start;
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();
        float deltaTime = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);

        if (keyboard.IsKeyDown(Keys.Escape))
            Exit();

        bool enterPressed = IsNewKeyPress(keyboard, Keys.Enter);
        bool clickedPlay = mouse.LeftButton == ButtonState.Pressed
            && _previousMouse.LeftButton == ButtonState.Released
            && PlayButton.Contains(mouse.Position);

        switch (_state)
        {
            case GameState.Start:
                _screenFade = MathHelper.Lerp(_screenFade, 1f, 5f * deltaTime);
                if (enterPressed || clickedPlay)
                {
                    ResetGame();
                    _state = GameState.Playing;
                    _screenFade = 0f;
                }
                break;
            case GameState.Playing:
                UpdatePlaying(deltaTime, keyboard);
                break;
            case GameState.GameOver:
                _screenFade = MathHelper.Lerp(_screenFade, 1f, 4f * deltaTime);
                if (enterPressed || clickedPlay)
                {
                    ResetGame();
                    _state = GameState.Playing;
                    _screenFade = 0f;
                }
                break;
        }

        _previousKeyboard = keyboard;
        _previousMouse = mouse;
        base.Update(gameTime);
    }

    private void UpdatePlaying(float deltaTime, KeyboardState keyboard)
    {
        _elapsedSurvivalTime += deltaTime;
        int newWave = DifficultyCalculator.WaveForTime(_elapsedSurvivalTime);
        if (newWave != _wave)
        {
            _wave = newWave;
            _statusMessage = $"WAVE {_wave}";
            _statusTimer = 2f;
        }

        Vector2 movement = ReadMovement(keyboard);
        _player.Move(movement, deltaTime, ArenaBounds);
        _player.Update(deltaTime);

        bool shootPressed = IsNewKeyPress(keyboard, Keys.Space);
        if (shootPressed && _player.TryShoot())
            _projectiles.Add(new Projectile(_player.Position, _player.FacingDirection));

        UpdateSpawning(deltaTime);

        foreach (Enemy enemy in _enemies)
            enemy.Update(_player.Position, deltaTime);
        foreach (Projectile projectile in _projectiles)
            projectile.Update(deltaTime);

        HandleCollisions();
        RemoveExpiredProjectiles();
        UpdateScoring(deltaTime);

        if (_statusTimer > 0f)
            _statusTimer -= deltaTime;

        // Three distinct Lerp uses: animated health, screen transitions, and danger tint.
        _displayedHealth = MathHelper.Lerp(_displayedHealth, _player.Health, 8f * deltaTime);
        _screenFade = MathHelper.Lerp(_screenFade, 0f, 5f * deltaTime);
        float dangerTarget = _player.Health <= 30 ? 1f : 0f;
        _dangerTint = MathHelper.Lerp(_dangerTint, dangerTarget, 3f * deltaTime);

        if (!_player.IsAlive)
            EndGame();
    }

    private void UpdateSpawning(float deltaTime)
    {
        _spawnTimer -= deltaTime;
        if (_spawnTimer <= 0f)
        {
            _spawnNumber++;
            _enemies.Add(CreateEnemy(_spawnNumber));
            _spawnTimer = DifficultyCalculator.SpawnInterval(_wave);
        }

        _powerUpTimer -= deltaTime;
        if (_powerUpTimer <= 0f && _powerUps.Count < 2)
        {
            Vector2 position = new(
                _random.Next(ArenaBounds.Left + 40, ArenaBounds.Right - 40),
                _random.Next(ArenaBounds.Top + 40, ArenaBounds.Bottom - 40));
            _powerUps.Add(new PowerUp(position, PowerUpType.Health));
            _powerUpTimer = 14f;
        }
    }

    private Enemy CreateEnemy(int spawnNumber)
    {
        Vector2 position = RandomEdgePosition();
        return DifficultyCalculator.ShouldSpawnTank(spawnNumber, _wave)
            ? new TankEnemy(position)
            : new RusherEnemy(position);
    }

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

    private void HandleCollisions()
    {
        for (int projectileIndex = _projectiles.Count - 1; projectileIndex >= 0; projectileIndex--)
        {
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
            _statusMessage = "+HEALTH";
            _statusTimer = 1.2f;
        }
    }

    private void RemoveExpiredProjectiles()
    {
        for (int index = _projectiles.Count - 1; index >= 0; index--)
        {
            if (!_projectiles[index].IsActive || !ArenaBounds.Intersects(_projectiles[index].Bounds))
                _projectiles.RemoveAt(index);
        }
    }

    private void UpdateScoring(float deltaTime)
    {
        _survivalScoreTimer += deltaTime;
        while (_survivalScoreTimer >= 1f)
        {
            _scoreManager.AddSurvivalSecond(_wave);
            _survivalScoreTimer -= 1f;
        }
    }

    private void EndGame()
    {
        _state = GameState.GameOver;
        _screenFade = 0f;
        if (_scoreManager.Score > _highScore)
        {
            _highScore = _scoreManager.Score;
            HighScoreStorage.Save(GetHighScorePath(), _highScore);
        }
    }

    private void ResetGame()
    {
        _player = new Player(new Vector2(ScreenWidth / 2f, ScreenHeight / 2f));
        _enemies.Clear();
        _projectiles.Clear();
        _powerUps.Clear();
        _scoreManager.Reset();
        _spawnTimer = DifficultyCalculator.SpawnInterval(1);
        _powerUpTimer = 8f;
        _survivalScoreTimer = 0f;
        _elapsedSurvivalTime = 0f;
        _displayedHealth = Player.MaximumHealth;
        _dangerTint = 0f;
        _wave = 1;
        _spawnNumber = 0;
        _statusTimer = 0f;
        _statusMessage = string.Empty;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Ink);
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawArena();
        if (_state == GameState.Start)
            DrawStartScreen();
        else
        {
            DrawEntities();
            DrawHud();
            if (_state == GameState.GameOver)
                DrawGameOverScreen();
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

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

        DrawPixelBox(_player.Bounds, Blue, new Color(143, 224, 255));
        Rectangle core = new(_player.Bounds.Center.X - 6, _player.Bounds.Center.Y - 6, 12, 12);
        DrawRectangle(core, Cyan);
        DrawPlayerFacingIndicator();
    }

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

    private void DrawEnemyHealth(Enemy enemy)
    {
        int width = enemy.Bounds.Width;
        Rectangle background = new(enemy.Bounds.Left, enemy.Bounds.Top - 9, width, 5);
        float percentage = enemy.Health / (float)enemy.MaximumHealth;
        Rectangle foreground = new(background.X, background.Y, (int)(background.Width * percentage), background.Height);
        DrawRectangle(background, Shadow);
        DrawRectangle(foreground, new Color(76, 230, 126));
    }

    private void DrawHud()
    {
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, 68), PanelDark);
        DrawRectangle(new Rectangle(0, 64, ScreenWidth, 4), Cyan);

        DrawStatPanel(new Rectangle(12, 10, 172, 44), $"SCORE {_scoreManager.Score}", Gold);
        DrawStatPanel(new Rectangle(192, 10, 166, 44), $"HIGH {_highScore}", SoftWhite);
        DrawStatPanel(new Rectangle(366, 10, 126, 44), $"WAVE {_wave}", Gold);
        DrawStatPanel(new Rectangle(500, 10, 160, 44), $"TIME {(int)_elapsedSurvivalTime}s", SoftWhite);

        Rectangle healthPanel = new(668, 10, 344, 44);
        DrawPanel(healthPanel, PanelMid, Crimson);
        Rectangle healthBackground = new(748, 20, 250, 24);
        const int barWidth = 250;
        int healthWidth = (int)(barWidth * Math.Clamp(_displayedHealth / Player.MaximumHealth, 0f, 1f));
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

        Rectangle controlsPanel = new(188, 574, 648, 24);
        DrawRectangle(controlsPanel, PanelDark);
        DrawBorder(controlsPanel, 2, PanelMid);
        DrawCentredText("MOVE: WASD / ARROWS   FIRE: SPACE   QUIT: ESC", controlsPanel, new Color(151, 181, 205));
    }

    private void DrawStartScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Ink * (0.76f * alpha));
        Rectangle menuPanel = new(178, 104, 668, 398);
        DrawPanel(menuPanel, PanelDark * alpha, Cyan * alpha);
        DrawRectangle(new Rectangle(190, 116, 644, 8), Gold * alpha);
        DrawRectangle(new Rectangle(190, 482, 644, 8), Blue * alpha);

        DrawCentredText("A R E N A M A X E R", new Rectangle(0, 150, ScreenWidth, 58), Cyan * alpha);
        DrawCentredText("SURVIVE  /  SCORE  /  MAX OUT", new Rectangle(0, 210, ScreenWidth, 36), Gold * alpha);
        DrawCentredText("RED  RUSHER    PURPLE  TANK    GREEN  MEDKIT",
            new Rectangle(0, 270, ScreenWidth, 34), new Color(184, 204, 221) * alpha);
        DrawButton("PLAY", alpha);
        DrawCentredText("CLICK PLAY OR PRESS ENTER", new Rectangle(0, 427, ScreenWidth, 32),
            new Color(151, 181, 205) * alpha);
    }

    private void DrawGameOverScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.78f * alpha));
        Rectangle gameOverPanel = new(208, 112, 608, 382);
        DrawPanel(gameOverPanel, PanelDark * alpha, Crimson * alpha);
        DrawRectangle(new Rectangle(220, 124, 584, 8), Crimson * alpha);
        DrawCentredText("SYSTEM DOWN", new Rectangle(0, 165, ScreenWidth, 54), Crimson * alpha);
        DrawCentredText($"SCORE {_scoreManager.Score}     TIME {(int)_elapsedSurvivalTime}s",
            new Rectangle(0, 235, ScreenWidth, 38), SoftWhite * alpha);
        DrawCentredText($"HIGH SCORE {_highScore}", new Rectangle(0, 280, ScreenWidth, 36), Gold * alpha);
        DrawButton("PLAY AGAIN", alpha);
    }

    private void DrawButton(string text, float alpha)
    {
        MouseState mouse = Mouse.GetState();
        bool hovered = PlayButton.Contains(mouse.Position);
        Color buttonColour = hovered ? Cyan : Blue;
        DrawRectangle(new Rectangle(PlayButton.X + 7, PlayButton.Y + 7, PlayButton.Width, PlayButton.Height),
            Shadow * alpha);
        DrawRectangle(PlayButton, Gold * alpha);
        Rectangle buttonInner = PlayButton;
        buttonInner.Inflate(-4, -4);
        DrawRectangle(buttonInner, buttonColour * alpha);
        DrawBorder(buttonInner, 3, PanelDark * alpha);
        DrawCentredText($"> {text} <", buttonInner, Color.White * alpha);
    }

    private void DrawStatPanel(Rectangle area, string text, Color accent)
    {
        DrawPanel(area, PanelMid, accent);
        DrawCentredText(text, area, SoftWhite);
    }

    private void DrawPanel(Rectangle area, Color fill, Color accent)
    {
        DrawRectangle(new Rectangle(area.X + 4, area.Y + 4, area.Width, area.Height), Shadow);
        DrawRectangle(area, accent);
        Rectangle inner = area;
        inner.Inflate(-3, -3);
        DrawRectangle(inner, fill);
        DrawBorder(inner, 2, PanelMid);
    }

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

    private void DrawCentredText(string text, Rectangle area, Color colour)
    {
        Vector2 size = _font.MeasureString(text);
        Vector2 position = new(
            area.X + (area.Width - size.X) / 2f,
            area.Y + (area.Height - size.Y) / 2f);
        _spriteBatch.DrawString(_font, text, position, colour);
    }

    private void DrawRectangle(Rectangle rectangle, Color colour) =>
        _spriteBatch.Draw(_pixel, rectangle, colour);

    private void DrawBorder(Rectangle rectangle, int thickness, Color colour)
    {
        DrawRectangle(new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), colour);
        DrawRectangle(new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), colour);
        DrawRectangle(new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), colour);
        DrawRectangle(new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), colour);
    }

    private bool IsNewKeyPress(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

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

    private static string GetHighScorePath()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArenaMaxer");
        return Path.Combine(directory, "highscore.txt");
    }
}
