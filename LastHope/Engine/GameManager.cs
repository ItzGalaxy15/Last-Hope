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

namespace Last_Hope.Engine;

public class GameManager
{
    private static GameManager gameManager;

    public List<GameObject> _gameObjects;
    public List<GameObject> _toBeRemoved;
    public List<GameObject> _toBeAdded;
    public Camera Camera { get; set; }

    public ContentManager _content;

    // private float _spawnTimer = 0f;
    // private float _spawnInterval = 5f;   // seconds between spawns (starts at 5 s)
    private const float MinSpawnInterval = 0.5f;  // fastest possible rate

    public Random RNG { get; private set; }
    public BasePlayer _player { get; private set; }
    public InputManager InputManager { get; private set; }
    public Game Game { get; private set; }
    public bool playerAlive = true;
    public int Score { get; set; } = 0;
    public Decoy ActiveDecoy { get; set; }
    public int SelectedItemSlot { get; private set; } = 0;

    public const int WorldWidth = 4000;
    public const int WorldHeight = 5000;
    //public Camera Camera { get; private set; }
    public Texture2D Pixel { get; private set; }


    public GameState _state;
    public SpriteFont _font;
    public Menu Menu { get; private set; }


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
            case GameState.Running:
                Menu.UpdateRunningMenu(gameTime);
                break;
            case GameState.Paused:
                Menu.UpdatePausedMenu(gameTime);
                break;
            case GameState.GameOver:
                Menu.UpdateGameOverMenu(gameTime);
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
            case GameState.Running:
                Menu.DrawRunningMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.Paused:
                Menu.DrawPausedMenu(gameTime, spriteBatch, transformMatrix);
                break;
            case GameState.GameOver:
                Menu.DrawGameOverMenu(gameTime, spriteBatch, transformMatrix);
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
    /// Remove GameObject from the GameManager.
    /// The GameObject will be removed at the start of the next Update step and its Destroy() mehtod will be called.
    /// After that the object will no longer receive any updates.
    /// </summary>
    /// <param name="gameObject"> The GameObject to Remove. </param>
    public void RemoveGameObject(GameObject gameObject)
    {
        _toBeRemoved.Add(gameObject);
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

        Warrior player = new Warrior(new Vector2(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height / 2));
        _player = player;

        AddGameObject(_player);
        AddGameObject(new Goblin(new Point(600, 660), new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: null)));
        AddGameObject(new Orc(new Point(300, 360)));
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