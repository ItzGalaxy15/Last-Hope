using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class WaveIndicator : UIElement
{
    private Vector2 _position;
    private const float TextScale = 0.4f;
    private Texture2D _pixel;

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        var gm = GameManager.GetGameManager();
        if (gm._font != null && gm.EnemySpawner != null)
        {
            int displayWave = System.Math.Min(gm.EnemySpawner.CurrentWave, gm.EnemySpawner.TotalWaves);
            string text = $"Wave {displayWave}/{gm.EnemySpawner.TotalWaves}";
            Vector2 size = gm._font.MeasureString(text) * TextScale;

            // Top right corner with some padding
            float x = viewport.Width - size.X - 15;
            float y = 60;
            _position = new Vector2(x, y);
        }
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

        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        Vector2 size = gm._font.MeasureString(waveText) * TextScale;
        int padding = 5;
        Rectangle bgRect = new Rectangle(
            (int)_position.X - padding,
            (int)_position.Y - padding,
            (int)size.X + padding * 2,
            (int)size.Y + padding * 2
        );
        Color bgRectColor = new (0, 0, 0, 170);
        Color textColor = new (255, 245, 210, 255);

        spriteBatch.Draw(_pixel, bgRect, bgRectColor);
        spriteBatch.DrawString(gm._font, waveText, _position, textColor, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
    }
}
