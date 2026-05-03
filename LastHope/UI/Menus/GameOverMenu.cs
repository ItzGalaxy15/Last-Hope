using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

public class GameOverMenu : MenuBase
{
    private float _fadeAmount = 0f;
    private const float FadeSpeed = 0.5f;

    public void Update(GameTime gameTime)
    {
        _fadeAmount += (float)gameTime.ElapsedGameTime.TotalSeconds * FadeSpeed;
        if (_fadeAmount > 0.8f) // Clamp to 0.8 so it doesn't go fully black
            _fadeAmount = 0.8f;

        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (restartRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            _fadeAmount = 0f;
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
        string gameOverText = "Game Over";
        Vector2 positrionGameOver = GetFontPosition(gameOverText);

        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (gm.DeathFade != null)
        {
            gm.DeathFade.Parameters["FadeAmount"]?.SetValue(_fadeAmount);
        }

        DrawWorld(gameTime, spriteBatch, transformMatrix, gm.DeathFade);

        spriteBatch.Begin();
        gm.DrawUiString(spriteBatch, _font, gameOverText, positrionGameOver, Color.Red);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        gm.DrawUiString(spriteBatch, _font, restartText, restartPos, Color.White);
        gm.DrawUiString(spriteBatch, _font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }
}
