using System;
using System.Collections.Generic;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        // House sprite settings

        private const int HouseSourceSize = 64;
        private const int HouseRenderScale = 4;

        private const int HouseRenderPx = HouseSourceSize * HouseRenderScale;

        private int HouseRenderTilesWide => HouseRenderPx / TileSize;
        private int HouseRenderTilesTall => HouseRenderPx / TileSize;

        private const int BuildingGapTiles = 4;
        private const int StreetWidthTiles = 4;

        private const int BuildingsPerRow = 3;
        private const int Rows = 2;

        // Village state

        private Texture2D? _villageSheet;
        private readonly List<VillageBuilding> _villageBuildings = new();
        private Rectangle _villageBounds;

        public Rectangle VillageBoundsInTiles => _villageBounds;

        private int VillageHouseCount =>
            _villageSheet == null
                ? 0
                : Math.Min(3, _villageSheet.Width / HouseSourceSize);

        /// <summary>
        /// Places the village centered on the player's spawn tile. Lays out 6 buildings in a
        /// 2 row by 3 column grid with a main street running between the two rows, then calls
        /// ApplyVillageWalkways to paint the stone paths connecting everything together.
        /// </summary>
        // How far from the left edge the village sits, as a fraction of map width.
        // 0.75 = 3/4 from the left, leaving the left quarter for the forest.
        private const float VillageXFraction = 0.75f;

        public Point VillageCenterTile { get; private set; }

        private void GenerateVillage()
        {
            if (_villageSheet == null || _map == null)
                return;

            int mapW = _map.GetLength(0);
            int mapH = _map.GetLength(1);

            Vector2 playerPosition = new Vector2(
                mapW * TileSize * VillageXFraction,
                mapH * TileSize / 2f
            );

            _villageBuildings.Clear();

            int playerTileX = (int)(playerPosition.X / TileSize);
            int playerTileY = (int)(playerPosition.Y / TileSize);
            VillageCenterTile = new Point(playerTileX, playerTileY);

            int houseW = HouseRenderTilesWide;
            int houseH = HouseRenderTilesTall;

            int clusterW = BuildingsPerRow * houseW + (BuildingsPerRow - 1) * BuildingGapTiles;
            int clusterH = Rows * houseH + StreetWidthTiles;

            // BUILD VILLAGE AROUND PLAYER

            int originX = playerTileX - clusterW / 2;

            // street is exactly where player is
            int streetY = playerTileY;

            int originY = streetY - houseH;

            // clamp to map bounds
            originX = Math.Max(2, Math.Min(originX, mapW - clusterW - 2));
            originY = Math.Max(2, Math.Min(originY, mapH - clusterH - 2));

            _villageBounds = new Rectangle(originX, originY, clusterW, clusterH);

            // BUILD HOUSES

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

        // Drawing

        /// <summary>
        /// Draws every building in the village, scaling each 64 px source sprite up by a factor
        /// of 4 to match the game's tile resolution. The source rectangle selects which house type
        /// to draw from the village spritesheet.
        /// </summary>
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

        // PATHS

        /// <summary>
        /// Paints stone tiles across the full width of the map to form the main street, then draws
        /// a 2 tile wide path down from each building's door to connect it to that street. Buildings
        /// above the street get paths going down; buildings below get paths going up.
        /// </summary>
        private void ApplyVillageWalkways(int clusterOriginX, int streetY, int clusterW)
        {
            if (_map == null)
                return;

            List<int> stoneTiles = GetTerrainTileIndicesForRows(1, 3);
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

        // Helpers

        /// <summary>
        /// Bounds checks the coordinates and then replaces that map tile with a random stone tile
        /// picked from the stone set. The randomness stops all walkways looking identical.
        /// </summary>
        private void SetStoneTile(int[,] map, int x, int y, List<int> stoneTiles)
        {
            if (x < 0 || y < 0 || x >= map.GetLength(0) || y >= map.GetLength(1))
                return;

            map[x, y] = stoneTiles[_random.Next(stoneTiles.Count)];
        }

        // Data

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

        // Logic: Manual Hitbox Adjustments.
        // Note: Houses use a Y offset to allow the player to walk "behind" the roof/top of the building
        // (pseudo depth/Z order) while colliding with the walls.
        private static readonly Dictionary<int, (int OffsetY, int HeightPx)> _houseCollisionData
            = new Dictionary<int, (int, int)>
            {
                { 2, (20 * HouseRenderScale, 44 * HouseRenderScale) },
            };

        /// <summary>
        /// Returns the Y pixel offset and collision box height for a given house type. The Y offset
        /// pushes the box down past the roof graphic so the player can walk behind the top of the
        /// building before hitting the actual wall, a simple way to fake depth without a Z axis.
        /// </summary>
        private (int offsetY, int heightPx) GetHouseCollisionData(int houseType)
        {
            if (_houseCollisionData.TryGetValue(houseType, out var data))
                return data;
            return (0, HouseRenderPx);
        }

        // Colliders

        /// <summary>
        /// Builds and returns a RectangleCollider for every building in the village, applying each
        /// house type's Y offset and height from GetHouseCollisionData so the boxes line up with
        /// the visible walls rather than the full sprite bounds.
        /// </summary>
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