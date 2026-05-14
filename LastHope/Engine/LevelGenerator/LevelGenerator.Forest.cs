using System;
using System.Collections.Generic;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        private const int TreeRenderPx = 256; // 2x scale — 8 tiles wide/tall at 32 px/tile

        // How many px to inset the trunk collision box on each side. Increase to shrink it inward.
        private const int TreeTrunkSideInset = 20;

        private const int TreeTrunkOffsetX = TreeTrunkSideInset;
        private const int TreeTrunkWidth   = TreeRenderPx - TreeTrunkSideInset * 2;
        private const int TreeTrunkOffsetY = 192; // 256 - 64
        private const int TreeTrunkHeight  = 64;

        private const int TreeMinSpacingTiles     = 6;
        private const int ForestPlacementAttempts = 2000;
        private const int ForestBufferTiles        = 5;

        // Clear horizontal corridors so the player can always walk north/south.
        private const int ForestCorridorSpacingTiles = 10; // one corridor every N tiles in Y
        private const int ForestCorridorWidthTiles   = 3;  // each corridor is this many tiles wide

        private Texture2D[]? _treeTextures;
        private readonly List<TreePlacement> _treePlacements = new();
        private Rectangle _forestBounds;

        public void LoadForestSprites(Texture2D[] trees)
        {
            _treeTextures = trees;
        }

        private void GenerateForest()
        {
            if (_treeTextures == null || _treeTextures.Length == 0 || _map == null)
                return;

            _treePlacements.Clear();

            int mapW = _map.GetLength(0);
            int mapH = _map.GetLength(1);

            int forestRight = _villageBounds == Rectangle.Empty
                ? mapW / 3
                : Math.Max(ForestBufferTiles, _villageBounds.Left - ForestBufferTiles);

            _forestBounds = new Rectangle(0, 0, forestRight, mapH);

            int treeTiles = TreeRenderPx / TileSize; // 8

            // Trunk occupies the bottom 2 tile-rows of the sprite (offsetY=192, height=64, tileSize=32).
            int trunkRowStart = TreeTrunkOffsetY / TileSize;                          // 6
            int trunkRowEnd   = (TreeTrunkOffsetY + TreeTrunkHeight) / TileSize - 1; // 7

            HashSet<int> grassSet = new HashSet<int>(GetTerrainTileIndicesForRows(0, 2));

            var placed = new List<Point>(128);

            for (int attempt = 0; attempt < ForestPlacementAttempts; attempt++)
            {
                int tx = _random.Next(1, forestRight - treeTiles - 1);
                int ty = _random.Next(1, mapH - treeTiles - 1);

                // Skip if any tile in the trunk rows is a road/stone tile.
                bool onRoad = false;
                for (int row = trunkRowStart; row <= trunkRowEnd && !onRoad; row++)
                {
                    int checkY = ty + row;
                    if (checkY < 0 || checkY >= mapH) continue;
                    for (int col = 0; col < treeTiles && !onRoad; col++)
                    {
                        int checkX = tx + col;
                        if (checkX < 0 || checkX >= mapW) continue;
                        if (!grassSet.Contains(_map[checkX, checkY]))
                            onRoad = true;
                    }
                }
                if (onRoad)
                    continue;

                // Reject if any trunk tile row falls inside a reserved north-south corridor.
                bool inCorridor = false;
                for (int row = trunkRowStart; row <= trunkRowEnd && !inCorridor; row++)
                {
                    int modY = (ty + row) % ForestCorridorSpacingTiles;
                    if (modY <= ForestCorridorWidthTiles / 2 ||
                        modY >= ForestCorridorSpacingTiles - ForestCorridorWidthTiles / 2)
                        inCorridor = true;
                }
                if (inCorridor)
                    continue;

                bool tooClose = false;
                foreach (var p in placed)
                {
                    int dx = tx - p.X;
                    int dy = ty - p.Y;
                    if (dx * dx + dy * dy < TreeMinSpacingTiles * TreeMinSpacingTiles)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose)
                    continue;

                placed.Add(new Point(tx, ty));
                _treePlacements.Add(new TreePlacement(tx, ty, _random.Next(_treeTextures.Length)));
            }

            _treePlacements.Sort((a, b) => a.TileY.CompareTo(b.TileY));
        }

        // Pixel Y of the trunk base — used as the depth anchor for Y-sorting.
        private int TrunkBaseY(TreePlacement tree) =>
            tree.TileY * TileSize + TreeRenderPx;

        // Fills the shared Y-sort list with one entry per tree so Last_Hope.cs
        // can merge trees and entities into a single depth-sorted draw pass.
        public void AppendTreeDrawItems(Vector2 origin, List<(float sortY, Action<SpriteBatch> draw)> list)
        {
            if (_treeTextures == null || _treeTextures.Length == 0)
                return;

            foreach (var tree in _treePlacements)
            {
                var captured = tree;
                // most important. Trees are sorted by the Y of their trunk base, so the player can be drawn in front of or behind them based on their Y position.
                list.Add((TrunkBaseY(captured), sb => DrawTree(sb, origin, captured)));
            }
        }

        // Trees whose trunk base is above (north of) the player draw before the player,
        // so the player appears in front of them.
        public void DrawForestBehindPlayer(SpriteBatch spriteBatch, Vector2 origin, float playerY)
        {
            if (_treeTextures == null || _treeTextures.Length == 0)
                return;

            foreach (var tree in _treePlacements)
            {
                if (TrunkBaseY(tree) < playerY)
                    DrawTree(spriteBatch, origin, tree);
            }
        }

        // Trees whose trunk base is below (south of) the player draw after the player,
        // so they overlap the player's feet.
        public void DrawForestInFrontOfPlayer(SpriteBatch spriteBatch, Vector2 origin, float playerY)
        {
            if (_treeTextures == null || _treeTextures.Length == 0)
                return;

            foreach (var tree in _treePlacements)
            {
                if (TrunkBaseY(tree) >= playerY)
                    DrawTree(spriteBatch, origin, tree);
            }
        }

        private void DrawTree(SpriteBatch spriteBatch, Vector2 origin, TreePlacement tree)
        {
            Texture2D tex = _treeTextures![tree.Variant];
            Vector2 pos = origin + new Vector2(tree.TileX * TileSize, tree.TileY * TileSize);
            spriteBatch.Draw(tex, new Rectangle((int)pos.X, (int)pos.Y, TreeRenderPx, TreeRenderPx), Color.White);
        }

        public IReadOnlyList<RectangleCollider> GetTreeColliders()
        {
            var list = new List<RectangleCollider>(_treePlacements.Count);

            foreach (var tree in _treePlacements)
            {
                int px = tree.TileX * TileSize + TreeTrunkOffsetX;
                int py = tree.TileY * TileSize + TreeTrunkOffsetY;
                list.Add(new RectangleCollider(new Rectangle(px, py, TreeTrunkWidth, TreeTrunkHeight)));
            }

            return list;
        }

        private sealed class TreePlacement
        {
            public int TileX   { get; }
            public int TileY   { get; }
            public int Variant { get; }

            public TreePlacement(int tileX, int tileY, int variant)
            {
                TileX   = tileX;
                TileY   = tileY;
                Variant = variant;
            }
        }
    }
}
