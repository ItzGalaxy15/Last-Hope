using System;
using System.Collections.Generic;

using Last_Hope.BaseModel;
using Last_Hope.UI;
using Last_Hope.Classes.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Classes.Camera;
using Last_Hope.Engine.Pathfinding;

namespace Last_Hope.Engine;

public class GameManager
{
    private static GameManager gameManager;

    public List<GameObject> _gameObjects;
    public List<GameObject> _toBeRemoved;
    public List<GameObject> _toBeAdded;
    public Camera Camera { get; set; }

    public ContentManager _content;

    public Random RNG { get; private set; }
    public BasePlayer _player { get; private set; }
    public InputManager InputManager { get; private set; }
    public Game Game { get; private set; }
    public bool playerAlive = true;
    public int Score { get; set; } = 0;
    public Decoy ActiveDecoy { get; set; }
    public bool HasUsedOneUp { get; set; } = false;
    public int SelectedItemSlot { get; private set; } = 0;

    public const int WorldWidth = 4000;
    public const int WorldHeight = 5000;
    //public Camera Camera { get; private set; }

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
    public Menu Menu { get; private set; }
    public EnemySpawner EnemySpawner { get; private set; }

    /// <summary>
    /// Tile grid for enemy pathfinding; set after level generation. Mark cells non-walkable when adding blocking collision.
    /// </summary>
    public NavigationGrid? NavigationGrid { get; set; }

    public static GameManager GetGameManager()
    {
        if (gameManager == null)
            gameManager = new GameManager();
        return gameManager;
    }
    public GameManager()
    {
        _gameObjects = new List<GameObject>();
        _toBeRemoved = new List<GameObject>();
        _toBeAdded = new List<GameObject>();
        InputManager = new InputManager();
        RNG = new Random();
        // Camera = new Camera();

        Menu = new Menu();
        EnemySpawner = new EnemySpawner();

        _state = GameState.StartMenu;
        SelectedItemSlot = 0;
    }

    public void Initialize(ContentManager content, Game game, BasePlayer player)
    {
        Game = game;
        _content = content;
        _player = player;
        Pixel = new Texture2D(Game.GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });
    }

    public void Load(ContentManager content)
    {
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Load(content);
        }

        _font = content.Load<SpriteFont>("Fonts/font");
    }

    public void HandleInput(InputManager inputManager)
    {
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.HandleInput(this.InputManager);
        }
    }

    public void CheckCollision()
    {
        // Checks once for every pair of 2 GameObjects if the collide.
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

    public List<GameObject> GetObjectsInRadius(Vector2 center, float radius)
    {
        List<GameObject> enemies = new List<GameObject>();
        foreach (GameObject e in _gameObjects)
        {
            if (e.GetCollider() == null) continue;

            Vector2 objCenter = e.GetCollider().GetBoundingBox().Center.ToVector2();

            if (Vector2.Distance(center, objCenter) <= radius)
            {
                enemies.Add(e);
            }
        }
        return enemies;
    }


    public void Update(GameTime gameTime)
    {
        InputManager.Update();
        switch (_state)
        {
            case GameState.StartMenu:
                Menu.UpdateStartMenu(gameTime);
                break;
            case GameState.ControlsMenu:
                Menu.UpdateControlsMenu(gameTime);
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
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        switch (_state)
        {
            case GameState.StartMenu:
                Menu.DrawStartMenu(gameTime, spriteBatch);
                break;
            case GameState.ControlsMenu:
                Menu.DrawControlsMenu(gameTime, spriteBatch, transformMatrix);
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

    private bool IsOneUpAlreadyActive()
    {
        if (HasUsedOneUp) return true;

        if (_player is Warrior warrior)
        {
            if (warrior.ExtraLives > 0) return true;
            
            foreach (ItemType item in warrior.Inventory)
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
    /// Remove GameObject from the GameManager.
    /// The GameObject will be removed at the start of the next Update step and its Destroy() mehtod will be called.
    /// After that the object will no longer receive any updates.
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

        Warrior player = new Warrior(new Vector2(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height / 2));
        _player = player;
        AddGameObject(_player);
    }

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