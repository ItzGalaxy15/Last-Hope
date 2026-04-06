using Last_Hope.Classes.Camera;
using Last_Hope.Engine;
using Last_Hope.Engine.LevelGenerator;
using Last_Hope.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope;

public class Last_Hope : Game
{
    private GraphicsDeviceManager _graphics;
    private InputManager _inputManager;
    private GameManager _gameManager;
    private SpriteBatch _spriteBatch;
    private Texture2D _terrainSheet;
    private Texture2D _decorationsSheet;
    private Texture2D? _itemSpriteSheet;
    private LevelGenerator _levelGenerator;
    private Camera _camera;
    private Warrior _player;
    private Hud _hud;

    public Last_Hope()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.IsFullScreen = false;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Initialize managers
        _inputManager = new InputManager();
        _gameManager = GameManager.GetGameManager();
        _levelGenerator = new LevelGenerator(tileSize: 32);
        base.Initialize();

        _player = new Warrior(new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2));
 
        _gameManager.AddGameObject(_player);
        _gameManager.AddGameObject(new Goblin(new Point(600, 660), new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: null)));
        _gameManager.AddGameObject(new Orc(new Point(300, 360)));
        _gameManager.Initialize(Content, this, _player);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _terrainSheet = Content.Load<Texture2D>("terrain");
        _decorationsSheet = Content.Load<Texture2D>("decorations");

        // Optional item sheet for hotbar icons.
        try
        {
            _itemSpriteSheet = Content.Load<Texture2D>("itemSpriteSheet");
        }
        catch (ContentLoadException)
        {
            _itemSpriteSheet = null;
        }

        _levelGenerator.LoadSpriteSheets(_terrainSheet, _decorationsSheet, terrainUsableRows: 5);
        _levelGenerator.GenerateMap(1920, 1080);

        _gameManager.Load(Content);

        _camera = new Camera(
            new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            new Point(_levelGenerator.MapWidthInTiles * _levelGenerator.TileSize, _levelGenerator.MapHeightInTiles * _levelGenerator.TileSize),
            1.2f);

        _gameManager.Camera = _camera;

        _hud = new Hud(_player, _gameManager.Pixel, _itemSpriteSheet);
    }

    protected override void Update(GameTime gameTime)
    {
        _gameManager.Update(gameTime);
        if (_gameManager.playerAlive && _gameManager._player != null)
            _camera.Update(_gameManager._player.GetPosition());

        _hud?.Update(gameTime, GraphicsDevice.Viewport);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(transformMatrix: _camera.ViewMatrix, samplerState: SamplerState.PointClamp);
        _levelGenerator.Draw(_spriteBatch, Vector2.Zero);
        _spriteBatch.End();

        _gameManager.Draw(gameTime, _spriteBatch, _camera.ViewMatrix);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _hud?.Draw(gameTime, _spriteBatch);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

}