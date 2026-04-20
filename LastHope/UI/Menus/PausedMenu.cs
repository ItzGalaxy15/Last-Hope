using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.UI.Menus;

public class PausedMenu : MenuBase
{
    public void Update(GameTime gameTime)
    {

        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 100);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (InputManager.IsKeyPress(Keys.Escape))
        {
            _state = GameState.Running;
        }

        if (restartRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            gm.ResetGame();
            _state = GameState.Running;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 100);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin();
        DrawControlsText(spriteBatch, gameTime);
        DrawItemsText(spriteBatch, gameTime);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }
}
