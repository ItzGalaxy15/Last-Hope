using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.UI.Menus;

public class PausedMenu : MenuBase
{
    public void Update(GameTime gameTime)
    {

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (InputManager.IsKeyPress(Keys.Escape))
        {
            _state = GameState.Running;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin();
        DrawControlsText(spriteBatch, gameTime);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }
}
