using System;
using System.Collections.Generic;
using System.IO;

using Last_Hope;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;
using Last_Hope.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Classes.Camera;
using Last_Hope.Collision;
using Last_Hope.Engine.Pathfinding;

namespace Last_Hope.Engine;

/// <summary>
/// The central hub of the game engine, responsible for managing the game loop, 
/// state machine, object lifecycle, and global resources.
/// </summary>
/// <remarks>
/// Implements the Singleton design pattern to provide global access to game systems.
/// Follows standard MonoGame architectural patterns for Update/Draw delegation and deferred object addition/removal to prevent collection modification during iteration.
/// </remarks>
public class GameManager
{
    private static GameManager? gameManager;

    public List<GameObject> _gameObjects;
    public List<GameObject> _toBeRemoved;
    public List<GameObject> _toBeAdded;
    public Camera Camera { get; set; }

    public ContentManager _content;

    public Random RNG { get; private set; }
    public BasePlayer? _player { get; private set; }

    /// <summary>Chosen from the character select screen; used for new runs and restarts.</summary>
    public PlayerCharacterKind SelectedCharacter { get; set; } = PlayerCharacterKind.Warrior;
    public InputManager InputManager { get; private set; }
    public Game Game { get; private set; }
    public bool playerAlive = true;
    public int Score { get; set; } = 0;
    public Decoy ActiveDecoy { get; set; }
    public bool HasUsedOneUp { get; set; } = false;
    public int SelectedItemSlot { get; private set; } = 0;

    public const int WorldWidth = 4000;
    public const int WorldHeight = 5000;

    // --- Configurable Item Drop Chances ---
    public readonly Dictionary<ItemType, double> ItemDropChances = new Dictionary<ItemType, double>
    {
        { ItemType.Bomb, 0.10 },          // 10% chance
        { ItemType.Decoy, 0.05 },         // 5% chance
        { ItemType.HealingPotion, 0.12 }, // 12% chance
        { ItemType.OneUp, 0.03 }          // 3% chance
    };
    public Texture2D Pixel { get; private set; }


    public GameState _state;
    public SpriteFont _font;

    /// <summary>State to enter when closing settings with Esc/Q (main menu from title hub, paused when opened from pause).</summary>
    public GameState StateAfterClosingSettings { get; set; } = GameState.MainMenu;

    /// <summary>Bitmap font from <c>Content/Font.fnt</c> + <c>Content/Font.png</c> when present next to the executable.</summary>
    public BmFont? FontBitmap { get; private set; }
    public Menu Menu { get; private set; }
    public EnemySpawner EnemySpawner { get; private set; }

    /// <summary>
    /// Tile grid for enemy pathfinding; set after level generation. Mark cells non-walkable when adding blocking collision.
    /// </summary>
    public NavigationGrid NavigationGrid { get; set; }
    public Point PlayerSpawnSearchCenter { get; set; } = new Point(-1, -1);

    public Effect DeathFade { get; private set; }
    public Effect? CooldownIcon { get; private set; }

    /// <summary>
    /// Gets the singleton instance of the GameManager.
    /// </summary>
    /// <returns>The global <see cref="GameManager"/> instance.</returns>
    public static GameManager GetGameManager()
    {
        gameManager ??= new GameManager();
        return gameManager;
    }

    public GameManager()
    {
        _gameObjects = new List<GameObject>();
        _toBeRemoved = new List<GameObject>();
        _toBeAdded = new List<GameObject>();
        InputManager = new InputManager();
        RNG = new Random();

        Menu = new Menu();
        EnemySpawner = new EnemySpawner();

        _state = GameState.MainMenu;
        SelectedItemSlot = 0;
    }

