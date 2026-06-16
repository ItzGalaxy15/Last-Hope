using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;
using Last_Hope.UI.Menus;
using Last_Hope.SkillTree;

namespace Last_Hope.UI;

/// <summary>
/// Facade for all full-screen menu states and the in-run skill tree overlay. 
/// Each method pair is invoked from <see cref="GameManager"/> based on <see cref="GameState"/>.
/// </summary>
/// <remarks>
/// This class acts as a State Machine manager for the UI layer. 
/// For information on State design patterns in C#, see: 
/// <see href="https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/">Microsoft Framework Design Guidelines</see>.
/// </remarks>
public class Menu
{
    private readonly MainMenuScreen _mainMenu = new();
    private readonly CharactersRosterMenu _charactersRosterMenu = new();
    private readonly CharacterSelectMenu _characterSelectMenu = new();
    private readonly ItemsIndexMenu _itemsIndexMenu = new();
    private readonly SettingsMenu _settingsMenu = new();
    private readonly RunningMenu _runningMenu = new();
    private readonly PausedMenu _pausedMenu = new();
    private readonly WinnerMenu _winnerMenu = new();
    private readonly GameOverMenu _gameOverMenu = new();

    private SkillTreeMenuCanvas _skillTreeCanvas;
    private bool _showSkillTree;

    private StatScreenOverlay _statScreen;
    private bool _showStatScreen;

    /// <summary>
    /// Ensures the stat screen overlay is instantiated before use, following the Lazy Initialization pattern.
    /// </summary>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/framework/performance/lazy-initialization">Microsoft Lazy Initialization</see>
    /// </remarks>
    private void EnsureStatScreen()
    {
        if (_statScreen == null)
            _statScreen = new StatScreenOverlay(GameManager.GetGameManager().Pixel);
    }

    /// <summary>
    /// Updates the main menu logic based on elapsed game time.
    /// </summary>
    /// <param name="gameTime">Snapshot of timing values. <see href="https://docs.monogame.net/api/Microsoft.Xna.Framework.GameTime.html">MonoGame GameTime</see></param>
    public void UpdateMainMenu(GameTime gameTime) => _mainMenu.Update(gameTime);

    /// <summary>
    /// Draws the main menu to the active graphics device.
    /// </summary>
    /// <param name="gameTime">Snapshot of timing values.</param>
    /// <param name="spriteBatch">Used to draw 2D textures. <see href="https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html">MonoGame SpriteBatch</see></param>
    /// <param name="transformMatrix">Optional camera matrix.</param>
    public void DrawMainMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _mainMenu.Draw(gameTime, spriteBatch, transformMatrix);

    /// <summary>
    /// Removes Gum controls when leaving the title hub so they do not stay interactive and consume memory or input events.
    /// </summary>
    public void ReleaseMainMenuGum() => _mainMenu.ReleaseGumUi();

    /// <summary>
    /// Removes pause Gum buttons when resuming or changing state away from Paused status.
    /// </summary>
    public void ReleasePausedMenuGum() => _pausedMenu.ReleaseGumUi();

    public void UpdateCharactersRosterMenu(GameTime gameTime) => _charactersRosterMenu.Update(gameTime);

    public void DrawCharactersRosterMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _charactersRosterMenu.Draw(gameTime, spriteBatch);

    public void UpdateCharacterSelectMenu(GameTime gameTime) => _characterSelectMenu.Update(gameTime);

    public void DrawCharacterSelectMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _characterSelectMenu.Draw(gameTime, spriteBatch);

    public void UpdateItemsIndexMenu(GameTime gameTime) => _itemsIndexMenu.Update(gameTime);

    public void DrawItemsIndexMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _itemsIndexMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateSettingsMenu(GameTime gameTime) => _settingsMenu.Update(gameTime);

    public void DrawSettingsMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _settingsMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdatePausedMenu(GameTime gameTime) => _pausedMenu.Update(gameTime);

    public void DrawPausedMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _pausedMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateWinnerMenu(GameTime gameTime) => _winnerMenu.Update(gameTime);

    public void DrawWinnerMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _winnerMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateGameOverMenu(GameTime gameTime) => _gameOverMenu.Update(gameTime);

    public void DrawGameOverMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _gameOverMenu.Draw(gameTime, spriteBatch, transformMatrix);

