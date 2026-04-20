using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.UI.Menus;

public class RunningMenu : MenuBase
{
    public void Update(GameTime gameTime)
    {
        float scale = 0.5f;
        Vector2 topLeft = new Vector2(20, 100);

        string quitText = "Quit Game";
        Vector2 quitPos = topLeft + new Vector2(10, 5);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos, scale);

        Vector2 quitSize = _font.MeasureString(quitText) * scale;
        string restartText = "Restart Game";
        Vector2 restartPos = quitPos + new Vector2(quitSize.X + 40, 0);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos, scale);

        if (InputManager.IsKeyPress(Keys.Escape))
        {
            _state = GameState.Paused;
            return;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
            return;
        }

        if (restartRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            gm.ResetGame();
            _state = GameState.Running;
        }

        gm.HandleInput(InputManager);

        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Update(gameTime);
        }

        gm.CheckCollision();

        foreach (GameObject gameObject in _toBeAdded)
        {
            gameObject.Load(_content);
            _gameObjects.Add(gameObject);
        }
        _toBeAdded.Clear();

        foreach (GameObject gameObject in _toBeRemoved)
        {
            gameObject.Destroy();
            _gameObjects.Remove(gameObject);
        }
        _toBeRemoved.Clear();
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        float scale = 0.5f;
        Vector2 topLeft = new Vector2(20, 100);

        string quitText = "Quit Game";
        Vector2 quitPos = topLeft + new Vector2(10, 5);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos, scale);

        Vector2 quitSize = _font.MeasureString(quitText) * scale;
        string restartText = "Restart Game";
        Vector2 restartPos = quitPos + new Vector2(quitSize.X + 40, 0);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos, scale);

        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin();
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.Green, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.End();
    }
}