    /// <summary>
    /// Initializes core engine references and graphics resources.
    /// </summary>
    /// <param name="content">The MonoGame content manager.</param>
    /// <param name="game">The main Game instance.</param>
    /// <param name="player">The initial player character, if any.</param>
    public void Initialize(ContentManager content, Game game, BasePlayer? player)
    {
        Game = game;
        _content = content;
        _player = player;
        Pixel = new Texture2D(Game.GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Loads global assets and triggers loading for all currently registered GameObjects.
    /// </summary>
    /// <param name="content">The MonoGame content manager.</param>
    public void Load(ContentManager content)
    {
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Load(content);
        }

        _font = content.Load<SpriteFont>("Fonts/font");
        DeathFade = content.Load<Effect>("Effects/DeathFade");
        try { CooldownIcon = content.Load<Effect>("Effects/CooldownIcon"); } catch { CooldownIcon = null; }

        FontBitmap = null;
        try
        {
            string fntPath = Path.Combine(AppContext.BaseDirectory, "Content", "Font.fnt");
            if (File.Exists(fntPath) && Game?.GraphicsDevice != null)
                FontBitmap = BmFont.TryLoad(Game.GraphicsDevice, fntPath);
        }
        catch
        {
            FontBitmap = null;
        }
    }

    /// <summary>
    /// Measures the dimensions of a given string using the appropriate active font.
    /// </summary>
    /// <param name="fallback">The SpriteFont to use if a Bitmap font is not loaded.</param>
    /// <param name="text">The string to measure.</param>
    /// <param name="scale">The scaling factor applied to the text.</param>
    /// <returns>A Vector2 representing the width and height of the text block.</returns>
    public Vector2 MeasureUiString(SpriteFont? fallback, string text, float scale)
    {
        if (string.IsNullOrEmpty(text))
            return Vector2.Zero;
        if (FontBitmap != null)
            return FontBitmap.MeasureString(text, scale);
        if (fallback == null)
            return Vector2.Zero;
        return fallback.MeasureString(text) * scale;
    }

    /// <summary>
    /// Draws a string to the screen, automatically handling the choice between Bitmap and Sprite fonts.
    /// </summary>
    public void DrawUiString(SpriteBatch spriteBatch, SpriteFont? fallback, string text, Vector2 position, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects, float layerDepth)
    {
        if (string.IsNullOrEmpty(text))
            return;
        if (FontBitmap != null)
        {
            FontBitmap.Draw(spriteBatch, text, position, color, scale, layerDepth);
            return;
        }

        if (fallback != null)
            spriteBatch.DrawString(fallback, text, position, color, rotation, origin, scale, effects, layerDepth);
    }

    /// <summary>
    /// Draws a string to the screen, automatically handling the choice between Bitmap and Sprite fonts.
    /// </summary>
    public void DrawUiString(SpriteBatch spriteBatch, SpriteFont? fallback, string text, Vector2 position, Color color, float scale) =>
        DrawUiString(spriteBatch, fallback, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

    /// <summary>
    /// Draws a string to the screen, automatically handling the choice between Bitmap and Sprite fonts.
    /// </summary>
    public void DrawUiString(SpriteBatch spriteBatch, SpriteFont? fallback, string text, Vector2 position, Color color) =>
        DrawUiString(spriteBatch, fallback, text, position, color, 1f);

    /// <summary>
    /// Dispatches input handling to all active GameObjects.
    /// </summary>
    public void HandleInput(InputManager inputManager)
    {
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.HandleInput(this.InputManager);
        }
    }

    /// <summary>
    /// Performs a naive O(N^2) broad-phase collision check across all active GameObjects.
    /// </summary>
    /// <remarks>
    /// Based on a standard brute-force collision checking approach. 
    /// Iterates through all unique pairs of objects and delegates intersection testing to their colliders.
    /// </remarks>
    public void CheckCollision()
    {
        for (int i = 0; i < _gameObjects.Count; i++)
        {
            for (int j = i + 1; j < _gameObjects.Count; j++)
            {
                if (_gameObjects[i].CheckCollision(_gameObjects[j]))
                {
                    _gameObjects[i].OnCollision(_gameObjects[j]);
                    _gameObjects[j].OnCollision(_gameObjects[i]);
                }
            }
        }

    }

    /// <summary>
    /// Finds all GameObjects within a specified radius from a given center point.
    /// </summary>
    /// <param name="center">The origin point of the search.</param>
    /// <param name="radius">The maximum distance from the center.</param>
    /// <returns>A list of objects that fall within the radius.</returns>
    /// <remarks>
    /// Uses squared distance comparisons to avoid expensive square root operations, a common optimization in game loops.
    /// </remarks>
    public List<GameObject> GetObjectsInRadius(Vector2 center, float radius)
    {
        List<GameObject> objectsInRadius = new List<GameObject>();
        float radiusSquared = radius * radius;
        foreach (GameObject e in _gameObjects)
        {
            if (e.GetCollider() == null) continue;

            Vector2 objCenter = e.GetCollider().GetBoundingBox().Center.ToVector2();

            if (Vector2.DistanceSquared(center, objCenter) <= radiusSquared)
            {
                objectsInRadius.Add(e);
            }
        }
        return objectsInRadius;
    }

    /// <summary>
    /// The main update loop for the game engine. Routes logic based on the current <see cref="GameState"/>.
    /// </summary>
    /// <param name="gameTime">Snapshot of the game's timing state.</param>
    /// <remarks>
    /// Operates conceptually as a Finite State Machine (FSM), delegating specific update logic to the <see cref="Menu"/> or active game systems.
    /// </remarks>
    public void Update(GameTime gameTime)
    {
        InputManager.Update();

        GameState stateAtFrameStart = _state;

        switch (_state)
        {
            case GameState.MainMenu:
                Menu.UpdateMainMenu(gameTime);
                break;
            case GameState.Characters:
                Menu.UpdateCharactersRosterMenu(gameTime);
                break;
            case GameState.CharacterSelect:
                Menu.UpdateCharacterSelectMenu(gameTime);
                break;
            case GameState.ItemsIndex:
                Menu.UpdateItemsIndexMenu(gameTime);
                break;
            case GameState.SettingsMenu:
                Menu.UpdateSettingsMenu(gameTime);
                break;
            case GameState.Running:
                EnemySpawner.Update(gameTime);
                Menu.UpdateRunningMenu(gameTime);
                break;
            case GameState.Paused:
                Menu.UpdatePausedMenu(gameTime);
                break;
            case GameState.GameOver:
                Menu.UpdateGameOverMenu(gameTime);
                break;
            case GameState.Winner:
                Menu.UpdateWinnerMenu(gameTime);
                break;
        }

        if (stateAtFrameStart == GameState.MainMenu && _state != GameState.MainMenu)
            Menu.ReleaseMainMenuGum();

        if (stateAtFrameStart == GameState.Paused && _state != GameState.Paused)
            Menu.ReleasePausedMenuGum();
    }

    /// <summary>
    /// The main draw loop for the game engine. Routes rendering based on the current <see cref="GameState"/>.
    /// </summary>
    /// <param name="gameTime">Snapshot of the game's timing state.</param>
    /// <param name="spriteBatch">The SpriteBatch used for drawing 2D textures.</param>
    /// <param name="transformMatrix">Optional camera transform matrix applied to rendering.</param>
    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        switch (_state)
        {
            case GameState.MainMenu:
                Menu.DrawMainMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.Characters:
                Menu.DrawCharactersRosterMenu(gameTime, spriteBatch);
                break;
            case GameState.CharacterSelect:
                Menu.DrawCharacterSelectMenu(gameTime, spriteBatch);
                break;
            case GameState.ItemsIndex:
                Menu.DrawItemsIndexMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.SettingsMenu:
                Menu.DrawSettingsMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.Running:
                Menu.DrawRunningMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.Paused:
                Menu.DrawPausedMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.GameOver:
                Menu.DrawGameOverMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.Winner:
                Menu.DrawWinnerMenu(gameTime, spriteBatch, transformMatrix);
                break;
        }
    }