    /// <summary>
    /// Core gameplay UI update loop. Handles toggling of the Skill Tree and Stat Screen overlays.
    /// </summary>
    public void UpdateRunningMenu(GameTime gameTime)
    {
        GameManager gm = GameManager.GetGameManager();
        InputManager input = gm.InputManager;

        if (input.IsGameplayKeyPress(KeybindId.StatScreen))
        {
            EnsureStatScreen();
            _showStatScreen = !_showStatScreen;
        }

        if (_showStatScreen && _statScreen != null)
            _statScreen.Update(gameTime);

        if (input.IsGameplayKeyPress(KeybindId.SkillTree))
        {
            _showSkillTree = !_showSkillTree;

            if (_showSkillTree && _skillTreeCanvas == null)
            {
                Viewport vp = gm.Game != null
                    ? gm.Game.GraphicsDevice.Viewport
                    : new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

                switch (gm._player)
                {
                    case Warrior:
                        _skillTreeCanvas = SkillTreeOverlayFactory.CreateWarriorOverlay(gm, vp);
                        break;
                    case Archer:
                        _skillTreeCanvas = SkillTreeOverlayFactory.CreateArcherOverlay(gm, vp);
                        break;
                    default:
                        _showSkillTree = false;
                        break;
                }
            }
        }

        if (_showSkillTree)
        {
            Viewport vp = gm.Game != null
                ? gm.Game.GraphicsDevice.Viewport
                : new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

            _skillTreeCanvas?.Update(gameTime, vp);
            if (_skillTreeCanvas?.IsClosed == true)
                _showSkillTree = false;
        }
        else
        {
            _runningMenu.Update(gameTime);
        }
    }

    /// <summary>
    /// Instantiates the skill tree backend silently to ensure event hooks function correctly during run continuation.
    /// </summary>
    public void LoadSkillTreeSilently()
    {
        if (_skillTreeCanvas == null)
        {
            GameManager gm = GameManager.GetGameManager();
            Viewport vp = gm.Game != null
                ? gm.Game.GraphicsDevice.Viewport
                : new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

            switch (gm._player)
            {
                case Warrior:
                    _skillTreeCanvas = SkillTreeOverlayFactory.CreateWarriorOverlay(gm, vp);
                    break;
                case Archer:
                    _skillTreeCanvas = SkillTreeOverlayFactory.CreateArcherOverlay(gm, vp);
                    break;
            }
        }
    }

    /// <summary>
    /// Forces the skill tree UI open, typically triggered by an external event like leveling up.
    /// </summary>
    public void ForceOpenSkillTree()
    {
        if (!_showSkillTree)
        {
            GameManager gm = GameManager.GetGameManager();
            Viewport vp = gm.Game != null
                ? gm.Game.GraphicsDevice.Viewport
                : new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

            if (_skillTreeCanvas == null)
            {
                switch (gm._player)
                {
                    case Warrior:
                        _skillTreeCanvas = SkillTreeOverlayFactory.CreateWarriorOverlay(gm, vp);
                        break;
                    case Archer:
                        _skillTreeCanvas = SkillTreeOverlayFactory.CreateArcherOverlay(gm, vp);
                        break;
                }
            }
            _showSkillTree = true;
        }
    }

    /// <summary>
    /// Awards a talent point by updating the central save state directly to prevent desynchronization, 
    /// then updates the UI if the Canvas is instantiated.
    /// </summary>
    /// <remarks>
    /// Centralized State Management prevents race conditions and data loss during Application Domain exits.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api">Microsoft Application State Management</see>
    /// </remarks>
    public void AwardTalentPoint()
    {
        // The canvas's tree shares the same SkillTreeState object as CurrentState,
        // so incrementing both would award two points. Add exactly one.
        if (_skillTreeCanvas != null)
        {
            _skillTreeCanvas.AddTalentPoint();
        }
        else if (SkillTreeSaveManager.CurrentState != null)
        {
            SkillTreeSaveManager.CurrentState.UnspentSkillPoints++;
        }

        SkillTreeSaveManager.SaveCurrent();

        GameManager.GetGameManager().RequestToast("Talent Point Earned!");
    }

    /// <summary>
    /// Clears UI references to allow the Garbage Collector to reclaim memory when the skill tree resets.
    /// </summary>
    public void ResetSkillTree()
    {
        _skillTreeCanvas = null;
        _showSkillTree = false;
    }

    /// <summary>
    /// Renders the running menu, drawing overlays sequentially on top of the world space.
    /// </summary>
    /// <param name="gameTime">Snapshot of timing values.</param>
    /// <param name="spriteBatch">Used to draw 2D textures.</param>
    /// <param name="transformMatrix">Optional camera matrix.</param>
    public void DrawRunningMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        _runningMenu.Draw(gameTime, spriteBatch, transformMatrix);

        if (_showSkillTree && _skillTreeCanvas != null)
        {
            GameManager gm = GameManager.GetGameManager();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            Viewport vp = spriteBatch.GraphicsDevice.Viewport;
            spriteBatch.Draw(gm.Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(10, 12, 15, 235));
            _skillTreeCanvas.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }

        if (_showStatScreen && _statScreen != null)
        {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            _statScreen.Draw(gameTime, spriteBatch);
            spriteBatch.End();
        }
    }
}