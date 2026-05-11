using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Game over overlay: dimmed world + restart/quit. Driven from <see cref="Last_Hope.UI.Menu.UpdateGameOverMenu"/> /
/// <see cref="Last_Hope.UI.Menu.DrawGameOverMenu"/> when <see cref="GameState"/> is game over.
/// </summary>
public class GameOverMenu : MenuBase
{
    private float _fadeAmount;
    private const float FadeSpeed = 0.5f;

    /// <summary>Advances fade and handles restart/quit clicks (layout from <see cref="MenuBase.LayoutEndGameTwoButtonMenu"/>).</summary>
    public void Update(GameTime gameTime)
    {
        _fadeAmount += (float)gameTime.ElapsedGameTime.TotalSeconds * FadeSpeed;
        if (_fadeAmount > 0.8f)
            _fadeAmount = 0.8f;

        const string title = "Game Over";
        EndGameMenuLayout layout = LayoutEndGameTwoButtonMenu(title);
        HandleEndGameMenuClicks(layout,
            onRestart: () =>
            {
                _fadeAmount = 0f;
                gm.ResetGame();
                _state = GameState.Running;
            },
            onQuit: () => Game.Exit());
    }

    /// <summary>Applies death fade shader, draws world, then title and <see cref="MenuBase.EndGameMenuLabels"/> buttons.</summary>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        const string title = "Game Over";
        EndGameMenuLayout layout = LayoutEndGameTwoButtonMenu(title);

        if (gm.DeathFade != null)
            gm.DeathFade.Parameters["FadeAmount"]?.SetValue(_fadeAmount);

        DrawWorld(gameTime, spriteBatch, transformMatrix, gm.DeathFade);

        spriteBatch.Begin();
        DrawEndGameTwoButtonOverlay(spriteBatch, title, Color.Red, Color.Red, layout);
        spriteBatch.End();
    }
}
