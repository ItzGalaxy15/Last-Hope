using Last_Hope.Classes.Camera;
using Last_Hope.Engine;
using Last_Hope.Engine.LevelGenerator;
using Last_Hope.Engine.Pathfinding;
using Last_Hope.UI;
using Microsoft.Xna.Framework;
using MonoGameGum;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Last_Hope;

public class Last_Hope : Game
{
    private const int MapWidthInTiles = 100;
    private const int MapHeightInTiles = 50;

    private GraphicsDeviceManager _graphics;
    private InputManager _inputManager;
    private GameManager _gameManager;
    private SpriteBatch _spriteBatch;
    private Texture2D _terrainSheet;
    private Texture2D _decorationsSheet;
    private Texture2D _villageSheet;
    private Texture2D? _itemSpriteSheet;
    private LevelGenerator _levelGenerator;
    private Camera _camera;
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

        _gameManager.Initialize(Content, this, null);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _terrainSheet = Content.Load<Texture2D>("terrain");
        _decorationsSheet = Content.Load<Texture2D>("decorations");
        _villageSheet = Content.Load<Texture2D>("VillageSpriteSheetNew");

        // Optional item sheet for hotbar icons.
        try
        {
            _itemSpriteSheet = Content.Load<Texture2D>("itemSpriteSheet");
        }
        catch (ContentLoadException)
        {
            _itemSpriteSheet = null;
        }

        _levelGenerator.LoadSpriteSheets(_terrainSheet, _decorationsSheet, _villageSheet, terrainUsableRows: 5);
        _levelGenerator.GenerateMap(MapWidthInTiles * _levelGenerator.TileSize, MapHeightInTiles * _levelGenerator.TileSize);

        _gameManager.NavigationGrid = new NavigationGrid(
            _levelGenerator.MapWidthInTiles,
            _levelGenerator.MapHeightInTiles,
            _levelGenerator.TileSize);

        // Register building colliders and block them in the navigation grid
        CollisionWorld.ClearStatic();
        foreach (var collider in _levelGenerator.GetVillageBuildingColliders())
        {
            CollisionWorld.RegisterStatic(collider);

            // Mark every tile the building footprint covers as non-walkable.
            // Inflate by one tile on every side so the A* path (which plans from the
            // enemy's point position) leaves room for the enemy's hitbox — otherwise
            // paths hug the wall and enemies just grind against the collider.
            Rectangle bounds = collider.GetBoundingBox();
            const int NavPaddingTiles = 1;
            int tileSize = _levelGenerator.TileSize;
            int tileLeft   = (bounds.Left        / tileSize) - NavPaddingTiles;
            int tileTop    = (bounds.Top         / tileSize) - NavPaddingTiles;
            int tileRight  = ((bounds.Right - 1) / tileSize) + NavPaddingTiles;
            int tileBottom = ((bounds.Bottom - 1)/ tileSize) + NavPaddingTiles;

            for (int ty = tileTop; ty <= tileBottom; ty++)
                for (int tx = tileLeft; tx <= tileRight; tx++)
                    _gameManager.NavigationGrid.SetWalkable(tx, ty, false);
        }

        _gameManager.Load(Content);

        _camera = new Camera(
            new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            new Point(_levelGenerator.MapWidthInTiles * _levelGenerator.TileSize, _levelGenerator.MapHeightInTiles * _levelGenerator.TileSize),
            1.2f);

        _gameManager.Camera = _camera;

        _hud = new Hud(null, _gameManager.Pixel, _itemSpriteSheet);

        GumBootstrap.Initialize(this, Content);
    }

    protected override void Update(GameTime gameTime)
    {
        _gameManager.Update(gameTime);
        GumService.Default.Update(gameTime);
        if (_gameManager.playerAlive && _gameManager._player != null)
            _camera.Update(_gameManager._player.GetPosition());

        if (ShouldShowHud(_gameManager._state))
            _hud?.Update(gameTime, GraphicsDevice.Viewport);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Effect backgroundEffect = _gameManager._state == GameState.GameOver ? _gameManager.DeathFade : null;

        _spriteBatch.Begin(transformMatrix: _camera.ViewMatrix, samplerState: SamplerState.PointClamp, effect: backgroundEffect);
        _levelGenerator.Draw(_spriteBatch, Vector2.Zero);
        _spriteBatch.End();

        _gameManager.Draw(gameTime, _spriteBatch, _camera.ViewMatrix);

        GumService.Default.Draw();

        if (ShouldShowHud(_gameManager._state))
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _hud?.Draw(gameTime, _spriteBatch);
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }

    private static bool ShouldShowHud(GameState state) =>
        state is GameState.Running or GameState.Paused;

    private Rectangle GetPlayerSpawnArea()
    {
        int size = 6 * _levelGenerator.TileSize; // safe radius around player

        Vector2 center = new Vector2(
            MapWidthInTiles * _levelGenerator.TileSize / 2f,
            MapHeightInTiles * _levelGenerator.TileSize / 2f
        );

        return new Rectangle(
            (int)(center.X - size / 2),
            (int)(center.Y - size / 2),
            size,
            size
        );
    }
}