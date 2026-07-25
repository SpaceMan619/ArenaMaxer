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
    private static readonly Rectangle ArenaBounds = new(18, 72, ScreenWidth - 36, ScreenHeight - 90);
    private static readonly Rectangle PlayButton = new(ScreenWidth / 2 - 110, 355, 220, 58);

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
        _spawnTimer = 1f;
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
        GraphicsDevice.Clear(new Color(10, 15, 28));
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
        DrawRectangle(ArenaBounds, new Color(22, 32, 52));
        DrawBorder(ArenaBounds, 3, new Color(49, 190, 190));

        for (int x = ArenaBounds.Left + 40; x < ArenaBounds.Right; x += 80)
            DrawRectangle(new Rectangle(x, ArenaBounds.Top, 1, ArenaBounds.Height), new Color(35, 48, 70));
        for (int y = ArenaBounds.Top + 40; y < ArenaBounds.Bottom; y += 80)
            DrawRectangle(new Rectangle(ArenaBounds.Left, y, ArenaBounds.Width, 1), new Color(35, 48, 70));

        if (_dangerTint > 0.01f)
            DrawRectangle(ArenaBounds, new Color(120, 18, 28) * (_dangerTint * 0.16f));
    }

    private void DrawEntities()
    {
        foreach (PowerUp powerUp in _powerUps)
        {
            DrawRectangle(powerUp.Bounds, new Color(50, 220, 110));
            Rectangle vertical = new(powerUp.Bounds.Center.X - 3, powerUp.Bounds.Top + 5, 6, powerUp.Size - 10);
            Rectangle horizontal = new(powerUp.Bounds.Left + 5, powerUp.Bounds.Center.Y - 3, powerUp.Size - 10, 6);
            DrawRectangle(vertical, Color.White);
            DrawRectangle(horizontal, Color.White);
        }

        foreach (Projectile projectile in _projectiles)
            DrawRectangle(projectile.Bounds, new Color(255, 220, 65));

        foreach (Enemy enemy in _enemies)
        {
            Color colour = enemy is TankEnemy ? new Color(155, 80, 210) : new Color(235, 65, 70);
            if (enemy.HitFlash > 0f)
                colour = Color.White;
            DrawRectangle(enemy.Bounds, colour);
            DrawEnemyHealth(enemy);
        }

        DrawRectangle(_player.Bounds, new Color(40, 160, 240));
        DrawPlayerFacingIndicator();
    }

    private void DrawPlayerFacingIndicator()
    {
        Vector2 facing = _player.FacingDirection;
        Vector2 centre = _player.Position + facing * (_player.Size / 2f + 7f);
        Rectangle indicator = Math.Abs(facing.X) > Math.Abs(facing.Y)
            ? new Rectangle((int)centre.X - 9, (int)centre.Y - 3, 18, 6)
            : new Rectangle((int)centre.X - 3, (int)centre.Y - 9, 6, 18);
        DrawRectangle(indicator, new Color(160, 235, 255));
    }

    private void DrawEnemyHealth(Enemy enemy)
    {
        int width = enemy.Bounds.Width;
        Rectangle background = new(enemy.Bounds.Left, enemy.Bounds.Top - 7, width, 4);
        float percentage = enemy.Health / (float)enemy.MaximumHealth;
        Rectangle foreground = new(background.X, background.Y, (int)(background.Width * percentage), background.Height);
        DrawRectangle(background, new Color(60, 20, 25));
        DrawRectangle(foreground, new Color(90, 240, 120));
    }

    private void DrawHud()
    {
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, 60), new Color(11, 19, 34));
        _spriteBatch.DrawString(_font, $"SCORE  {_scoreManager.Score}", new Vector2(22, 18), Color.White);
        _spriteBatch.DrawString(_font, $"HIGH  {_highScore}", new Vector2(190, 18), new Color(175, 195, 220));
        _spriteBatch.DrawString(_font, $"WAVE  {_wave}", new Vector2(365, 18), new Color(255, 211, 75));
        _spriteBatch.DrawString(_font, $"TIME  {(int)_elapsedSurvivalTime}s", new Vector2(500, 18), Color.White);

        const int barWidth = 220;
        Rectangle healthBackground = new(775, 18, barWidth, 22);
        int healthWidth = (int)(barWidth * Math.Clamp(_displayedHealth / Player.MaximumHealth, 0f, 1f));
        DrawRectangle(healthBackground, new Color(65, 24, 31));
        DrawRectangle(new Rectangle(healthBackground.X, healthBackground.Y, healthWidth, healthBackground.Height),
            _player.Health > 30 ? new Color(45, 210, 105) : new Color(245, 65, 65));
        DrawBorder(healthBackground, 2, Color.White);
        DrawCentredText($"HP {_player.Health}/{Player.MaximumHealth}", healthBackground, Color.White);

        if (_statusTimer > 0f)
            DrawCentredText(_statusMessage, new Rectangle(0, 80, ScreenWidth, 45), Color.Gold);

        _spriteBatch.DrawString(_font, "MOVE: WASD / ARROWS     SHOOT: SPACE     ESC: QUIT",
            new Vector2(22, ScreenHeight - 22), new Color(145, 165, 190));
    }

    private void DrawStartScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(6, 10, 20) * (0.78f * alpha));
        DrawCentredText("ARENAMAXER", new Rectangle(0, 145, ScreenWidth, 60), new Color(65, 220, 225) * alpha);
        DrawCentredText("Survive the arena. Defeat the swarm.", new Rectangle(0, 220, ScreenWidth, 40), Color.White * alpha);
        DrawCentredText("Red: fast and weak    Purple: slow and strong    Green: health",
            new Rectangle(0, 270, ScreenWidth, 40), new Color(190, 205, 225) * alpha);
        DrawButton("PLAY", alpha);
        DrawCentredText("Click Play or press Enter", new Rectangle(0, 425, ScreenWidth, 35),
            new Color(160, 180, 205) * alpha);
    }

    private void DrawGameOverScreen()
    {
        float alpha = Math.Clamp(_screenFade, 0f, 1f);
        DrawRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black * (0.72f * alpha));
        DrawCentredText("GAME OVER", new Rectangle(0, 165, ScreenWidth, 60), new Color(255, 75, 80) * alpha);
        DrawCentredText($"Final score: {_scoreManager.Score}    Survived: {(int)_elapsedSurvivalTime}s",
            new Rectangle(0, 240, ScreenWidth, 45), Color.White * alpha);
        DrawCentredText($"Highest score: {_highScore}", new Rectangle(0, 285, ScreenWidth, 40),
            new Color(255, 215, 85) * alpha);
        DrawButton("PLAY AGAIN", alpha);
    }

    private void DrawButton(string text, float alpha)
    {
        MouseState mouse = Mouse.GetState();
        bool hovered = PlayButton.Contains(mouse.Position);
        Color buttonColour = hovered ? new Color(45, 200, 205) : new Color(28, 135, 165);
        DrawRectangle(PlayButton, buttonColour * alpha);
        DrawBorder(PlayButton, 3, Color.White * alpha);
        DrawCentredText(text, PlayButton, Color.White * alpha);
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
