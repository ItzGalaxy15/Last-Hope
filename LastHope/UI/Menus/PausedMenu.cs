using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.UI.Menus;

public class PausedMenu : MenuBase
{
    public void Update(GameTime gameTime)
    {
        string backText = "Back to Menu";
        string restartText = "Restart Game";
        string quitText = "Quit Game";

        Vector2 restartPos = GetFontPosition(restartText);
        Vector2 backPos = restartPos + new Vector2(0, -100);
        Vector2 quitPos = restartPos + new Vector2(0, 100);

        Rectangle backRect = GetTextRectangleForFont(_font, backText, backPos);
        Rectangle restartRect = GetTextRectangleForFont(_font, restartText, restartPos);
        Rectangle quitRect = GetTextRectangleForFont(_font, quitText, quitPos);

        if (InputManager.IsKeyPress(Keys.Escape))
        {
            _state = GameState.Running;
        }

        Point mouse = InputManager.CurrentMouseState.Position;
        if (InputManager.LeftMousePress())
        {
            if (backRect.Contains(mouse))
            {
                _state = GameState.MainMenu;
                return;
            }

            if (restartRect.Contains(mouse))
            {
                gm.ResetGame();
                _state = GameState.Running;
                return;
            }

            if (quitRect.Contains(mouse))
            {
                Game.Exit();
            }
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string backText = "Back to Menu";
        string restartText = "Restart Game";
        string quitText = "Quit Game";

        Vector2 restartPos = GetFontPosition(restartText);
        Vector2 backPos = restartPos + new Vector2(0, -100);
        Vector2 quitPos = restartPos + new Vector2(0, 100);

        Rectangle backRect = GetTextRectangleForFont(_font, backText, backPos);
        Rectangle restartRect = GetTextRectangleForFont(_font, restartText, restartPos);
        Rectangle quitRect = GetTextRectangleForFont(_font, quitText, quitPos);

        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        float ctrlScale = 0.55f * ui;
        DrawSavedControlsText(spriteBatch, gameTime, new Vector2(48f * ui, 220f * ui), ctrlScale);
        SpriteFont mf = MenuUiFont;
        float itemScale = 0.55f * ui;
        float inner = MeasureItemsContentWidth(mf, itemScale);
        DrawItemsText(spriteBatch, gameTime, new Vector2(vp.Width - inner - 56f * ui, 220f * ui), inner, itemScale);
        spriteBatch.Draw(Pixel, backRect, new Color(30, 50, 70));
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, backText, backPos, Color.LightSkyBlue);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }
}
