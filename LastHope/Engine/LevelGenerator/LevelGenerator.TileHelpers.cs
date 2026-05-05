using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        /// <summary>
        /// Returns all the flat tile indices that belong to the given row numbers on the terrain
        /// spritesheet. Pass rows 0 and 2 to get the grass tiles, rows 1 and 3 for the stone tiles.
        /// </summary>
        private List<int> GetTerrainTileIndicesForRows(params int[] rowNumbers)
        {
            List<int> result = new List<int>();

            if (_terrainColumns <= 0 || _terrainRows <= 0)
                return result;

            foreach (int row in rowNumbers)
            {
                if (row < 0 || row >= _terrainRows)
                    continue;

                int start = row * _terrainColumns;
                int endExclusive = start + _terrainColumns;
                for (int i = start; i < endExclusive; i++)
                    result.Add(i);
            }

            return result;
        }

        /// <summary>
        /// Converts a (row, column) position on the decoration spritesheet into a single flat index.
        /// Returns -1 if the position is out of bounds so callers can check whether the tile actually
        /// exists before trying to use it.
        /// </summary>
        private int GetDecorationTileIndex(int row, int column)
        {
            if (_decorationColumns <= 0 || _decorationRows <= 0)
                return -1;

            if (row < 0 || row >= _decorationRows)
                return -1;

            if (column < 0 || column >= _decorationColumns)
                return -1;

            return (row * _decorationColumns) + column;
        }

        /// <summary>
        /// Scans a cell's options and returns the first tile index still marked as possible.
        /// Used to read out the final tile choices once WFC has successfully collapsed every cell.
        /// </summary>
        private int GetFirstPossibleTile(bool[,,] possible, int x, int y, int tileCount)
        {
            for (int t = 0; t < tileCount; t++)
            {
                if (possible[x, y, t])
                    return t;
            }

            return 0;
        }

        /// <summary>
        /// Returns the spawn weight for a tile, defaulting to 1 if no weight table has been set.
        /// A higher weight means WFC is more likely to pick that tile when collapsing a cell.
        /// </summary>
        private float GetWeight(int tileIndex)
        {
            if (_weights == null || tileIndex < 0 || tileIndex >= _weights.Length)
                return 1f;

            return _weights[tileIndex] > 0f ? _weights[tileIndex] : 0f;
        }
    }
}
