using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Victory overlay with restart/quit. Routed via <see cref="Last_Hope.UI.Menu.UpdateWinnerMenu"/> /
/// <see cref="Last_Hope.UI.Menu.DrawWinnerMenu"/>.
/// </summary>
public class WinnerMenu : MenuBase
{
    /// <summary>Handles restart/quit using shared <see cref="MenuBase.HandleEndGameMenuClicks"/>.</summary>
    public void Update(GameTime gameTime)
    {
        const string title = "Winner";
        EndGameMenuLayout layout = LayoutEndGameTwoButtonMenu(title);
        HandleEndGameMenuClicks(layout,
            onRestart: () =>
            {
                gm.ResetGame();
                _state = GameState.Running;
            },
            onMainMenu: () =>
            {
                gm.ResetGame();
                _state = GameState.MainMenu;
            });
    }

    /// <summary>Draws the running world then winner text and end-game buttons.</summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        const string title = "Winner";
        EndGameMenuLayout layout = LayoutEndGameTwoButtonMenu(title);

        DrawWorld(gameTime, spriteBatch, transformMatrix);

        spriteBatch.Begin();
        DrawEndGameTwoButtonOverlay(spriteBatch, title, Color.LimeGreen, Color.Red, layout);
        spriteBatch.End();
    }
}
