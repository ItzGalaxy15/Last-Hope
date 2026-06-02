using System;
using System.Collections.Generic;
using Last_Hope.Classes.Camera;
using Last_Hope.Engine;
using Last_Hope.Engine.LevelGenerator;
using Last_Hope.Engine.Pathfinding;
using Last_Hope.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace Last_Hope;

public class Last_Hope : Game
{
    private const int MapWidthInTiles = 150;
    private const int MapHeightInTiles = 100;
    public GraphicsDeviceManager Graphics { get; }

    private InputManager _inputManager;
    private GameManager _gameManager;
    private SpriteBatch _spriteBatch;
    private Texture2D _terrainSheet;
    private Texture2D _decorationsSheet;
    private Texture2D _villageSheet;
    private Texture2D? _itemSpriteSheet;
    private Texture2D[] _treeTextures = Array.Empty<Texture2D>();
    private LevelGenerator _levelGenerator;
    private Camera _camera;
    private Hud _hud;

    public Last_Hope()
    {
        Graphics = new GraphicsDeviceManager(this);

        Graphics.PreferredBackBufferWidth = 1920;
        Graphics.PreferredBackBufferHeight = 1080;
        Graphics.IsFullScreen = false;

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

        _levelGenerator.LoadSpriteSheets(_terrainSheet, _decorationsSheet, _villageSheet, terrainUsableRows: 8);

        _treeTextures = new Texture2D[]
        {
            Content.Load<Texture2D>("forest/trees-version-1"),
            Content.Load<Texture2D>("forest/trees-version-2"),
            Content.Load<Texture2D>("forest/trees-version-3"),
            Content.Load<Texture2D>("forest/trees-version-4"),
        };
        _levelGenerator.LoadForestSprites(_treeTextures);

        _levelGenerator.GenerateMap(MapWidthInTiles * _levelGenerator.TileSize, MapHeightInTiles * _levelGenerator.TileSize);

        _gameManager.NavigationGrid = new NavigationGrid(
            _levelGenerator.MapWidthInTiles,
            _levelGenerator.MapHeightInTiles,
            _levelGenerator.TileSize);
        _gameManager.PlayerSpawnSearchCenter = _levelGenerator.VillageCenterTile;
        _gameManager.ForestBoundaryX = _levelGenerator.ForestBoundsInTiles.Right * _levelGenerator.TileSize;

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

        foreach (var collider in _levelGenerator.GetTreeColliders())
        {
            CollisionWorld.RegisterStatic(collider);

            Rectangle bounds = collider.GetBoundingBox();
            const int TreeNavPaddingTiles = 1;
            int tileSize = _levelGenerator.TileSize;
            int tileLeft   = (bounds.Left        / tileSize) - TreeNavPaddingTiles;
            int tileTop    = (bounds.Top         / tileSize) - TreeNavPaddingTiles;
            int tileRight  = ((bounds.Right - 1) / tileSize) + TreeNavPaddingTiles;
            int tileBottom = ((bounds.Bottom - 1)/ tileSize) + TreeNavPaddingTiles;

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

        Texture2D? dashIcon = null;
        try { dashIcon = Content.Load<Texture2D>("icons/dash"); } catch { }

        Texture2D? teleportIcon = null;
        try { teleportIcon = Content.Load<Texture2D>("icons/teleport"); } catch { }

        Texture2D? rapidFireIcon = null;
        try { rapidFireIcon = Content.Load<Texture2D>("icons/AtkSpdUpTemp"); } catch { }

        Texture2D? critGuaranteeIcon = null;
        try { critGuaranteeIcon = Content.Load<Texture2D>("icons/GuarenteedCrit"); } catch { }

        _hud = new Hud(null, _gameManager.Pixel, _itemSpriteSheet, dashIcon, teleportIcon, _gameManager.CooldownIcon, Content, rapidFireIcon, critGuaranteeIcon);

        GumBootstrap.Initialize(this, Content);
    }

    protected override void Update(GameTime gameTime)
    {
        IsMouseVisible = !(KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly && _gameManager._state == GameState.Running);
        _gameManager.Update(gameTime);
        GumService.Default.Update(gameTime);
        if (_gameManager.playerAlive && _gameManager._player != null)
        {
            if (_gameManager.CurrentZone == Zone.Forest)
            {
                _camera.MinX = 0f;
                _camera.MaxX = _gameManager.ForestBoundaryX;
            }
            else
            {
                _camera.MinX = _gameManager.IsForestLocked ? _gameManager.ForestBoundaryX : 0f;
                _camera.MaxX = null;
            }
            _camera.Update(_gameManager._player.GetPosition());
        }

        if (ShouldShowHud(_gameManager._state))
            _hud?.Update(gameTime, GraphicsDevice.Viewport);

        base.Update(gameTime);
    }
    // idea for drawing player behind trees (Y-sorting) = https://eliasdaler.wordpress.com/2013/11/20/z-order-in-top-down-2d-games/
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        Effect backgroundEffect = _gameManager._state == GameState.GameOver ? _gameManager.DeathFade : null;

        // Ground layer: terrain, overlay decorations, village buildings.
        _spriteBatch.Begin(transformMatrix: _camera.ViewMatrix, samplerState: SamplerState.PointClamp, effect: backgroundEffect);
        _levelGenerator.Draw(_spriteBatch, Vector2.Zero);
        _spriteBatch.End();

        // Y-sorted layer: trees and all entities (player + enemies) sorted together by depth.
        var ySortList = new List<(float sortY, Action<SpriteBatch> draw)>();

        _levelGenerator.AppendTreeDrawItems(Vector2.Zero, ySortList);

        foreach (var obj in _gameManager.GetYSortedObjects())
        {
            var captured = obj;
            ySortList.Add((captured.GetSortY(), sb => captured.Draw(gameTime, sb)));
            
            // Allow objects like Warrior to inject background effects (e.g. Shield Slam) into the Y-sort layer
            if (captured is BaseModel.BasePlayer player)
            {
                player.AppendBackgroundDrawItems(ySortList);
            }
        }

        ySortList.Sort((a, b) => a.sortY.CompareTo(b.sortY));

        _spriteBatch.Begin(transformMatrix: _camera.ViewMatrix, samplerState: SamplerState.PointClamp, effect: backgroundEffect);
        foreach ((float _, Action<SpriteBatch> draw) in ySortList)
            draw(_spriteBatch);
        _spriteBatch.End();

        // Non-Y-sorted layer: projectiles, items, UI elements in world space.
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
}