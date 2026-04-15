using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
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

    }
}