    /// <summary>
    /// Add a new GameObject to the GameManager.
    /// The GameObject will be added at the start of the next Update step.
    /// Once it is added, the GameManager will ensure all steps of the game loop will be called on the object automatically.
    /// </summary>
    /// <param name="gameObject"> The GameObject to add. </param>
    public void AddGameObject(GameObject gameObject)
    {
        _toBeAdded.Add(gameObject);
    }

    /// <summary>
    /// Evaluates if a One-Up item is already present in the world, in the player's inventory, or active.
    /// </summary>
    /// <returns><c>true</c> if a One-Up is active or available; otherwise, <c>false</c>.</returns>
    private bool IsOneUpAlreadyActive()
    {
        if (HasUsedOneUp) return true;

        if (PlayerInventoryHelper.GetHudExtraLives(_player) > 0)
            return true;

        ItemType[]? slots = PlayerInventoryHelper.GetInventorySlots(_player);
        if (slots != null)
        {
            foreach (ItemType item in slots)
            {
                if (item == ItemType.OneUp) return true;
            }
        }

        foreach (var obj in _gameObjects)
        {
            if (obj is ItemDrop item && item.Type == ItemType.OneUp) return true;
        }

        foreach (var obj in _toBeAdded)
        {
            if (obj is ItemDrop item && item.Type == ItemType.OneUp) return true;
        }

        return false;
    }

