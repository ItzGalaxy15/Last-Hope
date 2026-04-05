using System;
using System.Collections.Generic;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {
        private void ApplyDecorations(int[,] baseMap, int[,] overlayMap)
        {
            List<int> grassTiles = GetTileIndicesForRowsOneBased(1, 3);
            List<int> stoneTiles = GetTileIndicesForRowsOneBased(2);

            int weedTile = GetTileIndexOneBased(5, 1);
            int rockTile = GetTileIndexOneBased(5, 2);
            int pebble1 = GetTileIndexOneBased(5, 3);
            int pebble2 = GetTileIndexOneBased(5, 4);

            // Bunny animations live on rows 6-7, columns 5-8 of the sprite sheet.
            List<(int firstFrame, int lastFrame)> bunnyRanges = new List<(int firstFrame, int lastFrame)>();
            AddValidAnimationRange(bunnyRanges, GetTileIndexOneBased(6, 5), GetTileIndexOneBased(6, 8));
            AddValidAnimationRange(bunnyRanges, GetTileIndexOneBased(7, 5), GetTileIndexOneBased(7, 8));

            // Snail animations live on rows 6-9, columns 1-4.
            List<(int firstFrame, int lastFrame)> snailRanges = new List<(int firstFrame, int lastFrame)>();
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(6, 1), GetTileIndexOneBased(6, 4));
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(7, 1), GetTileIndexOneBased(7, 4));
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(8, 1), GetTileIndexOneBased(8, 4));
            AddValidAnimationRange(snailRanges, GetTileIndexOneBased(9, 1), GetTileIndexOneBased(9, 4));

            List<int> pebbleTiles = new List<int>();
            if (pebble1 >= 0)
                pebbleTiles.Add(pebble1);
            if (pebble2 >= 0)
                pebbleTiles.Add(pebble2);

            bool hasWeed = weedTile >= 0;
            bool hasRock = rockTile >= 0;
            bool hasPebbles = pebbleTiles.Count > 0;
            bool hasBunnies = bunnyRanges.Count > 0;
            bool hasSnails = snailRanges.Count > 0;

            if ((!hasWeed && !hasRock && !hasPebbles && !hasBunnies && !hasSnails) || grassTiles.Count == 0)
                return;

            HashSet<int> grassSet = new HashSet<int>(grassTiles);
            HashSet<int> stoneSet = new HashSet<int>(stoneTiles);
            HashSet<int> usedSnailStartFrames = new HashSet<int>();

            int width = baseMap.GetLength(0);
            int height = baseMap.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Only place decorations on grass, not on stone walkways.
                    if (!grassSet.Contains(baseMap[x, y]))
                        continue;

                    if (stoneSet.Contains(baseMap[x, y]))
                        continue;

                    // Enforce minimum spacing between decorations.
                    if (HasNearbyDecoration(overlayMap, x, y))
                        continue;

                    // Animated critters are checked first (rarest).
                    if (hasBunnies && _random.NextSingle() <= BunnyChance && TryAddAnimatedDecoration(bunnyRanges, x, y))
                    {
                        overlayMap[x, y] = -2;
                        continue;
                    }

                    if (hasSnails && _random.NextSingle() <= SnailChance && TryAddUniqueAnimatedDecoration(snailRanges, usedSnailStartFrames, x, y))
                    {
                        overlayMap[x, y] = -2;
                        continue;
                    }

                    // Static decorations share a combined chance roll.
                    float totalChance = 0f;
                    if (hasRock)
                        totalChance += RockChance;
                    if (hasPebbles)
                        totalChance += PebbleChance;
                    if (hasWeed)
                        totalChance += WeedChance;

                    if (totalChance <= 0f)
                        continue;

                    float roll = _random.NextSingle();
                    float cappedTotalChance = Math.Min(1f, totalChance);
                    if (roll > cappedTotalChance)
                        continue;

                    // Walk through each decoration type in priority order.
                    float threshold = 0f;

                    if (hasRock)
                    {
                        threshold += RockChance;
                        if (roll <= threshold)
                        {
                            overlayMap[x, y] = rockTile;
                            continue;
                        }
                    }

                    if (hasPebbles)
                    {
                        threshold += PebbleChance;
                        if (roll <= threshold)
                        {
                            overlayMap[x, y] = pebbleTiles[_random.Next(pebbleTiles.Count)];
                            continue;
                        }
                    }

                    if (hasWeed)
                    {
                        threshold += WeedChance;
                        if (roll <= threshold)
                        {
                            overlayMap[x, y] = weedTile;
                        }
                    }
                }
            }
        }

        // ── Animated decoration helpers ──────────────────────────────

        // Creates an AnimationManager for a randomly chosen animation
        // range and registers it so the Draw loop can render it.
        private bool TryAddAnimatedDecoration(List<(int firstFrame, int lastFrame)> ranges, int x, int y)
        {
            if (ranges.Count == 0 || _columns <= 0)
                return false;

            (int firstFrame, int lastFrame) range = ranges[_random.Next(ranges.Count)];
            int frameCount = (range.lastFrame - range.firstFrame) + 1;
            if (frameCount <= 0)
                return false;

            int startRow = range.firstFrame / _columns;
            int startColumn = range.firstFrame % _columns;

            int interval = _random.Next(10, 20);
            AnimationManager animation = new AnimationManager(
                frameCount,
                _columns,
                new Vector2(TileSize, TileSize),
                interval,
                true,
                startColumn * TileSize,
                startRow * TileSize);

            _animatedDecorations.Add(new AnimatedDecoration(x, y, animation));
            return true;
        }

        // Same as above but ensures each animation range is only used
        // once — prevents duplicate snail sprites on the map.
        private bool TryAddUniqueAnimatedDecoration(List<(int firstFrame, int lastFrame)> ranges, HashSet<int> usedStartFrames, int x, int y)
        {
            List<(int firstFrame, int lastFrame)> availableRanges = new List<(int firstFrame, int lastFrame)>();
            for (int i = 0; i < ranges.Count; i++)
            {
                if (!usedStartFrames.Contains(ranges[i].firstFrame))
                    availableRanges.Add(ranges[i]);
            }

            if (availableRanges.Count == 0 || _columns <= 0)
                return false;

            (int firstFrame, int lastFrame) range = availableRanges[_random.Next(availableRanges.Count)];
            int frameCount = (range.lastFrame - range.firstFrame) + 1;
            if (frameCount <= 0)
                return false;

            int startRow = range.firstFrame / _columns;
            int startColumn = range.firstFrame % _columns;

            int interval = _random.Next(10, 20);
            AnimationManager animation = new AnimationManager(
                frameCount,
                _columns,
                new Vector2(TileSize, TileSize),
                interval,
                true,
                startColumn * TileSize,
                startRow * TileSize);

            _animatedDecorations.Add(new AnimatedDecoration(x, y, animation));
            usedStartFrames.Add(range.firstFrame);
            return true;
        }

        private static void AddValidAnimationRange(List<(int firstFrame, int lastFrame)> ranges, int firstFrame, int lastFrame)
        {
            if (firstFrame >= 0 && lastFrame >= firstFrame)
                ranges.Add((firstFrame, lastFrame));
        }

        // ── Spacing check ────────────────────────────────────────────
        // Returns true if any of the 8 surrounding tiles already has
        // a decoration, preventing clumping.
        private bool HasNearbyDecoration(int[,] overlayMap, int x, int y)
        {
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                        continue;

                    int nx = x + ox;
                    int ny = y + oy;

                    if (nx < 0 || ny < 0 || nx >= overlayMap.GetLength(0) || ny >= overlayMap.GetLength(1))
                        continue;

                    if (overlayMap[nx, ny] != -1)
                        return true;
                }
            }

            return false;
        }

        // ── AnimatedDecoration data class ────────────────────────────
        // Pairs a tile coordinate with its animation so the Draw loop
        // can update and render it each frame.
        private sealed class AnimatedDecoration
        {
            public int TileX { get; }
            public int TileY { get; }
            public AnimationManager Animation { get; }

            public AnimatedDecoration(int tileX, int tileY, AnimationManager animation)
            {
                TileX = tileX;
                TileY = tileY;
                Animation = animation;
            }
        }
    }
}
