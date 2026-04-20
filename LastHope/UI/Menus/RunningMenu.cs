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

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        DrawWorld(gameTime, spriteBatch, transformMatrix);
    }
}