    /// <summary>
    /// Removes a GameObject from the active simulation.
    /// </summary>
    /// <remarks>
    /// The GameObject will be removed at the start of the next Update step and its Destroy() method will be called.
    /// After that the object will no longer receive any updates.
    /// Handles dropping random items when an enemy object is destroyed.
    /// </summary>
    /// <param name="gameObject"> The GameObject to Remove. </param>
    public void RemoveGameObject(GameObject gameObject)
    {
        if (!_toBeRemoved.Contains(gameObject))
        {
            _toBeRemoved.Add(gameObject);

            if (gameObject is BaseEnemy enemy && enemy.CurrentHealth <= 0)
            {
                double roll = RNG.NextDouble();
                double cumulative = 0.0;
                ItemType droppedType = ItemType.None;

                foreach (var drop in ItemDropChances)
                {
                    cumulative += drop.Value;
                    if (roll < cumulative)
                    {
                        droppedType = drop.Key;
                        break;
                    }
                }

                if (droppedType != ItemType.None)
                {
                    if (droppedType == ItemType.OneUp && IsOneUpAlreadyActive())
                    {
                        droppedType = ItemType.HealingPotion;
                    }

                    AddGameObject(new ItemDrop(enemy.GetPosition(), droppedType));
                }
            }
        }
    }

    /// <summary>
    /// Get a random location on the screen.
    /// </summary>
    public Vector2 RandomScreenLocation()
    {
        return new Vector2(
            RNG.Next(0, Game.GraphicsDevice.Viewport.Width),
            RNG.Next(0, Game.GraphicsDevice.Viewport.Height));
    }

    public void SetSelectedItemSlot(int slotIndex)
    {
        SelectedItemSlot = Math.Clamp(slotIndex, 0, 1);
    }

    /// <summary>
    /// Resets the engine state for a new game run. 
    /// Clears all game objects, resets score and inventory, and spawns a fresh player character.
    /// </summary>
    public void ResetGame()
    {
        _gameObjects.Clear();
        _toBeAdded.Clear();
        _toBeRemoved.Clear();

        // Reset player state
        playerAlive = true;
        Score = 0;
        ActiveDecoy = null;
        SelectedItemSlot = 0;
        HasUsedOneUp = false;

        EnemySpawner.Reset();

        Vector2 spawn = GetDefaultPlayerSpawn();
        _player = CreatePlayerFromSelection(spawn);
        AddGameObject(_player);
    }

    /// <summary>Matches player draw scale (32px frame × 3) for spawn search and clamping.</summary>
    private const float SpawnBodyWidthPx = 96f;
    /// <summary>Same fraction as <see cref="Warrior"/> / <see cref="Archer"/> hitbox vs body.</summary>
    private const float SpawnHitboxFraction = 0.55f;

