using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        //1. All tiles available → TryGenerateWfc (the setup at the top where it fills everything with true)
        //2. Find lowest entropy → TryFindLowestEntropyCell
        //3. Check weights + pick a tile → SelectTileFromCell
        //4. Lock it in → CollapseCellToTile
        //5. Ripple outward + remove incompatible → Propagate which uses GetNeighbor to find the 4 neighbours
        //6. Repeat → the while(true) loop back in TryGenerateWfc
        //7. If contradiction, restart → the retry loop in TryGenerateWfc
        //8. If all retries fail → FillRandomFallback
        private bool[,,]? _compatibility;

        // ── Build compatibility table ────────────────────────────────
        // For every pair of tiles and every direction, compare the
        // pixels along the shared edge. If the average colour
        // difference is within EdgeTolerance the pair is marked
        // compatible.  Each tile is guaranteed at least 4 neighbours
        // per direction so the solver never gets stuck immediately.
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

                    // If too few neighbours passed the tolerance, force-
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

        // ── WFC solver ───────────────────────────────────────────────
        // allowedTiles restricts which tiles WFC can place.
        // Passing only grass tiles here ensures stone never appears as a base tile.
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

        // ── Entropy selection ────────────────────────────────────────
        // Scans for the cell with the smallest number of remaining
        // options (> 1).  Ties are broken via reservoir sampling so
        // the result is uniformly random among equals.
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

        // ── Tile selection ───────────────────────────────────────────
        // Picks one tile from the remaining options in a cell, using
        // the optional weight table to bias the choice.
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

        // ── Cell collapse ────────────────────────────────────────────
        // Sets a cell to exactly one tile, removing all other options.
        private void CollapseCellToTile(bool[,,] possible, int[,] optionCount, int x, int y, int tile, int tileCount)
        {
            for (int t = 0; t < tileCount; t++)
                possible[x, y, t] = t == tile;

            optionCount[x, y] = 1;
        }

        // ── Constraint propagation ───────────────────────────────────
        // BFS outward from the collapsed cell.  For each neighbour,
        // remove any tile that is no longer supported by the current
        // cell's remaining options.  If a neighbour changes, enqueue
        // it so its own neighbours get checked too.
        // Returns false if any cell ends up with zero options
        // (contradiction).
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

        // ── Neighbour lookup ─────────────────────────────────────────
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

        // ── Random fallback ──────────────────────────────────────────
        // Used when WFC fails after all retry attempts – fill every
        // cell with a random tile from the allowed set.
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

        // ── Edge difference scoring ──────────────────────────────────
        // Compares a row (or column) of pixels along the touching edge
        // of two tiles.  Returns the average per-channel difference.
        // A low score means the tiles look similar along that edge and
        // can be placed next to each other.
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
