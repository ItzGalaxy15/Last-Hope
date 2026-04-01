using System;
using System.Collections.Generic;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal class LevelGenerator
    {
        private const int Up = 0;
        private const int Right = 1;
        private const int Down = 2;
        private const int Left = 3;

        private readonly Random _random;
        private readonly List<Rectangle> _sourceTiles;
        private readonly List<AnimatedDecoration> _animatedDecorations;

        private Texture2D? _spriteSheet;
        private int[,]? _map;
        private int[,]? _overlayMap;
        private float[]? _weights;
        private bool[,,]? _compatibility;
        private int _columns;
        private int _rows;

        public int TileSize { get; }
        public int MapWidthInTiles => _map?.GetLength(0) ?? 0;
        public int MapHeightInTiles => _map?.GetLength(1) ?? 0;

        public float EdgeTolerance { get; set; } = 45f;
        public int MaxGenerationAttempts { get; set; } = 20;
        public float WeedChance { get; set; } = 0.4f;
        public float RockChance { get; set; } = 0.03f;
        public float PebbleChance { get; set; } = 0.12f;
        public float BunnyChance { get; set; } = 0.008f;
        public float SnailChance { get; set; } = 0.015f;
        public float DecorationChance
        {
            get => WeedChance;
            set => WeedChance = value;
        }

        public LevelGenerator(int tileSize = 32, int? seed = null)
        {
            TileSize = tileSize;
            _random = seed.HasValue ? new Random(seed.Value) : new Random();
            _sourceTiles = new List<Rectangle>();
            _animatedDecorations = new List<AnimatedDecoration>();
        }

        public void LoadSpriteSheet(Texture2D spriteSheet, int usableRows = 4)
        {
            _spriteSheet = spriteSheet;
            _sourceTiles.Clear();

            int columns = spriteSheet.Width / TileSize;
            int rows = Math.Min(usableRows, spriteSheet.Height / TileSize);
            _columns = columns;
            _rows = rows;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    _sourceTiles.Add(new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize));
                }
            }

            _weights = null;
            BuildCompatibility();
        }

        public void SetTileWeights(IReadOnlyList<float> weights)
        {
            if (_sourceTiles.Count == 0)
                throw new InvalidOperationException("Call LoadSpriteSheet before setting weights.");

            if (weights.Count != _sourceTiles.Count)
                throw new ArgumentException($"Expected {_sourceTiles.Count} weights, got {weights.Count}.", nameof(weights));

            _weights = new float[weights.Count];
            for (int i = 0; i < weights.Count; i++)
            {
                _weights[i] = Math.Max(0f, weights[i]);
            }
        }

        public void GenerateMap(int pixelWidth, int pixelHeight)
        {
            if (_sourceTiles.Count == 0)
                throw new InvalidOperationException("Call LoadSpriteSheet before generating a map.");

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

            if (!TryGenerateWfc(_map))
            {
                FillRandomFallback(_map);
            }

            _animatedDecorations.Clear();
            ApplyGrassAndWalkways(_map);
            ApplyDecorations(_map, _overlayMap);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 origin)
        {
            if (_spriteSheet == null || _map == null)
                return;

            for (int y = 0; y < _map.GetLength(1); y++)
            {
                for (int x = 0; x < _map.GetLength(0); x++)
                {
                    int tileIndex = _map[x, y];
                    Rectangle source = _sourceTiles[tileIndex];
                    Vector2 position = origin + new Vector2(x * TileSize, y * TileSize);

                    spriteBatch.Draw(_spriteSheet, position, source, Color.White);

                    if (_overlayMap != null && _overlayMap[x, y] >= 0)
                    {
                        Rectangle overlaySource = _sourceTiles[_overlayMap[x, y]];
                        spriteBatch.Draw(_spriteSheet, position, overlaySource, Color.White);
                    }
                }
            }

            foreach (AnimatedDecoration decoration in _animatedDecorations)
            {
                decoration.Animation.Update();
                Vector2 position = origin + new Vector2(decoration.TileX * TileSize, decoration.TileY * TileSize);
                spriteBatch.Draw(_spriteSheet, position, decoration.Animation.GetSourceRect(), Color.White);
            }
        }

        private void BuildCompatibility()
        {
            if (_spriteSheet == null)
                return;

            int tileCount = _sourceTiles.Count;
            _compatibility = new bool[tileCount, 4, tileCount];

            Color[] pixels = new Color[_spriteSheet.Width * _spriteSheet.Height];
            _spriteSheet.GetData(pixels);

            int minNeighborsPerDirection = Math.Min(4, tileCount);

            for (int a = 0; a < tileCount; a++)
            {
                for (int direction = 0; direction < 4; direction++)
                {
                    float[] diffs = new float[tileCount];
                    int allowedCount = 0;

                    for (int b = 0; b < tileCount; b++)
                    {
                        float diff = GetEdgeDifference(pixels, _spriteSheet.Width, _sourceTiles[a], _sourceTiles[b], direction);
                        diffs[b] = diff;

                        if (diff <= EdgeTolerance)
                        {
                            _compatibility[a, direction, b] = true;
                            allowedCount++;
                        }
                    }

                    while (allowedCount < minNeighborsPerDirection)
                    {
                        int bestTile = -1;
                        float bestDiff = float.MaxValue;

                        for (int b = 0; b < tileCount; b++)
                        {
                            if (_compatibility[a, direction, b])
                                continue;

                            if (diffs[b] < bestDiff)
                            {
                                bestDiff = diffs[b];
                                bestTile = b;
                            }
                        }

                        if (bestTile < 0)
                            break;

                        _compatibility[a, direction, bestTile] = true;
                        allowedCount++;
                    }
                }
            }
        }

        private bool TryGenerateWfc(int[,] outputMap)
        {
            if (_compatibility == null)
                return false;

            int width = outputMap.GetLength(0);
            int height = outputMap.GetLength(1);
            int tileCount = _sourceTiles.Count;

            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                bool[,,] possible = new bool[width, height, tileCount];
                int[,] optionCount = new int[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        optionCount[x, y] = tileCount;
                        for (int t = 0; t < tileCount; t++)
                            possible[x, y, t] = true;
                    }
                }

                bool failed = false;

                while (true)
                {
                    if (!TryFindLowestEntropyCell(optionCount, out int cellX, out int cellY))
                    {
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                outputMap[x, y] = GetFirstPossibleTile(possible, x, y, tileCount);
                            }
                        }

                        return true;
                    }

                    int selectedTile = SelectTileFromCell(possible, optionCount, cellX, cellY, tileCount);
                    CollapseCellToTile(possible, optionCount, cellX, cellY, selectedTile, tileCount);

                    if (!Propagate(possible, optionCount, cellX, cellY, width, height, tileCount))
                    {
                        failed = true;
                        break;
                    }
                }

                if (!failed)
                    return true;
            }

            return false;
        }

        private bool TryFindLowestEntropyCell(int[,] optionCount, out int resultX, out int resultY)
        {
            int width = optionCount.GetLength(0);
            int height = optionCount.GetLength(1);

            int bestCount = int.MaxValue;
            int seen = 0;
            resultX = -1;
            resultY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int count = optionCount[x, y];
                    if (count <= 1)
                        continue;

                    if (count < bestCount)
                    {
                        bestCount = count;
                        resultX = x;
                        resultY = y;
                        seen = 1;
                    }
                    else if (count == bestCount)
                    {
                        seen++;
                        if (_random.Next(seen) == 0)
                        {
                            resultX = x;
                            resultY = y;
                        }
                    }
                }
            }

            return resultX >= 0;
        }

        private int SelectTileFromCell(bool[,,] possible, int[,] optionCount, int x, int y, int tileCount)
        {
            float totalWeight = 0f;
            for (int t = 0; t < tileCount; t++)
            {
                if (!possible[x, y, t])
                    continue;

                totalWeight += GetWeight(t);
            }

            if (totalWeight <= 0f)
            {
                for (int t = 0; t < tileCount; t++)
                {
                    if (possible[x, y, t])
                        return t;
                }

                return 0;
            }

            float roll = _random.NextSingle() * totalWeight;
            float cumulative = 0f;

            for (int t = 0; t < tileCount; t++)
            {
                if (!possible[x, y, t])
                    continue;

                cumulative += GetWeight(t);
                if (roll <= cumulative)
                    return t;
            }

            for (int t = tileCount - 1; t >= 0; t--)
            {
                if (possible[x, y, t])
                    return t;
            }

            return 0;
        }

        private void CollapseCellToTile(bool[,,] possible, int[,] optionCount, int x, int y, int tile, int tileCount)
        {
            for (int t = 0; t < tileCount; t++)
                possible[x, y, t] = t == tile;

            optionCount[x, y] = 1;
        }

        private bool Propagate(bool[,,] possible, int[,] optionCount, int startX, int startY, int width, int height, int tileCount)
        {
            if (_compatibility == null)
                return false;

            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));

            while (queue.Count > 0)
            {
                Point cell = queue.Dequeue();

                for (int direction = 0; direction < 4; direction++)
                {
                    GetNeighbor(cell.X, cell.Y, direction, out int nx, out int ny);

                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                        continue;

                    bool changed = false;
                    int remaining = optionCount[nx, ny];

                    for (int neighborTile = 0; neighborTile < tileCount; neighborTile++)
                    {
                        if (!possible[nx, ny, neighborTile])
                            continue;

                        bool supported = false;
                        for (int currentTile = 0; currentTile < tileCount; currentTile++)
                        {
                            if (!possible[cell.X, cell.Y, currentTile])
                                continue;

                            if (_compatibility[currentTile, direction, neighborTile])
                            {
                                supported = true;
                                break;
                            }
                        }

                        if (!supported)
                        {
                            possible[nx, ny, neighborTile] = false;
                            remaining--;
                            changed = true;
                        }
                    }

                    if (remaining <= 0)
                        return false;

                    if (changed)
                    {
                        optionCount[nx, ny] = remaining;
                        queue.Enqueue(new Point(nx, ny));
                    }
                }
            }

            return true;
        }

        private static void GetNeighbor(int x, int y, int direction, out int nx, out int ny)
        {
            nx = x;
            ny = y;

            switch (direction)
            {
                case Up:
                    ny--;
                    break;
                case Right:
                    nx++;
                    break;
                case Down:
                    ny++;
                    break;
                case Left:
                    nx--;
                    break;
            }
        }

        private void FillRandomFallback(int[,] map)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    map[x, y] = PickWeightedRandomIndex();
                }
            }
        }

        private void ApplyGrassAndWalkways(int[,] map)
        {
            List<int> grassTiles = GetTileIndicesForRowsOneBased(1, 3);
            List<int> stoneTiles = GetTileIndicesForRowsOneBased(2);

            if (grassTiles.Count == 0)
                return;

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    map[x, y] = grassTiles[_random.Next(grassTiles.Count)];
                }
            }

            if (stoneTiles.Count == 0)
                return;

            int horizontalPaths = Math.Max(2, height / 18);
            int verticalPaths = Math.Max(2, width / 24);

            for (int i = 0; i < horizontalPaths; i++)
            {
                int y = _random.Next(height);
                DrawHorizontalPath(map, y, stoneTiles);
            }

            for (int i = 0; i < verticalPaths; i++)
            {
                int x = _random.Next(width);
                DrawVerticalPath(map, x, stoneTiles);
            }
        }

        private void ApplyDecorations(int[,] baseMap, int[,] overlayMap)
        {
            List<int> grassTiles = GetTileIndicesForRowsOneBased(1, 3);
            List<int> stoneTiles = GetTileIndicesForRowsOneBased(2);

            int weedTile = GetTileIndexOneBased(5, 1);
            int rockTile = GetTileIndexOneBased(5, 2);
            int pebble1 = GetTileIndexOneBased(5, 3);
            int pebble2 = GetTileIndexOneBased(5, 4);

            List<(int firstFrame, int lastFrame)> bunnyRanges = new List<(int firstFrame, int lastFrame)>();
            AddValidAnimationRange(bunnyRanges, GetTileIndexOneBased(6, 5), GetTileIndexOneBased(6, 8));
            AddValidAnimationRange(bunnyRanges, GetTileIndexOneBased(7, 5), GetTileIndexOneBased(7, 8));

            List<(int firstFrame, int lastFrame)> snailRanges = new List<(int firstFrame, int lastFrame)>();
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(6, 1), GetTileIndexOneBased(6, 4));
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(7, 1), GetTileIndexOneBased(7, 4));
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(8, 1), GetTileIndexOneBased(8, 4));
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(9, 1), GetTileIndexOneBased(9, 4));

            List<int> pebbleTiles = new List<int>();
            if (pebble1 >= 0)
                pebbleTiles.Add(pebble1);
            if (pebble2 >= 0)
                pebbleTiles.Add(pebble2);

            bool hasWeed = weedTile >= 0;
            bool hasRock = rockTile >= 0;
            bool hasPebbles = pebbleTiles.Count > 0;
            bool hasBunnies = bunnyRanges.Count > 0;
            bool hasSnails = snailRanges.Count > 0;

            if ((!hasWeed && !hasRock && !hasPebbles && !hasBunnies && !hasSnails) || grassTiles.Count == 0)
                return;

            HashSet<int> grassSet = new HashSet<int>(grassTiles);
            HashSet<int> stoneSet = new HashSet<int>(stoneTiles);
            HashSet<int> usedSnailStartFrames = new HashSet<int>();

            int width = baseMap.GetLength(0);
            int height = baseMap.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!grassSet.Contains(baseMap[x, y]))
                        continue;

                    if (stoneSet.Contains(baseMap[x, y]))
                        continue;

                    if (HasNearbyDecoration(overlayMap, x, y))
                        continue;

                    if (hasBunnies && _random.NextSingle() <= BunnyChance && TryAddAnimatedDecoration(bunnyRanges, x, y))
                    {
                        overlayMap[x, y] = -2;
                        continue;
                    }

                    if (hasSnails && _random.NextSingle() <= SnailChance && TryAddUniqueAnimatedDecoration(snailRanges, usedSnailStartFrames, x, y))
                    {
                        overlayMap[x, y] = -2;
                        continue;
                    }

                    float totalChance = 0f;
                    if (hasRock)
                        totalChance += RockChance;
                    if (hasPebbles)
                        totalChance += PebbleChance;
                    if (hasWeed)
                        totalChance += WeedChance;

                    if (totalChance <= 0f)
                        continue;

                    float roll = _random.NextSingle();
                    float cappedTotalChance = Math.Min(1f, totalChance);
                    if (roll > cappedTotalChance)
                        continue;

                    float threshold = 0f;

                    if (hasRock)
                    {
                        threshold += RockChance;
                        if (roll <= threshold)
                        {
                            overlayMap[x, y] = rockTile;
                            continue;
                        }
                    }

                    if (hasPebbles)
                    {
                        threshold += PebbleChance;
                        if (roll <= threshold)
                        {
                            overlayMap[x, y] = pebbleTiles[_random.Next(pebbleTiles.Count)];
                            continue;
                        }
                    }

                    if (hasWeed)
                    {
                        threshold += WeedChance;
                        if (roll <= threshold)
                        {
                            overlayMap[x, y] = weedTile;
                        }
                    }
                }
            }
        }

        private bool TryAddAnimatedDecoration(List<(int firstFrame, int lastFrame)> ranges, int x, int y)
        {
            if (ranges.Count == 0 || _columns <= 0)
                return false;

            (int firstFrame, int lastFrame) range = ranges[_random.Next(ranges.Count)];
            int frameCount = (range.lastFrame - range.firstFrame) + 1;
            if (frameCount <= 0)
                return false;

            int startRow = range.firstFrame / _columns;
            int startColumn = range.firstFrame % _columns;

            int interval = _random.Next(10, 20);
            AnimationManager animation = new AnimationManager(
                frameCount,
                _columns,
                new Vector2(TileSize, TileSize),
                interval,
                true,
                startColumn * TileSize,
                startRow * TileSize);

            _animatedDecorations.Add(new AnimatedDecoration(x, y, animation));
            return true;
        }

        private bool TryAddUniqueAnimatedDecoration(List<(int firstFrame, int lastFrame)> ranges, HashSet<int> usedStartFrames, int x, int y)
        {
            List<(int firstFrame, int lastFrame)> availableRanges = new List<(int firstFrame, int lastFrame)>();
            for (int i = 0; i < ranges.Count; i++)
            {
                if (!usedStartFrames.Contains(ranges[i].firstFrame))
                    availableRanges.Add(ranges[i]);
            }

            if (availableRanges.Count == 0 || _columns <= 0)
                return false;

            (int firstFrame, int lastFrame) range = availableRanges[_random.Next(availableRanges.Count)];
            int frameCount = (range.lastFrame - range.firstFrame) + 1;
            if (frameCount <= 0)
                return false;

            int startRow = range.firstFrame / _columns;
            int startColumn = range.firstFrame % _columns;

            int interval = _random.Next(10, 20);
            AnimationManager animation = new AnimationManager(
                frameCount,
                _columns,
                new Vector2(TileSize, TileSize),
                interval,
                true,
                startColumn * TileSize,
                startRow * TileSize);

            _animatedDecorations.Add(new AnimatedDecoration(x, y, animation));
            usedStartFrames.Add(range.firstFrame);
            return true;
        }

        private static void AddValidAnimationRange(List<(int firstFrame, int lastFrame)> ranges, int firstFrame, int lastFrame)
        {
            if (firstFrame >= 0 && lastFrame >= firstFrame)
                ranges.Add((firstFrame, lastFrame));
        }

        private int GetTileIndexOneBased(int row, int column)
        {
            if (_columns <= 0 || _rows <= 0)
                return -1;

            int zeroBasedRow = row - 1;
            int zeroBasedColumn = column - 1;

            if (zeroBasedRow < 0 || zeroBasedRow >= _rows)
                return -1;

            if (zeroBasedColumn < 0 || zeroBasedColumn >= _columns)
                return -1;

            return (zeroBasedRow * _columns) + zeroBasedColumn;
        }

        private bool HasNearbyDecoration(int[,] overlayMap, int x, int y)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                        continue;

                    int nx = x + ox;
                    int ny = y + oy;

                    if (nx < 0 || ny < 0 || nx >= overlayMap.GetLength(0) || ny >= overlayMap.GetLength(1))
                        continue;

                    if (overlayMap[nx, ny] != -1)
                        return true;
                }
            }

            return false;
        }

        private void DrawHorizontalPath(int[,] map, int y, List<int> stoneTiles)
        {
            int width = map.GetLength(0);
            int pathThickness = _random.Next(1, 3);

            for (int x = 0; x < width; x++)
            {
                for (int t = 0; t < pathThickness; t++)
                {
                    int yy = y + t;
                    if (yy >= 0 && yy < map.GetLength(1))
                        map[x, yy] = stoneTiles[_random.Next(stoneTiles.Count)];
                }
            }
        }

        private void DrawVerticalPath(int[,] map, int x, List<int> stoneTiles)
        {
            int height = map.GetLength(1);
            int pathThickness = _random.Next(1, 3);

            for (int y = 0; y < height; y++)
            {
                for (int t = 0; t < pathThickness; t++)
                {
                    int xx = x + t;
                    if (xx >= 0 && xx < map.GetLength(0))
                        map[xx, y] = stoneTiles[_random.Next(stoneTiles.Count)];
                }
            }
        }

        private List<int> GetTileIndicesForRowsOneBased(params int[] rowNumbers)
        {
            List<int> result = new List<int>();

            if (_columns <= 0 || _rows <= 0)
                return result;

            foreach (int oneBasedRow in rowNumbers)
            {
                int row = oneBasedRow - 1;
                if (row < 0 || row >= _rows)
                    continue;

                int start = row * _columns;
                int endExclusive = start + _columns;
                for (int i = start; i < endExclusive; i++)
                    result.Add(i);
            }

            return result;
        }

        private int GetFirstPossibleTile(bool[,,] possible, int x, int y, int tileCount)
        {
            for (int t = 0; t < tileCount; t++)
            {
                if (possible[x, y, t])
                    return t;
            }

            return 0;
        }

        private float GetWeight(int tileIndex)
        {
            if (_weights == null || tileIndex < 0 || tileIndex >= _weights.Length)
                return 1f;

            return _weights[tileIndex] > 0f ? _weights[tileIndex] : 0f;
        }

        private float GetEdgeDifference(Color[] pixels, int textureWidth, Rectangle a, Rectangle b, int direction)
        {
            int length = TileSize;
            float total = 0f;

            for (int i = 0; i < length; i++)
            {
                Color c1;
                Color c2;

                switch (direction)
                {
                    case Up:
                        c1 = pixels[(a.Y * textureWidth) + (a.X + i)];
                        c2 = pixels[((b.Y + TileSize - 1) * textureWidth) + (b.X + i)];
                        break;
                    case Right:
                        c1 = pixels[((a.Y + i) * textureWidth) + (a.X + TileSize - 1)];
                        c2 = pixels[((b.Y + i) * textureWidth) + b.X];
                        break;
                    case Down:
                        c1 = pixels[((a.Y + TileSize - 1) * textureWidth) + (a.X + i)];
                        c2 = pixels[(b.Y * textureWidth) + (b.X + i)];
                        break;
                    default:
                        c1 = pixels[((a.Y + i) * textureWidth) + a.X];
                        c2 = pixels[((b.Y + i) * textureWidth) + (b.X + TileSize - 1)];
                        break;
                }

                total += Math.Abs(c1.R - c2.R);
                total += Math.Abs(c1.G - c2.G);
                total += Math.Abs(c1.B - c2.B);
            }

            return total / (length * 3f);
        }

        private int PickWeightedRandomIndex()
        {
            if (_weights == null)
                return _random.Next(_sourceTiles.Count);

            float total = 0f;
            for (int i = 0; i < _weights.Length; i++)
            {
                total += _weights[i];
            }

            if (total <= 0f)
                return _random.Next(_sourceTiles.Count);

            float roll = _random.NextSingle() * total;
            float cumulative = 0f;

            for (int i = 0; i < _weights.Length; i++)
            {
                cumulative += _weights[i];
                if (roll <= cumulative)
                    return i;
            }

            return _weights.Length - 1;
        }

        private sealed class AnimatedDecoration
        {
            public int TileX { get; }
            public int TileY { get; }
            public AnimationManager Animation { get; }

            public AnimatedDecoration(int tileX, int tileY, AnimationManager animation)
            {
                TileX = tileX;
                TileY = tileY;
                Animation = animation;
            }
        }
    }
}
