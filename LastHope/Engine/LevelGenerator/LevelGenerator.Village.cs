using System;
using System.Collections.Generic;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        // ── House sprite settings ────────────────────────────────

        private const int HouseSourceSize = 64;
        private const int HouseRenderScale = 4;

        private const int HouseRenderPx = HouseSourceSize * HouseRenderScale;

        private int HouseRenderTilesWide => HouseRenderPx / TileSize;
        private int HouseRenderTilesTall => HouseRenderPx / TileSize;

        private const int BuildingGapTiles = 4;
        private const int StreetWidthTiles = 4;

        private const int BuildingsPerRow = 3;
        private const int Rows = 2;

        // ── Village state ────────────────────────────────────────

        private Texture2D? _villageSheet;
        private readonly List<VillageBuilding> _villageBuildings = new();

        private int VillageHouseCount =>
            _villageSheet == null
                ? 0
                : Math.Min(3, _villageSheet.Width / HouseSourceSize);

        // ── Generation ───────────────────────────────────────────

        private void GenerateVillage()
        {
            Vector2 playerPosition = new Vector2(
                MapWidthInTiles * TileSize / 2f,
                MapHeightInTiles * TileSize / 2f
            );
            if (_villageSheet == null || _map == null)
                return;

            _villageBuildings.Clear();

            int mapW = _map.GetLength(0);
            int mapH = _map.GetLength(1);

            int playerTileX = (int)(playerPosition.X / TileSize);
            int playerTileY = (int)(playerPosition.Y / TileSize);

            int houseW = HouseRenderTilesWide;
            int houseH = HouseRenderTilesTall;

            int clusterW = BuildingsPerRow * houseW + (BuildingsPerRow - 1) * BuildingGapTiles;
            int clusterH = Rows * houseH + StreetWidthTiles;

            // ── BUILD VILLAGE AROUND PLAYER ─────────────────────────

            int originX = playerTileX - clusterW / 2;

            // street is exactly where player is
            int streetY = playerTileY;

            int originY = streetY - houseH;

            // clamp to map bounds
            originX = Math.Max(2, Math.Min(originX, mapW - clusterW - 2));
            originY = Math.Max(2, Math.Min(originY, mapH - clusterH - 2));

            // ── BUILD HOUSES ───────────────────────────────────────

            for (int row = 0; row < Rows; row++)
            {
                int tileY = originY + row * (houseH + StreetWidthTiles);

                for (int col = 0; col < BuildingsPerRow; col++)
                {
                    int tileX = originX + col * (houseW + BuildingGapTiles);

                    int houseType =
                        (row * BuildingsPerRow + col) % Math.Max(1, VillageHouseCount);

                    _villageBuildings.Add(
                        new VillageBuilding(tileX, tileY, houseType)
                    );
                }
            }

            ApplyVillageWalkways(originX, streetY, clusterW);
        }

        // ── Drawing ──────────────────────────────────────────────

        private void DrawVillage(SpriteBatch spriteBatch, Vector2 origin)
        {
            if (_villageSheet == null)
                return;

            foreach (var b in _villageBuildings)
            {
                int clampedType = Math.Min(b.HouseType, VillageHouseCount - 1);

                Rectangle source = new Rectangle(
                    clampedType * HouseSourceSize,
                    0,
                    HouseSourceSize,
                    HouseSourceSize);

                Vector2 pos = origin + new Vector2(
                    b.TileX * TileSize,
                    b.TileY * TileSize);

                Rectangle dest = new Rectangle(
                    (int)pos.X,
                    (int)pos.Y,
                    HouseRenderPx,
                    HouseRenderPx);

                spriteBatch.Draw(_villageSheet, dest, source, Color.White);
            }
        }

        // ── PATHS ───────────────────────────────────────────────

        private void ApplyVillageWalkways(int clusterOriginX, int streetY, int clusterW)
        {
            if (_map == null)
                return;

            List<int> stoneTiles = GetTerrainTileIndicesForRowsOneBased(2, 4);
            if (stoneTiles.Count == 0)
                return;

            int mapW = _map.GetLength(0);
            int houseH = HouseRenderTilesTall;

            int streetStartY = streetY;
            int streetEndY = streetY + StreetWidthTiles - 1;

            for (int y = streetStartY; y <= streetEndY; y++)
            {
                for (int x = 0; x < mapW; x++)
                    SetStoneTile(_map, x, y, stoneTiles);
            }

            foreach (var b in _villageBuildings)
            {
                int houseW = HouseRenderTilesWide;
                int doorX = b.TileX + houseW / 2;

                bool topRow = b.TileY < streetY;

                if (topRow)
                {
                    int bottom = b.TileY + houseH;

                    for (int y = bottom; y < streetStartY; y++)
                    {
                        SetStoneTile(_map, doorX, y, stoneTiles);
                        SetStoneTile(_map, doorX + 1, y, stoneTiles);
                    }
                }
                else
                {
                    int top = b.TileY;

                    for (int y = streetEndY; y < top; y++)
                    {
                        SetStoneTile(_map, doorX, y, stoneTiles);
                        SetStoneTile(_map, doorX + 1, y, stoneTiles);
                    }
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────

        private void SetStoneTile(int[,] map, int x, int y, List<int> stoneTiles)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
                return;

            map[x, y] = stoneTiles[_random.Next(stoneTiles.Count)];
        }

        // ── Data ────────────────────────────────────────────────

        private sealed class VillageBuilding
        {
            public int TileX { get; }
            public int TileY { get; }
            public int HouseType { get; }

            public VillageBuilding(int tileX, int tileY, int houseType)
            {
                TileX = tileX;
                TileY = tileY;
                HouseType = houseType;
            }
        }

        private (int offsetY, int heightPx) GetHouseCollisionData(int houseType)
        {
            return houseType switch
            {
                // Type 2: roof starts at 20px, height is 44px visible body
                2 => (20 * HouseRenderScale, 44 * HouseRenderScale),

                // default houses
                _ => (0, HouseRenderPx)
            };
        }

        // ── Colliders ────────────────────────────────────────────

        public IReadOnlyList<RectangleCollider> GetVillageBuildingColliders()
        {
            var list = new List<RectangleCollider>();

            foreach (var b in _villageBuildings)
            {
                int width = HouseRenderPx;

                var (offsetY, height) = GetHouseCollisionData(b.HouseType);

                list.Add(new RectangleCollider(new Rectangle(
                    b.TileX * TileSize,
                    b.TileY * TileSize + offsetY,
                    width,
                    height
                )));
            }

            return list;
        }
    }
}