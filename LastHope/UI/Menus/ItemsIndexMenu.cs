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
        SpriteFont layoutFont = MenuUiFont ?? _font;
        if (layoutFont == null && gm.FontBitmap == null)
            return;

        if (transformMatrix != null)
            DrawWorld(gameTime, spriteBatch, transformMatrix);

        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);

        // Same shell as SettingsMenu: hub backdrop + rail, dim, framed panel, centered title/footer.
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        DrawHubMenuBackdrop(spriteBatch, Pixel, vp);
        DrawHubMenuLeftRail(spriteBatch, Pixel, vp, ui);
        spriteBatch.End();

        float panelW = MathHelper.Min(720f * ui, vp.Width - 40f);
        float panelH = vp.Height * 0.76f;
        int px = (int)(vp.Width / 2f - panelW / 2f);
        int py = (int)(vp.Height / 2f - panelH / 2f);
        var panel = new Rectangle(px, py, (int)panelW, (int)panelH);

        float titleTextScale = 0.72f * ui;
        Vector2 titleSize = gm.MeasureUiString(layoutFont, "ITEMS INDEX", titleTextScale);
        float titleY = panel.Y + 22f * ui;
        var titlePos = new Vector2(panel.X + panel.Width / 2f - titleSize.X / 2f, titleY);

        float contentTop = titleY + titleSize.Y + 28f * ui;
        var content = new Rectangle(
            panel.X + (int)(16f * ui),
            (int)contentTop,
            panel.Width - (int)(32f * ui),
            panel.Bottom - (int)contentTop - (int)(48f * ui));

        float textScale = 0.56f * ui;
        float innerPad = 10f * ui;
        float wrapInner = MathHelper.Max(content.Width - innerPad * 2f, 220f);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        spriteBatch.Draw(Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(0, 0, 0, 72));
        spriteBatch.Draw(Pixel, panel, new Color(18, 24, 38, 200));
        DrawPanelOutline(spriteBatch, panel, new Color(140, 185, 255, 110));

        gm.DrawUiString(spriteBatch, layoutFont, "ITEMS INDEX", titlePos, Color.White, 0f, Vector2.Zero, titleTextScale, SpriteEffects.None, 0f);

        DrawItemsText(spriteBatch, gameTime, new Vector2(content.X + innerPad, content.Y + 8f * ui), wrapInner, textScale);

        var (footFont, footMul) = BodyFontForMenuText(layoutFont);
        float footS = 0.48f * ui * footMul;
        const string foot = "Esc / Q - Back";
        Vector2 fs = gm.MeasureUiString(footFont, foot, footS);
        gm.DrawUiString(spriteBatch, footFont, foot,
            new Vector2(panel.Center.X - fs.X / 2f, panel.Bottom - 36f * ui),
            Color.Gray, 0f, Vector2.Zero, footS, SpriteEffects.None, 0f);

        spriteBatch.End();
    }
}
