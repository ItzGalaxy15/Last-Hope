using System;
using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        private void ApplyWalkways(int[,] map)
        {
            // Stone walkways live on rows 2 and 4 of the terrain sheet.
            List<int> stoneTiles = GetTerrainTileIndicesForRowsOneBased(2, 4);

            if (stoneTiles.Count == 0)
                return;

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            // Carve walkways — more paths on bigger maps.
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

        // ── Flower field ─────────────────────────────────────────────
        // Stamps a single rectangle of flower tiles (row 5 of the
        // terrain sheet) somewhere on the grass. Size is randomly 4×4,
        // 4×6, or 6×4 — twice the footprint of the original 2×2 / 2×3
        // stamp. Placement is only accepted if every cell under the
        // stamp is currently grass so walkways stay intact.
        private void ApplyFlowerField(int[,] map)
        {
            List<int> flowerTiles = GetTerrainTileIndicesForRowsOneBased(5);
            if (flowerTiles.Count == 0)
                return;

            HashSet<int> grassSet = new HashSet<int>(GetTerrainTileIndicesForRowsOneBased(1, 3));
            if (grassSet.Count == 0)
                return;

            // Pick 4×4, 4×6, or 6×4.
            int shape = _random.Next(3);
            int fieldW = shape == 2 ? 12 : 6;
            int fieldH = shape == 1 ? 12 : 6;

            int width = map.GetLength(0);
            int height = map.GetLength(1);

            if (width < fieldW || height < fieldH)
                return;

            const int maxAttempts = 100;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int startX = _random.Next(width - fieldW + 1);
                int startY = _random.Next(height - fieldH + 1);

                bool allGrass = true;
                for (int dy = 0; dy < fieldH && allGrass; dy++)
                {
                    for (int dx = 0; dx < fieldW && allGrass; dx++)
                    {
                        if (!grassSet.Contains(map[startX + dx, startY + dy]))
                            allGrass = false;
                    }
                }

                if (!allGrass)
                    continue;

                for (int dy = 0; dy < fieldH; dy++)
                {
                    for (int dx = 0; dx < fieldW; dx++)
                    {
                        map[startX + dx, startY + dy] = flowerTiles[_random.Next(flowerTiles.Count)];
                    }
                }

                return;
            }
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
    }
}
