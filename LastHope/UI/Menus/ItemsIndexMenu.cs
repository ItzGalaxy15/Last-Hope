using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

public sealed class ItemsIndexMenu : MenuBase
{
    public void Update(GameTime gameTime)
    {
        if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.Q))
            _state = GameState.MainMenu;
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        SpriteFont font = MenuUiFont;
        if (font == null)
            return;

        if (transformMatrix != null)
            DrawWorld(gameTime, spriteBatch, transformMatrix);

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        float textScale = 0.62f * ui;
        float wrap = MathHelper.Clamp(MeasureItemsContentWidth(font, textScale) + 120f * ui, 480f * ui, vp.Width - 80f);

        float panelTop = 110f * ui;
        float titleScale = 1.05f * ui;
        Vector2 titleSize = font.MeasureString("ITEMS INDEX") * titleScale;
        Vector2 titlePos = new Vector2(vp.Width / 2f - titleSize.X * 0.5f, 40f * ui);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        spriteBatch.DrawString(font, "ITEMS INDEX", titlePos, new Color(255, 220, 160), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        float panelLeft = vp.Width / 2f - (wrap + 20f) / 2f;
        DrawItemsText(spriteBatch, gameTime, new Vector2(panelLeft + 10f * ui, panelTop), wrap, textScale);

        const string back = "Esc / Q - Back";
        float backScale = 0.55f * ui;
        spriteBatch.DrawString(font, back, new Vector2(40 * ui, vp.Height - 56f * ui), Color.Gray, 0f, Vector2.Zero, backScale, SpriteEffects.None, 0f);
        spriteBatch.End();
    }
}
