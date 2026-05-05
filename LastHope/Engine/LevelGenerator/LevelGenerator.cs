using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    /// <summary>
    ///  implemented the Wave Function Collapse algorithm as originally published by Maxim Gumin
    /// (github.com/mxgmn/WaveFunctionCollapse, MIT License). The algorithm structure
    ///  observe, collapse, propagate, retry — follows his design. The implementation is my own,
    /// adapted for MonoGame with pixel-based edge compatibility instead of XML adjacency rules
    /// </summary>
    internal partial class LevelGenerator
    {
        private const int Up = 0;
        private const int Right = 1;
        private const int Down = 2;
        private const int Left = 3;

        private readonly Random _random;
        private readonly List<Rectangle> _terrainTiles;
        private readonly List<Rectangle> _decorationTiles;
        private readonly List<AnimatedDecoration> _animatedDecorations;

        private Texture2D? _terrainSheet;
        private Texture2D? _decorationsSheet;
        private int[,]? _map;
        private int[,]? _overlayMap;
        private float[]? _weights;
        private int _terrainColumns;
        private int _terrainRows;
        private int _decorationColumns;
        private int _decorationRows;

        // Public properties
        public int TileSize { get; }
        public int MapWidthInTiles => _map?.GetLength(0) ?? 0;
        public int MapHeightInTiles => _map?.GetLength(1) ?? 0;

        // Tuning knobs exposed so callers can tweak generation.
        public float EdgeTolerance { get; set; } = 45f;
        public int MaxGenerationAttempts { get; set; } = 20;
        public float WeedChance { get; set; } = 0.4f;
        public float RockChance { get; set; } = 0.03f;
        public float PebbleChance { get; set; } = 0.18f;
        public float BunnyChance { get; set; } = 0.015f;
        public float SnailChance { get; set; } = 0.015f;
        //  Constructor
        /// <summary>
        /// Sets up the generator with a tile size and an optional random seed. Passing a seed makes
        /// the output reproducible, which is handy for debugging a specific map layout.
        /// </summary>
        public LevelGenerator(int tileSize = 32, int? seed = null)
        {
            TileSize = tileSize;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _terrainTiles = new List<Rectangle>();
            _decorationTiles = new List<Rectangle>();
            _animatedDecorations = new List<AnimatedDecoration>();
        }

        /// <summary>
        /// Takes the terrain, decoration, and village textures and slices each one into individual
        /// tile rectangles that the rest of the generator uses. Also calls BuildCompatibility
        /// straight away so the WFC solver has its compatibility data ready before GenerateMap
        /// is ever called.
        /// </summary>
        public void LoadSpriteSheets(Texture2D terrainSheet, Texture2D decorationsSheet, Texture2D villageSheet, int terrainUsableRows = 5)
        {
            _terrainSheet = terrainSheet;
            _decorationsSheet = decorationsSheet;
            _villageSheet = villageSheet;

            _terrainTiles.Clear();
            _terrainColumns = terrainSheet.Width / TileSize;
            _terrainRows = Math.Min(terrainUsableRows, terrainSheet.Height / TileSize);
            for (int y = 0; y < _terrainRows; y++)
            {
                for (int x = 0; x < _terrainColumns; x++)
                {
                    _terrainTiles.Add(new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
            }

            _decorationTiles.Clear();
            _decorationColumns = decorationsSheet.Width / TileSize;
            _decorationRows = decorationsSheet.Height / TileSize;
            for (int y = 0; y < _decorationRows; y++)
            {
                for (int x = 0; x < _decorationColumns; x++)
                {
                    _decorationTiles.Add(new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
            }

            _weights = null;
            BuildCompatibility();
        }

        /// <summary>
        /// Top-level entry point that builds a complete level. Runs WFC on grass tiles only, falls
        /// back to random fill if every attempt fails, then places the village, stamps a flower field
        /// somewhere on the grass, and scatters decorations across the overlay layer on top.
        /// </summary>
        public void GenerateMap(int pixelWidth, int pixelHeight)
        {
            if (_terrainTiles.Count == 0)
                throw new InvalidOperationException("Call LoadSpriteSheets before generating a map.");

            if (_compatibility == null)
                BuildCompatibility();

            int widthInTiles = (pixelWidth + TileSize - 1) / TileSize;
            int heightInTiles = (pixelHeight + TileSize - 1) / TileSize;
            _map = new int[widthInTiles, heightInTiles];
            _overlayMap = new int[widthInTiles, heightInTiles];

            for (int y = 0; y < heightInTiles; y++)
            {
                for (int x = 0; x < widthInTiles; x++)
                {
                    _overlayMap[x, y] = -1;
                }
            }

            // WFC only places grass tiles — stone is reserved for walkways.
            List<int> grassTiles = GetTerrainTileIndicesForRows(0, 2);
            HashSet<int> grassSet = new HashSet<int>(grassTiles);

            if (!TryGenerateWfc(_map, grassSet))
            {
                FillRandomFallback(_map, grassTiles);
            }

            _animatedDecorations.Clear();
            _villageBounds = Rectangle.Empty;
            if (_villageSheet != null)
            {
                GenerateVillage();
            }
            ApplyFlowerField(_map);
            ApplyDecorations(_map, _overlayMap);
        }
        /// <summary>
        /// Draws the whole level each frame. Terrain tiles go first, then any static overlay
        /// decorations on top of them, then animated critters (their animation gets updated here
        /// too), and finally the village buildings drawn last so they appear above everything else.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            if (_terrainSheet == null || _decorationsSheet == null || _map == null)
                return;

            for (int y = 0; y < _map.GetLength(1); y++)
            {
                for (int x = 0; x < _map.GetLength(0); x++)
                {
                    int tileIndex = _map[x, y];
                    Rectangle source = _terrainTiles[tileIndex];
                    Vector2 position = origin + new Vector2(x * TileSize, y * TileSize);

                    spriteBatch.Draw(_terrainSheet, position, source, Color.White);

                    if (_overlayMap != null && _overlayMap[x, y] >= 0)
                    {
                        Rectangle overlaySource = _decorationTiles[_overlayMap[x, y]];
                        spriteBatch.Draw(_decorationsSheet, position, overlaySource, Color.White);
                    }
                }
            }

            foreach (AnimatedDecoration decoration in _animatedDecorations)
            {
                decoration.Animation.Update();
                Vector2 position = origin + new Vector2(decoration.TileX * TileSize, decoration.TileY * TileSize);
                spriteBatch.Draw(_decorationsSheet, position, decoration.Animation.GetSourceRect(), Color.White);
            }
            DrawVillage(spriteBatch, origin);
        }
    }
}
