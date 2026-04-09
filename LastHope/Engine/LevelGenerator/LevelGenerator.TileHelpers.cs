using System.Collections.Generic;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        // ── Terrain sheet helpers ────────────────────────────────────
        private int GetTerrainTileIndexOneBased(int row, int column)
        {
            return GetTileIndexOneBased(row, column, _terrainColumns, _terrainRows);
        }

        private List<int> GetTerrainTileIndicesForRowsOneBased(params int[] rowNumbers)
        {
            return GetTileIndicesForRowsOneBased(_terrainColumns, _terrainRows, rowNumbers);
        }

        // ── Decoration sheet helpers ─────────────────────────────────
        private int GetDecorationTileIndexOneBased(int row, int column)
        {
            return GetTileIndexOneBased(row, column, _decorationColumns, _decorationRows);
        }

        private List<int> GetDecorationTileIndicesForRowsOneBased(params int[] rowNumbers)
        {
            return GetTileIndicesForRowsOneBased(_decorationColumns, _decorationRows, rowNumbers);
        }

        // ── Shared implementations ───────────────────────────────────
        private static int GetTileIndexOneBased(int row, int column, int sheetColumns, int sheetRows)
        {
            if (sheetColumns <= 0 || sheetRows <= 0)
                return -1;

            int zeroBasedRow = row - 1;
            int zeroBasedColumn = column - 1;

            if (zeroBasedRow < 0 || zeroBasedRow >= sheetRows)
                return -1;

            if (zeroBasedColumn < 0 || zeroBasedColumn >= sheetColumns)
                return -1;

            return (zeroBasedRow * sheetColumns) + zeroBasedColumn;
        }

        private static List<int> GetTileIndicesForRowsOneBased(int sheetColumns, int sheetRows, int[] rowNumbers)
        {
            List<int> result = new List<int>();

            if (sheetColumns <= 0 || sheetRows <= 0)
                return result;

            foreach (int oneBasedRow in rowNumbers)
            {
                int row = oneBasedRow - 1;
                if (row < 0 || row >= sheetRows)
                    continue;

                int start = row * sheetColumns;
                int endExclusive = start + sheetColumns;
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
