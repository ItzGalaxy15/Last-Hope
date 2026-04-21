using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class WaveIndicator : UIElement
{
    private const float TextScale = 0.4f;
    private Texture2D _pixel;

    public override void Update(GameTime gameTime, Viewport viewport)
    {
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var gm = GameManager.GetGameManager();
        if (gm._font == null || gm.EnemySpawner == null) return;

        if (gm._state is GameState.MainMenu or GameState.ItemsIndex or GameState.SettingsMenu
            or GameState.Characters or GameState.CharacterSelect)
            return;

        int displayWave = System.Math.Min(gm.EnemySpawner.CurrentWave, gm.EnemySpawner.TotalWaves);
        string waveText = $"Wave {displayWave}/{gm.EnemySpawner.TotalWaves}";
        string enemiesText = $"Enemies left: {gm.EnemySpawner.GetEnemiesLeftCount()}";

        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        Vector2 waveSize = gm._font.MeasureString(waveText) * TextScale;
        Vector2 enemiesSize = gm._font.MeasureString(enemiesText) * TextScale;

        // Determine bounding box
        float maxTextWidth = System.Math.Max(waveSize.X, enemiesSize.X);
        float totalHeight = waveSize.Y + enemiesSize.Y + 2f;

        // Top right corner with padding from edge
        float rightEdgePadding = 15;
        float topEdgePadding = 60;

        float boxX = spriteBatch.GraphicsDevice.Viewport.Width - maxTextWidth - rightEdgePadding;
        float boxY = topEdgePadding;

        int padding = 5;
        Rectangle bgRect = new Rectangle(
            (int)boxX - padding,
            (int)boxY - padding,
            (int)maxTextWidth + padding * 2,
            (int)totalHeight + padding * 2
        );

        Color bgRectColor = new (0, 0, 0, 170);
        Color textColor = new (255, 245, 210, 255);

        spriteBatch.Draw(_pixel, bgRect, bgRectColor);

        // Left align the text within the box
        Vector2 wavePos = new (boxX, boxY);
        Vector2 enemiesPos = new (boxX, boxY + waveSize.Y + 2f);

        spriteBatch.DrawString(gm._font, waveText, wavePos, textColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(gm._font, enemiesText, enemiesPos, textColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
    }
}
