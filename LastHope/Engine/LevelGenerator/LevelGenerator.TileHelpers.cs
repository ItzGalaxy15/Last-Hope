using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
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
    }
}
