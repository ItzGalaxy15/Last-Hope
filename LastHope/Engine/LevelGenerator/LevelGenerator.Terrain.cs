using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {

        /// <summary>
        /// Picks a random rectangle of grass tiles and replaces them all with flower tiles to give
        /// the map a bit of variety. It tries up to 100 times to find a spot that's entirely plain
        /// grass. If it can't find one it just skips the flower field for this map rather than
        /// forcing it somewhere it doesn't fit.
        /// </summary>
        private void ApplyFlowerField(int[,] map)
        {
            List<int> flowerTiles = GetTerrainTileIndicesForRows(4);
            if (flowerTiles.Count == 0)
                return;

            HashSet<int> grassSet = new HashSet<int>(GetTerrainTileIndicesForRows(0, 2));
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

    }
}
