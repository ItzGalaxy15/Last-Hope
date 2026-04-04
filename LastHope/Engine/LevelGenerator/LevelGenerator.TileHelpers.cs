using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
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

    }
}