    private static bool PlayerFootprintOverlapsStatic(float bodyWidth, Vector2 topLeftWorld)
    {
        float hitboxSize = bodyWidth * SpawnHitboxFraction;
        float inset = (bodyWidth - hitboxSize) * 0.5f;
        var rect = new Rectangle(
            (int)(topLeftWorld.X + inset),
            (int)(topLeftWorld.Y + inset),
            (int)hitboxSize,
            (int)hitboxSize);
        return CollisionWorld.CollidesWithStatic(new RectangleCollider(rect));
    }

    /// <summary>
    /// First walkable nav tile (from map center outward) whose footprint does not overlap static geometry.
    /// </summary>
    /// <remarks>
    /// Uses a concentric/spiral search algorithm to find the nearest valid grid cell starting from the center.
    /// </remarks>
    private bool TryFindSpawnTopLeft(out Vector2 spawnTopLeft)
    {
        spawnTopLeft = Vector2.Zero;
        if (NavigationGrid == null)
            return false;

        int ts = NavigationGrid.TileSize;
        float mapW = NavigationGrid.WidthInTiles * ts;
        float mapH = NavigationGrid.HeightInTiles * ts;
        float body = SpawnBodyWidthPx;

        int cx = PlayerSpawnSearchCenter.X >= 0 ? PlayerSpawnSearchCenter.X : NavigationGrid.WidthInTiles / 2;
        int cy = PlayerSpawnSearchCenter.Y >= 0 ? PlayerSpawnSearchCenter.Y : NavigationGrid.HeightInTiles / 2;
        int maxD = Math.Max(NavigationGrid.WidthInTiles, NavigationGrid.HeightInTiles);

        for (int d = 0; d <= maxD; d++)
        {
            for (int ty = cy - d; ty <= cy + d; ty++)
            {
                for (int tx = cx - d; tx <= cx + d; tx++)
                {
                    if (Math.Max(Math.Abs(tx - cx), Math.Abs(ty - cy)) != d)
                        continue;

                    if (!NavigationGrid.IsWalkable(tx, ty))
                        continue;

                    Vector2 pos = new Vector2(tx * ts, ty * ts);
                    if (pos.X + body > mapW || pos.Y + body > mapH)
                        continue;

                    if (PlayerFootprintOverlapsStatic(body, pos))
                        continue;

                    spawnTopLeft = pos;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// World-space spawn on a free tile (not map geometric center — that often sits inside village colliders).
    /// </summary>
    public Vector2 GetDefaultPlayerSpawn()
    {
        if (NavigationGrid != null && TryFindSpawnTopLeft(out Vector2 spawn))
            return spawn;

        if (NavigationGrid != null)
        {
            float mapW = NavigationGrid.WidthInTiles * NavigationGrid.TileSize;
            float mapH = NavigationGrid.HeightInTiles * NavigationGrid.TileSize;
            return new Vector2((mapW - SpawnBodyWidthPx) * 0.5f, (mapH - SpawnBodyWidthPx) * 0.5f);
        }

        return new Vector2(WorldWidth / 2f - 48f, WorldHeight / 2f - 48f);
    }

    /// <summary>
    /// Instantiates a new player character based on the user's selection from the title screen.
    /// </summary>
    /// <param name="spawnPosition">The world-space coordinates where the player should start.</param>
    public BasePlayer CreatePlayerFromSelection(Vector2 spawnPosition)
    {
        if (!PlayableCharacterRegistry.TryGet(SelectedCharacter, out _))
            SelectedCharacter = PlayableCharacterRegistry.DefaultKind;
        return PlayableCharacterRegistry.Create(SelectedCharacter, spawnPosition);
    }

    /// <summary>
    /// Transforms the screen-space mouse coordinates into world-space coordinates using the active Camera.
    /// </summary>
    /// <returns>A Vector2 representing the mouse position in the game world.</returns>
    public Vector2 GetWorldMousePosition()
    {
        Vector2 screenMousePos = InputManager.CurrentMouseState.Position.ToVector2();
        if (Camera != null)
        {
            return Vector2.Transform(screenMousePos, Matrix.Invert(Camera.ViewMatrix));
        }
        return screenMousePos;
    }
}
