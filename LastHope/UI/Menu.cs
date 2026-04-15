using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.UI.Menus;

namespace Last_Hope.UI;

public class Menu
{
    public GameState PreviousState => _previousState;
    private GameState _previousState = GameState.StartMenu;

    private readonly StartMenu _startMenu = new();
    private readonly ControlsMenu _controlsMenu;
    private readonly RunningMenu _runningMenu = new();
    private readonly PausedMenu _pausedMenu = new();
    private readonly WinnerMenu _winnerMenu = new();
    private readonly GameOverMenu _gameOverMenu = new();

    public Menu()
    {
        _controlsMenu = new ControlsMenu(this);
    }

    public void UpdateStartMenu(GameTime gameTime) => _startMenu.Update(gameTime);
    public void DrawStartMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _startMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateControlsMenu(GameTime gameTime) => _controlsMenu.Update(gameTime);
    public void DrawControlsMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _controlsMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateRunningMenu(GameTime gameTime) => _runningMenu.Update(gameTime);
    public void DrawRunningMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _runningMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdatePausedMenu(GameTime gameTime) => _pausedMenu.Update(gameTime);
    public void DrawPausedMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _pausedMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateWinnerMenu(GameTime gameTime) => _winnerMenu.Update(gameTime);
    public void DrawWinnerMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _winnerMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateGameOverMenu(GameTime gameTime) => _gameOverMenu.Update(gameTime);
    public void DrawGameOverMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _gameOverMenu.Draw(gameTime, spriteBatch, transformMatrix);
}
