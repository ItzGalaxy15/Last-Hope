using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        private bool[,,]? _compatibility;

        /// <summary>
        /// Compares every tile against every other tile along each of their four edges, pixel by pixel.
        /// If the average colour difference is low enough I mark those two tiles as compatible in that
        /// direction, meaning WFC is allowed to place them next to each other. If a tile ends up with
        /// too few compatible neighbours I force add the closest ones so the solver doesn't get stuck
        /// right away.
        /// </summary>
        private void BuildCompatibility()
        {
            if (_terrainSheet == null)
                return;

            int tileCount = _terrainTiles.Count;
            _compatibility = new bool[tileCount, 4, tileCount];

            Color[] pixels = new Color[_terrainSheet.Width * _terrainSheet.Height];
            _terrainSheet.GetData(pixels);

            int minNeighborsPerDirection = Math.Min(4, tileCount);

            for (int a = 0; a < tileCount; a++)
            {
                for (int direction = 0; direction < 4; direction++)
                {
                    float[] diffs = new float[tileCount];
                    int allowedCount = 0;

                    for (int b = 0; b < tileCount; b++)
                    {
                        float diff = GetEdgeDifference(pixels, _terrainSheet.Width, _terrainTiles[a], _terrainTiles[b], direction);
                        diffs[b] = diff;

                        if (diff <= EdgeTolerance)
                        {
                            _compatibility[a, direction, b] = true;
                            allowedCount++;
                        }
                    }

                    // If too few neighbours passed the tolerance, force
                    // add the closest ones so the solver has room to work.
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

        // Algorithm: Wave Function Collapse (Maxim Gumin, github.com/mxgmn/WaveFunctionCollapse)
        // Reimplemented observe collapse propagate retry structure follows his design;
        // retry loop, data structures, and helper split are my own.
        /// <summary>
        /// The main WFC loop. It repeatedly picks the undecided cell with the fewest remaining tile
        /// options, collapses it to one choice, then propagates the constraints outward to rule out
        /// now impossible options in neighbouring cells. If it hits a contradiction a cell ends up
        /// with zero valid tiles it wipes the grid and retries from scratch, up to
        /// <see cref="MaxGenerationAttempts"/> times. Returns true if a valid map was produced.
        /// </summary>
        private bool TryGenerateWfc(int[,] outputMap, HashSet<int> allowedTiles)
        {
            if (_compatibility == null)
                return false;

            int width = outputMap.GetLength(0);
            int height = outputMap.GetLength(1);
            int tileCount = _terrainTiles.Count;

            for (int attempt = 0; attempt < MaxGenerationAttempts; attempt++)
            {
                // Only mark allowed tiles as possible — everything else starts false.
                bool[,,] possible = new bool[width, height, tileCount];
                int[,] optionCount = new int[width, height];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int count = 0;
                        for (int t = 0; t < tileCount; t++)
                        {
                            bool allowed = allowedTiles.Contains(t);
                            possible[x, y, t] = allowed;
                            if (allowed) count++;
                        }
                        optionCount[x, y] = count;
                    }
                }

                bool failed = false;

                while (true)
                {
                    // Find the undecided cell with the fewest options.
                    if (!TryFindLowestEntropyCell(optionCount, out int cellX, out int cellY))
                    {
                        // No undecided cells left → success.
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                outputMap[x, y] = GetFirstPossibleTile(possible, x, y, tileCount);
                            }
                        }

                        return true;
                    }

                    // Collapse the chosen cell to a single tile.
                    int selectedTile = SelectTileFromCell(possible, optionCount, cellX, cellY, tileCount);
                    CollapseCellToTile(possible, optionCount, cellX, cellY, selectedTile, tileCount);

                    // Ripple constraints outward; if a contradiction is
                    // found (some cell has zero options) we restart.
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
        // Algorithm: minimum entropy cell selection (github.com/mxgmn/WaveFunctionCollapse)
        // Reimplemented using plain option count + reservoir sampling for ties;
        // mxgmn uses Shannon entropy with per cell Gaussian noise instead.
        /// <summary>
        /// Scans the whole grid and returns the coordinates of the undecided cell that has the fewest
        /// tiles still possible. Cells already collapsed to one option are skipped. When multiple cells
        /// share the same minimum count, ties are broken randomly using reservoir sampling so it's not
        /// always biased towards the top left corner.
        /// </summary>
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

        // Tile selection
        /// <summary>
        /// Picks one tile from however many options are still valid for this cell. Uses a weighted
        /// random roll so tiles with a higher entry in the weight table appear more often. Falls back
        /// to the first available tile if the total weight works out to zero.
        /// </summary>
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

        // Cell collapse
        /// <summary>
        /// Locks a cell to a single tile by setting every other option to false and recording that
        /// exactly one choice remains. Always called right after SelectTileFromCell.
        /// </summary>
        private void CollapseCellToTile(bool[,,] possible, int[,] optionCount, int x, int y, int tile, int tileCount)
        {
            for (int t = 0; t < tileCount; t++)
                possible[x, y, t] = t == tile;

            optionCount[x, y] = 1;
        }
        
        // Algorithm: AC-3 arc consistency propagation (BorisTheBrave/DeBroglie, Ac3PatternModelConstraint.cs)
        // Reimplemented with bool[,,] and Queue<Point>; DeBroglie uses BitArray propagators and (index, Direction) tuples.
        /// <summary>
        /// After a cell is collapsed, this ripples the change outward across the grid using a queue.
        /// Starting from the collapsed cell it visits each neighbour and removes any tile option that
        /// has no compatible partner left in the current cell. If a neighbour's options change, that
        /// neighbour gets queued so its own neighbours are checked too. Returns false the moment any
        /// cell ends up with zero options left, which means the current attempt has a contradiction
        /// and needs to restart.
        /// </summary>
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

        // Neighbour lookup
        /// <summary>
        /// Translates a direction index (0 up, 1 right, 2 down, 3 left) into the coordinates of the
        /// adjacent cell. Keeps the direction logic in one place so the propagation loop doesn't have
        /// to repeat the same switch every time it checks a neighbour.
        /// </summary>
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

        // Random fallback
        /// <summary>
        /// If WFC fails every retry attempt this just fills the whole map with random tiles from the
        /// allowed set so the game never crashes on level load. The output won't look as good as a
        /// proper WFC result, but it's a safe fallback.
        /// </summary>
        private void FillRandomFallback(int[,] map, IReadOnlyList<int> allowedTiles)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    map[x, y] = allowedTiles[_random.Next(allowedTiles.Count)];
                }
            }
        }

        // Edge difference scoring
        /// <summary>
        /// Reads the row or column of pixels along the touching edge of two tiles and returns the
        /// average per channel (R, G, B) colour difference. A low score means those edges look similar
        /// and the tiles can sit next to each other without a visible seam. This score is what
        /// BuildCompatibility uses to decide whether to allow a tile pairing.
        /// </summary>
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
    }
}
