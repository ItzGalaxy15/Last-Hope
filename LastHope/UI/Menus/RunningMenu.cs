using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.UI.Menus;

public class RunningMenu : MenuBase
{
    /// <summary>
    /// Updates the main game loop logic, handling input, object updates, collisions, and state changes.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public void Update(GameTime gameTime)
    {

        if (InputManager.IsKeyPress(Keys.Escape))
        {
            _state = GameState.Paused;
            return;
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

    /// <summary>
    /// Draws the active game world to the screen.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    /// <param name="spriteBatch">The SpriteBatch used for rendering.</param>
    /// <param name="transformMatrix">Optional transformation matrix.</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        DrawWorld(gameTime, spriteBatch, transformMatrix);
    }
}
