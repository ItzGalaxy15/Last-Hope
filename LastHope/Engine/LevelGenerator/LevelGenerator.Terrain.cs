using System;
using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        private void ApplyWalkways(int[,] map)
        {
            List<int> stoneTiles = GetTileIndicesForRowsOneBased(2);

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
