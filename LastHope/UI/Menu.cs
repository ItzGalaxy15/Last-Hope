using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.BaseModel;
using Last_Hope.SkillTree; // Needed for new Skill Tree architecture
using System.IO;
using System.Text.Json;
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

    private SkillTreeMenuCanvas _skillTreeCanvas;
    private bool _showSkillTree = false;

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
                // 1. Load JSON Data (Updated to your new folder structure)
                string jsonPath = "SkillTree/WarriorSkillTree.json";
                ClassSkillTreeData treeData = null;
                
                if (File.Exists(jsonPath))
                {
                    string rawJson = File.ReadAllText(jsonPath);
                    treeData = JsonSerializer.Deserialize<ClassSkillTreeData>(rawJson);
                }

                // [DEBUG STEP 4]: Failsafe Null Check
                if (treeData == null) 
                { 
                    throw new System.Exception($"[SkillTree Error] Data is null! Could not find JSON at: {Path.GetFullPath(jsonPath)} \nEnsure the file's 'Copy to Output Directory' property is set to 'Copy if newer' in Visual Studio!"); 
                }
                if (treeData.Nodes == null || treeData.Nodes.Count == 0)
                {
                    throw new System.Exception("[SkillTree Error] JSON loaded, but Nodes list is empty! Check your JSON structure.");
                }

                // [DEBUG STEP 1]: Log node instantiation count
                System.Console.WriteLine($"[SkillTree UI] Successfully parsed {treeData.Nodes.Count} nodes from JSON.");
                
                // 2. Load Persisted Player State
                SkillTreeState state = SkillTreeSaveManager.Load("Warrior");
                
                // 3. Build Logic Tree
                BaseSkillTree tree = new BaseSkillTree(treeData, state);
                
                // 4. Wire the Stats Bus directly into the Player Component
                if (gm._player is Warrior warrior)
                {
                    tree.OnEffectApplied += warrior.ApplyNodeEffect;
                    tree.OnTreeRespec += warrior.RevertAllSkillStats;
                }
                
                // 5. Build Dynamic UI Canvas
                UIThemeData theme = new UIThemeData { 
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
