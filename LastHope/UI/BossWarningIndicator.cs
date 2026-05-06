using System;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class BossWarningIndicator : UIElement
{
    private bool _hasStarted = false;
    private float _timer = 0f;
    private const float DisplayDuration = 3f;

    private Texture2D _bossWarningTexture;
    private int _currentFrame = 0;
    private float _frameTimer = 0f;
    private const int TotalFrames = 20;
    private const float TimePerFrame = 0.05f;

    /// <summary>
    /// Initializes a new instance of the BossWarningIndicator.
    /// </summary>
    public BossWarningIndicator()
    {
    }

    /// <summary>
    /// Loads the graphical content needed for the boss warning indicator.
    /// </summary>
    /// <param name="content">The ContentManager to load from.</param>
    public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
    {
        _bossWarningTexture = content.Load<Texture2D>("BossWarning");
    }

    /// <summary>
    /// Updates the logic and timing for displaying the boss warning.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="viewport">The current game viewport.</param>
    public override void Update(GameTime gameTime, Viewport viewport)
    {
        var gm = GameManager.GetGameManager();
        if (gm.EnemySpawner == null) return;

        if (!gm.EnemySpawner.BossSpawned)
        {
            _hasStarted = false;
        }
        else if (gm.EnemySpawner.BossSpawned && !_hasStarted)
        {
            _hasStarted = true;
            _timer = DisplayDuration;
        }

        if (_timer > 0)
        {
            _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_frameTimer >= TimePerFrame)
            {
                _frameTimer = 0f;
                _currentFrame = (_currentFrame + 1) % TotalFrames;
            }
        }
    }

    /// <summary>
    /// Draws the boss warning text or sprite to the screen.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="spriteBatch">The SpriteBatch used to draw the texture.</param>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_timer <= 0) return;

        var gm = GameManager.GetGameManager();
        if (_bossWarningTexture == null)
        {
            if (gm._font == null) return;
            string text = "! BOSS INCOMING !";
            Vector2 textSize = gm._font.MeasureString(text);

            Vector2 position = new Vector2(
                gm.Game.GraphicsDevice.Viewport.Width / 2f,
                gm.Game.GraphicsDevice.Viewport.Height / 2f
            );

            Vector2 origin = new Vector2(
                textSize.X / 2f,
                textSize.Y / 2f
            );

            // Calculate a scale so the text spans roughly 25% of the screen width
            float targetWidth = gm.Game.GraphicsDevice.Viewport.Width * 0.25f;
            float baseScale = targetWidth / textSize.X;

            // Calculate pulsating scale
            // This way of making the pulsing is derived from line 135 of GameManager.cs of this project
            // https://github.com/Samonum/GameDevSamples/blob/main/Samples/7.%20Shaders/SpaceDefence/Engine/GameManager.cs
            float pulse = (float)Math.Sin(_timer * Math.PI * 2) * 0.1f * baseScale;
            float scale = baseScale + pulse;

            // Draw an outline/shadow for readability
            spriteBatch.DrawString( gm._font, text, position + new Vector2(2, 2) * scale, Color.Black, 0f, origin, scale, SpriteEffects.None, 0f );

            // Draw text
            spriteBatch.DrawString(gm._font, text, position, Color.Red, 0f, origin, scale, SpriteEffects.None, 0f);
        }
        else
        {
            Vector2 position = new Vector2(
                gm.Game.GraphicsDevice.Viewport.Width / 2f,
                gm.Game.GraphicsDevice.Viewport.Height / 2f
            );

            int frameWidth = _bossWarningTexture.Width / TotalFrames;
            int frameHeight = _bossWarningTexture.Height;

            Rectangle sourceRect = new Rectangle(_currentFrame * frameWidth, 0, frameWidth, frameHeight);

            Vector2 origin = new Vector2(frameWidth / 2f, frameHeight / 2f);

            // Calculate a scale so the sprite spans roughly 25% of the screen width
            float targetWidth = gm.Game.GraphicsDevice.Viewport.Width * 0.25f;
            float baseScale = targetWidth / frameWidth;

            float pulse = (float)Math.Sin(_timer * Math.PI * 2) * 0.1f * baseScale;
            float scale = baseScale + pulse;

            spriteBatch.Draw(_bossWarningTexture, position, sourceRect, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}
