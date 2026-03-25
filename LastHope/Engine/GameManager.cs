using System;
using System.Collections.Generic;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine;
    public class GameManager
    {
        private static GameManager gameManager;

        private List<GameObject> _gameObjects;
        private List<GameObject> _toBeRemoved;
        private List<GameObject> _toBeAdded;
        private ContentManager _content;

        // private float _spawnTimer = 0f;
        // private float _spawnInterval = 5f;   // seconds between spawns (starts at 5 s)
        private const float MinSpawnInterval = 0.5f;  // fastest possible rate

        public Random RNG { get; private set; }
        public BasePlayer _player { get; private set; }
        public InputManager InputManager { get; private set; }
        public Game Game { get; private set; }
        public bool playerAlive = true;
        public int Score {get; set;} = 0;

        public const int WorldWidth = 4000;
        public const int WorldHeight = 5000;
        // public Camera Camera { get; private set; }
        public Texture2D Pixel {get; private set; }


        public static GameManager GetGameManager()
        {
            if(gameManager == null)
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
        }

        public void Initialize(ContentManager content, Game game, BasePlayer player)
        {
            Game = game;
            _content = content;
            _player = player;
            Pixel = new Texture2D(Game.GraphicsDevice, 1,1);
            Pixel.SetData(new[] {Color.White});
        }

        public void Load(ContentManager content)
        {
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Load(content);
            }
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
                for (int j = i+1; j < _gameObjects.Count; j++)
                {
                    if (_gameObjects[i].CheckCollision(_gameObjects[j]))
                    {
                        _gameObjects[i].OnCollision(_gameObjects[j]);
                        _gameObjects[j].OnCollision(_gameObjects[i]);
                    }
                }
            }

        }

        // public List<GameObject> GetObjectsInRadius(Vector2 center, float radius)
        // {
        //     List<GameObject> enemies = new List<GameObject>();
        //     foreach(GameObject e in _gameObjects)
        //     {
        //         if (e.GetCollider() == null) continue;

        //         Vector2 objCenter = e.GetCollider().GetBoundingBox().Center.ToVector2();

        //         if (Vector2.Distance(center, objCenter) <= radius)
        //         {
        //             enemies.Add(e);
        //         }
        //     }
        //     return enemies;
        // }

        public void Update(GameTime gameTime)
        {
            InputManager.Update();
            HandleInput(InputManager);

            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Update(gameTime);
            }

            CheckCollision();

            foreach (GameObject gameObject in _toBeAdded)
            {
                gameObject.Load(_content);
                _gameObjects.Add(gameObject);
            }
            _toBeAdded.Clear();

            foreach (GameObject gameObject in _toBeRemoved)
            {
                gameObject.Destroy();
                _gameObjects.Remove(gameObject);
            }
            _toBeRemoved.Clear();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }
            spriteBatch.End();
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

        // public void Reset(ContentManager content, Game game, Ship player)
        // {
        //     _gameObjects.Clear();
        //     _toBeAdded.Clear();
        //     _toBeRemoved.Clear();
        //     playerAlive = true;
        //     _spawnTimer = 0f;
        //     _spawnInterval = 5f;
        //     Score = 0;
        //     Initialize(content, game, player);
        // }
    }
