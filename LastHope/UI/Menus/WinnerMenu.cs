using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

public class WinnerMenu : MenuBase
{
    /// <summary>
    /// Updates the Winner menu logic, primarily checking user input for restarting or quitting.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public void Update(GameTime gameTime)
    {
        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

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

    /// <summary>
    /// Draws the Winner menu text and options to the screen.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="spriteBatch">The SpriteBatch used for rendering.</param>
    /// <param name="transformMatrix">Optional transformation matrix.</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string winnerText = "Winner";
        Vector2 positionWinner = GetFontPosition(winnerText);

        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin();
        gm.DrawUiString(spriteBatch, _font, winnerText, positionWinner, Color.LimeGreen);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        gm.DrawUiString(spriteBatch, _font, restartText, restartPos, Color.White);
        gm.DrawUiString(spriteBatch, _font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }
}
