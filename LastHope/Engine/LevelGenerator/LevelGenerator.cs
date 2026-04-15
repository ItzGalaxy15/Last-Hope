using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        // Direction constants – shared by WFC propagation and edge comparison.
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

        // ── Public properties ────────────────────────────────────────
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
        public float DecorationChance
        {
            get => WeedChance;
            set => WeedChance = value;
        }

        // ── Constructor ──────────────────────────────────────────────
        public LevelGenerator(int tileSize = 32, int? seed = null)
        {
            TileSize = tileSize;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _terrainTiles = new List<Rectangle>();
            _decorationTiles = new List<Rectangle>();
            _animatedDecorations = new List<AnimatedDecoration>();
        }

        // ── Sprite-sheet loading ─────────────────────────────────────
        // Slices both sheets into TileSize × TileSize source rectangles.
        // The terrain sheet feeds the WFC solver (and drives the
        // compatibility table), the decorations sheet feeds the overlay
        // layer — weed, rocks, pebbles, snails, bunnies.
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

        // ── Tile weights ─────────────────────────────────────────────
        // Optional per-terrain-tile weights so some tiles show up more
        // often during WFC selection and the random fallback.
        public void SetTileWeights(IReadOnlyList<float> weights)
        {
            if (_terrainTiles.Count == 0)
                throw new InvalidOperationException("Call LoadSpriteSheets before setting weights.");

            if (weights.Count != _terrainTiles.Count)
                throw new ArgumentException($"Expected {_terrainTiles.Count} weights, got {weights.Count}.", nameof(weights));

            _weights = new float[weights.Count];
            for (int i = 0; i < weights.Count; i++)
            {
                _weights[i] = Math.Max(0f, weights[i]);
            }
        }

        // ── Map generation ───────────────────────────────────────────
        // 1. Restricts WFC to grass tiles only (rows 1 & 3) so stone
        //    never appears as a base tile.
        // 2. Attempts to fill the tile map with the WFC solver.
        // 3. Falls back to random grass placement on failure.
        // 4. Carves stone walkways on top of the WFC output.
        // 5. Stamps a single flower-field square somewhere on the grass.
        // 6. Scatters decorations (weed, rocks, pebbles, critters) from
        //    the decorations sheet onto the overlay layer.
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
            List<int> grassTiles = GetTerrainTileIndicesForRowsOneBased(1, 3);
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

        // ── Drawing ──────────────────────────────────────────────────
        // Renders the base tile layer (terrain sheet), then the overlay
        // layer (decorations sheet), and finally any animated decorations
        // on top (also decorations sheet).
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
