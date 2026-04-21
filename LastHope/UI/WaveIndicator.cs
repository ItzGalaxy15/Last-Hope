using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class WaveIndicator : UIElement
{
    private Vector2 _position;
    private const float TextScale = 0.4f;

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
            float y = 50;
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
        spriteBatch.DrawString(gm._font, waveText, _position, Color.White, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
    }
}
