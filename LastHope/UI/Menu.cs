using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.BaseModel;
using Last_Hope.SkillTree;
using System.IO;
using System.Text.Json;
using Last_Hope.UI.Menus;

namespace Last_Hope.UI;

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
    private bool _showSkillTree = false;

    public void UpdateMainMenu(GameTime gameTime) => _mainMenu.Update(gameTime);
    public void DrawMainMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _mainMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateCharactersRosterMenu(GameTime gameTime) => _charactersRosterMenu.Update(gameTime);
    public void DrawCharactersRosterMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _charactersRosterMenu.Draw(gameTime, spriteBatch);

    public void UpdateCharacterSelectMenu(GameTime gameTime) => _characterSelectMenu.Update(gameTime);
    public void DrawCharacterSelectMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _characterSelectMenu.Draw(gameTime, spriteBatch);

    public void UpdateItemsIndexMenu(GameTime gameTime) => _itemsIndexMenu.Update(gameTime);
    public void DrawItemsIndexMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _itemsIndexMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateSettingsMenu(GameTime gameTime) => _settingsMenu.Update(gameTime);
    public void DrawSettingsMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        => _settingsMenu.Draw(gameTime, spriteBatch, transformMatrix);

    public void UpdateRunningMenu(GameTime gameTime)
    {
        _runningMenu.Update(gameTime);

        var gm = GameManager.GetGameManager();
        var input = gm.InputManager;

        if (input.IsKeyPress(Keys.N))
        {
            _showSkillTree = !_showSkillTree;
            if (_showSkillTree && _skillTreeCanvas == null)
            {
                string jsonPath = "SkillTree/WarriorSkillTree.json";
                ClassSkillTreeData treeData = null;

                if (File.Exists(jsonPath))
                {
                    string rawJson = File.ReadAllText(jsonPath);
                    treeData = JsonSerializer.Deserialize<ClassSkillTreeData>(rawJson);
                }

                if (treeData == null)
                {
                    throw new System.Exception($"[SkillTree Error] Data is null! Could not find JSON at: {Path.GetFullPath(jsonPath)} \nEnsure the file's 'Copy to Output Directory' property is set to 'Copy if newer' in Visual Studio!");
                }
                if (treeData.Nodes == null || treeData.Nodes.Count == 0)
                {
                    throw new System.Exception("[SkillTree Error] JSON loaded, but Nodes list is empty! Check your JSON structure.");
                }

                System.Console.WriteLine($"[SkillTree UI] Successfully parsed {treeData.Nodes.Count} nodes from JSON.");

                SkillTreeState state = SkillTreeSaveManager.Load("Warrior");

                BaseSkillTree tree = new BaseSkillTree(treeData, state);

                if (gm._player is Warrior warrior)
                {
                    tree.OnEffectApplied += warrior.ApplyNodeEffect;
                    tree.OnTreeRespec += warrior.RevertAllSkillStats;
                }

                UIThemeData theme = new UIThemeData
                {
                    LockedDesaturation = new Color(50, 50, 50),
                    AccentGlowColor = new Color(230, 60, 70)
                };

                Viewport vp = default;
                if (gm.Game != null)
                    vp = gm.Game.GraphicsDevice.Viewport;
                else
                    vp = new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

                _skillTreeCanvas = new SkillTreeMenuCanvas(tree, theme, gm.Pixel, vp);
            }
        }

        if (_showSkillTree)
        {
            Viewport vp = default;
            if (gm.Game != null)
                vp = gm.Game.GraphicsDevice.Viewport;
            else
                vp = new Viewport(0, 0, GameManager.WorldWidth, GameManager.WorldHeight);

            _skillTreeCanvas?.Update(gameTime, vp);
            if (_skillTreeCanvas?.CloseRequested == true)
            {
                _showSkillTree = false;
            }
        }
    }

    public void DrawRunningMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        _runningMenu.Draw(gameTime, spriteBatch, transformMatrix);

        if (_showSkillTree && _skillTreeCanvas != null)
        {
            var gm = GameManager.GetGameManager();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);

            Viewport vp = spriteBatch.GraphicsDevice.Viewport;
            spriteBatch.Draw(gm.Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(22, 24, 28, 240));
            _skillTreeCanvas.Draw(gameTime, spriteBatch);

            spriteBatch.End();
        }
    }

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
