using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Browse playable characters (roster only — no run selection here).
/// </summary>
public class CharactersRosterMenu : MenuBase
{
    public void Update(GameTime gameTime)
    {
        if (_font == null)
            return;

        if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.Q))
        {
            _state = GameState.StartMenu;
            return;
        }

        Viewport vp = Game.GraphicsDevice.Viewport;
        Rectangle backRect = GetBackButtonRect(vp);
        if (backRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
            _state = GameState.StartMenu;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_font == null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        spriteBatch.Draw(Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(12, 18, 32, 255));

        const float titleScale = 0.75f;
        string title = "CHARACTERS";
        Vector2 titleSize = _font.MeasureString(title) * titleScale;
        spriteBatch.DrawString(
            _font,
            title,
            new Vector2(vp.Width / 2f - titleSize.X / 2f, 40f),
            Color.White,
            0f,
            Vector2.Zero,
            titleScale,
            SpriteEffects.None,
            0f);

        const float lineScale = 0.45f;
        float y = 130f;
        int index = 1;
        foreach (PlayableCharacterRegistry.Definition def in PlayableCharacterRegistry.Ordered)
        {
            string header = $"{index}. {def.DisplayName}";
            spriteBatch.DrawString(_font, header, new Vector2(80f, y), new Color(200, 220, 255), 0f, Vector2.Zero, lineScale, SpriteEffects.None, 0f);
            y += _font.LineSpacing * lineScale + 4f;
            spriteBatch.DrawString(_font, def.Tagline, new Vector2(100f, y), new Color(180, 190, 210), 0f, Vector2.Zero, lineScale * 0.92f, SpriteEffects.None, 0f);
            y += _font.LineSpacing * lineScale * 1.2f + 28f;
            index++;
        }

        Rectangle backRect = GetBackButtonRect(vp);
        spriteBatch.Draw(Pixel, backRect, new Color(40, 55, 75, 255));
        const float backScale = 0.5f;
        string back = "BACK";
        Vector2 bs = _font.MeasureString(back) * backScale;
        spriteBatch.DrawString(
            _font,
            back,
            new Vector2(backRect.Center.X - bs.X / 2f, backRect.Center.Y - bs.Y / 2f),
            Color.White,
            0f,
            Vector2.Zero,
            backScale,
            SpriteEffects.None,
            0f);

        const float hintScale = 0.38f;
        string hint = "Back: Esc / Q  or  click BACK";
        Vector2 hintSize = _font.MeasureString(hint) * hintScale;
        spriteBatch.DrawString(
            _font,
            hint,
            new Vector2(vp.Width - hintSize.X - 24, vp.Height - hintSize.Y - 18),
            new Color(200, 210, 230, 255),
            0f,
            Vector2.Zero,
            hintScale,
            SpriteEffects.None,
            0f);

        spriteBatch.End();
    }

    private static Rectangle GetBackButtonRect(Viewport vp)
    {
        return new Rectangle(vp.Width / 2 - 90, vp.Height - 100, 180, 44);
    }
}
