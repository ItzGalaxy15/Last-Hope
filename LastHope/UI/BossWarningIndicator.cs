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

    public BossWarningIndicator()
    {
    }

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        var gm = GameManager.GetGameManager();
        if (gm.EnemySpawner == null) return;

        if (gm.EnemySpawner.BossSpawned && !_hasStarted)
        {
            _hasStarted = true;
            _timer = DisplayDuration;
        }

        if (_timer > 0)
        {
            _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_timer <= 0) return;

        var gm = GameManager.GetGameManager();
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
        float pulse = (float)Math.Sin(_timer * Math.PI * 2) * 0.1f * baseScale;
        float scale = baseScale + pulse;

        // Draw an outline/shadow for readability
        spriteBatch.DrawString( gm._font, text, position + new Vector2(2, 2) * scale, Color.Black, 0f, origin, scale, SpriteEffects.None, 0f );

        // Draw text
        spriteBatch.DrawString(gm._font, text, position, Color.Red, 0f, origin, scale, SpriteEffects.None, 0f);
    }
}
