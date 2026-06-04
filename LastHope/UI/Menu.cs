using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;
using Last_Hope.UI.Menus;
using Last_Hope.SkillTree;

namespace Last_Hope.UI;

/// <summary>
/// Facade for all full-screen menu states and the in-run skill tree overlay. Each method pair is invoked from
/// <see cref="GameManager"/> based on <see cref="GameState"/> (e.g. <see cref="GameState.MainMenu"/> →
/// <see cref="UpdateMainMenu"/> / <see cref="DrawMainMenu"/>). Gum-backed hubs call <see cref="ReleaseMainMenuGum"/> /
/// <see cref="ReleasePausedMenuGum"/> when leaving those states.
/// </summary>
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

    private void EnsureStatScreen()
    {
        if (_statScreen == null)
            _statScreen = new StatScreenOverlay(GameManager.GetGameManager().Pixel);
    }

    public void UpdateMainMenu(GameTime gameTime) => _mainMenu.Update(gameTime);

    public void DrawMainMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null) =>
        _mainMenu.Draw(gameTime, spriteBatch, transformMatrix);

    /// <summary>Removes Gum controls when leaving the title hub so they do not stay interactive.</summary>
    public void ReleaseMainMenuGum() => _mainMenu.ReleaseGumUi();

    /// <summary>Removes pause Gum buttons when resuming or changing state away from <see cref="GameState.Paused"/>.</summary>
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

    /// <summary>Running gameplay update, or skill tree UI when the overlay is open (toggle: N).</summary>
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
                        _showSkillTree = false; // no overlay for other classes
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

    public void ForceOpenSkillTree()
    {
        if (!_showSkillTree)
        {
            GameManager gm = GameManager.GetGameManager();
            Viewport vp = gm.Game != null
                ? gm.Game.GraphicsDevice.Viewport
                : new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

            switch (gm._player)
            {
                case Warrior:
                    _skillTreeCanvas = SkillTreeOverlayFactory.CreateWarriorOverlay(gm, vp);
                    _showSkillTree = true;
                    break;
                case Archer:
                    _skillTreeCanvas = SkillTreeOverlayFactory.CreateArcherOverlay(gm, vp);
                    _showSkillTree = true;
                    break;
            }
        }
    }

    public void AwardTalentPoint()
    {
        if (_skillTreeCanvas != null)
        {
            _skillTreeCanvas.AddTalentPoint();
        }
        else
        {
            // Canvas not yet opened this run, write directly to the save file so the
            // point is there when the player opens the skill tree later.
            GameManager gm = GameManager.GetGameManager();
            string classId = gm._player switch
            {
                Warrior => "Warrior",
                Archer  => "Archer",
                _       => null
            };
            if (classId == null) return;
            SkillTreeState state = SkillTreeSaveManager.Load(classId);
            state.UnspentSkillPoints++;
            SkillTreeSaveManager.Save(state);
        }

        GameManager.GetGameManager().RequestToast("Talent Point Earned!");
    }

    public void ResetSkillTree()
    {
        _skillTreeCanvas = null;
        _showSkillTree = false;
    }

    /// <summary>World draw plus optional full-screen skill tree overlay on top.</summary>
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
