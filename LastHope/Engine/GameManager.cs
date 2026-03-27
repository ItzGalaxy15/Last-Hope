using System;
using System.Collections.Generic;

using Last_Hope.BaseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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
        //public Camera Camera { get; private set; }
        public Texture2D Pixel {get; private set; }


        public GameState _state;
        private SpriteFont _font;


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

            _state = GameState.StartMenu;
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
                    UpdateStartMenu(gameTime);
                    break;
                case GameState.Running:
                    UpdateRunning(gameTime);
                    break;
                case GameState.Paused:
                    UpdatePaused(gameTime);
                    break;
                case GameState.GameOver:
                    UpdateGameOver(gameTime);
                    break;
            }
        }


        public void UpdateStartMenu(GameTime gameTime)
        {
            if (InputManager.IsKeyPress(Keys.Enter)) _state = GameState.Running;
            if (InputManager.IsKeyPress(Keys.Escape)) Game.Exit();

        }

        public void UpdateRunning(GameTime gameTime)
        {

            if (InputManager.IsKeyPress(Keys.Space))
            {
                _state = GameState.Paused;
                return;
            }
            if (InputManager.IsKeyPress(Keys.Escape))
            {
                Game.Exit();
                return;
            }

            // Handle input
            HandleInput(InputManager);


            // Update
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Update(gameTime);
            }




            // Check Collission
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

        public void UpdatePaused(GameTime gameTime)
        {
            if (InputManager.IsKeyPress(Keys.Space)) _state = GameState.Running;
            if (InputManager.IsKeyPress(Keys.Escape)) Game.Exit();

        }
        public void UpdateGameOver(GameTime gameTime)
        {
            if (InputManager.IsKeyPress(Keys.Enter))
            {
                ResetGame();
                _state = GameState.Running;
            }


            if (InputManager.IsKeyPress(Keys.Escape)) Game.Exit();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {
            switch (_state)
            {
                case GameState.StartMenu:
                    DrawStartMenu(gameTime, spriteBatch, transformMatrix);
                    break;
                case GameState.Running:
                    DrawRunning(gameTime, spriteBatch, transformMatrix);
                    break;
                case GameState.Paused:
                    DrawPaused(gameTime, spriteBatch, transformMatrix);
                    break;
                case GameState.GameOver:
                    DrawGameOver(gameTime, spriteBatch, transformMatrix);
                    break;
            }
        }


        public void DrawStartMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {
            string enterText = "Press Enter to start the Game";
            string escapeText = "Press Escape to quit";
            Vector2 positionEnter = GetFontPosition(enterText);
            Vector2 positionEscape = GetFontPosition(escapeText);

            spriteBatch.Begin();
            spriteBatch.DrawString(_font, enterText, positionEnter, Color.White);
            spriteBatch.DrawString(_font, escapeText, positionEscape + new Vector2(0, 100), Color.Red);
            spriteBatch.End();
        }

        public void DrawRunning(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {

            string spaceText = "Press Space to pause the Game";
            string escapeText = "Press Escape to quit";
            Vector2 topLeft = new Vector2(20, 20);

            spriteBatch.Begin(transformMatrix: transformMatrix);
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }
             spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.DrawString(_font, spaceText, topLeft, Color.White);
            spriteBatch.DrawString(_font, escapeText, topLeft + new Vector2(0, 100), Color.Red);
            spriteBatch.End();
        }

        public void DrawPaused(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {
            string spaceText = "Press Space to continue the Game";
            string escapeText = "Press Escape to quit";
            Vector2 positionEnter = GetFontPosition(spaceText);
            Vector2 positionEscape = GetFontPosition(escapeText);


            spriteBatch.Begin(transformMatrix: transformMatrix);
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }
            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.DrawString(_font, spaceText, positionEnter, Color.White);
            spriteBatch.DrawString(_font, escapeText, positionEscape + new Vector2(0, 100), Color.Red);
            spriteBatch.End();
        }

        public void DrawGameOver(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {
            string gameOverText = "Game Over";
            string enterText = "Press Enter to restart the Game";
            string escapeText = "Press Escape to quit";
            Vector2 positrionGameOver = GetFontPosition(gameOverText);
            Vector2 positionEnter = GetFontPosition(enterText);
            Vector2 positionEscape = GetFontPosition(escapeText);

            spriteBatch.Begin(transformMatrix: transformMatrix);
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }

            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.DrawString(_font, gameOverText, positrionGameOver, Color.Red);
            spriteBatch.DrawString(_font, enterText, positionEnter + new Vector2(0, 100), Color.White);
            spriteBatch.DrawString(_font, escapeText, positionEscape + new Vector2(0, 200), Color.Red);
            spriteBatch.End();
        }


        public Vector2 GetFontPosition(string text)
        {
            Viewport viewport = Game.GraphicsDevice.Viewport;
            Vector2 center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
            Vector2 textSize = _font.MeasureString(text);
            Vector2 position = new Vector2(center.X - textSize.X / 2f, center.Y - textSize.Y / 2f);
            return position;
        }


    //public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    //{
    //    spriteBatch.Begin(transformMatrix: transformMatrix);
    //    foreach (GameObject gameObject in _gameObjects)
    //    {
    //        gameObject.Draw(gameTime, spriteBatch);
    //    }
    //    spriteBatch.End();
    //}



    //public void Update(GameTime gameTime)
    //{
    //    InputManager.Update();
    //    HandleInput(InputManager);

    //    foreach (GameObject gameObject in _gameObjects)
    //    {
    //        gameObject.Update(gameTime);
    //    }

    //    CheckCollision();

    //    foreach (GameObject gameObject in _toBeAdded)
    //    {
    //        gameObject.Load(_content);
    //        _gameObjects.Add(gameObject);
    //    }
    //    _toBeAdded.Clear();

    //    foreach (GameObject gameObject in _toBeRemoved)
    //    {
    //        gameObject.Destroy();
    //        _gameObjects.Remove(gameObject);
    //    }
    //    _toBeRemoved.Clear();
    //}





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


        public void ResetGame()
        {
            _gameObjects.Clear();
            _toBeAdded.Clear();
            _toBeRemoved.Clear();

            // Reset player state
            playerAlive = true;
            Score = 0;

            Warrior player = new Warrior(new Vector2(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height / 2));
            _player = player;

            AddGameObject(_player);
            AddGameObject(new Goblin(new Point(600, 660)));
            AddGameObject(new Orc(new Point(300, 360)));
    }
}
