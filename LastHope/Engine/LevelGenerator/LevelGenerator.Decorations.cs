using System;
using System.Collections.Generic;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine.LevelGenerator
{
    internal partial class LevelGenerator
    {

        /// <summary>
        /// Scans every grass tile on the map and uses weighted random rolls to decide what decoration
        /// to place there, if any. Checks the 8 surrounding tiles first to stop decorations spawning
        /// right next to each other. Animated critters (bunnies, snails) are handled separately from
        /// static props (weeds, rocks, pebbles) and are checked first since they're rarest.
        /// </summary>
        private void ApplyDecorations(int[,] baseMap, int[,] overlayMap)
        {
            // Grass is rows 1 and 3 of the terrain sheet; stone walkways
            // live on rows 2 and 4. Decorations only land on grass.
            HashSet<int> grassSet = new HashSet<int>(GetTerrainTileIndicesForRows(0, 2));

            // All decoration indices below are into the decorations sheet.
            int weedTile = GetDecorationTileIndex(0, 0);
            int rockTile = GetDecorationTileIndex(0, 1);
            int pebble1 = GetDecorationTileIndex(0, 2);
            int pebble2 = GetDecorationTileIndex(0, 3);

            // Bunny animations live on rows 2-3 cols 5-8 of decorations.png
            // (two 4-frame animations, duplicates allowed on the map).
            List<(int firstFrame, int lastFrame)> bunnyRanges = new List<(int firstFrame, int lastFrame)>();
            AddValidAnimationRange(bunnyRanges, GetDecorationTileIndex(1, 4), GetDecorationTileIndex(1, 7));
            AddValidAnimationRange(bunnyRanges, GetDecorationTileIndex(2, 4), GetDecorationTileIndex(2, 7));

            // Snail animations live on rows 2-5 cols 1-4 of decorations.png
            // (four 4-frame animations, each variant only appears once).
            List<(int firstFrame, int lastFrame)> snailRanges = new List<(int firstFrame, int lastFrame)>();
            AddValidAnimationRange(snailRanges, GetDecorationTileIndex(1, 0), GetDecorationTileIndex(1, 3));
            AddValidAnimationRange(snailRanges, GetDecorationTileIndex(2, 0), GetDecorationTileIndex(2, 3));
            AddValidAnimationRange(snailRanges, GetDecorationTileIndex(3, 0), GetDecorationTileIndex(3, 3));
            AddValidAnimationRange(snailRanges, GetDecorationTileIndex(4, 0), GetDecorationTileIndex(4, 3));

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

            if ((!hasWeed && !hasRock && !hasPebbles && !hasBunnies && !hasSnails) || grassSet.Count == 0)
                return;

            HashSet<int> usedSnailStartFrames = new HashSet<int>();

            int width = baseMap.GetLength(0);
            int height = baseMap.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Only place decorations on plain grass — flowers,
                    // stone walkways and anything else get skipped.
                    if (!grassSet.Contains(baseMap[x, y]))
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

        // Animated decoration helpers

        /// <summary>
        /// Picks a random animation range from the list and tries to register an animated decoration
        /// at the given tile. Returns false if the list is empty or the decoration sheet isn't loaded.
        /// </summary>
        private bool TryAddAnimatedDecoration(List<(int firstFrame, int lastFrame)> ranges, int x, int y)
        {
            if (ranges.Count == 0 || _decorationColumns <= 0)
                return false;

            return CreateAndRegisterAnimation(ranges[_random.Next(ranges.Count)], x, y);
        }

        // Ensures each animation range is only used once prevents
        // duplicate snail sprites on the map.
        /// <summary>
        /// Same as TryAddAnimatedDecoration but filters out any animation ranges whose start frame
        /// has already been used, so each snail variant only appears once on the whole map.
        /// </summary>
        private bool TryAddUniqueAnimatedDecoration(List<(int firstFrame, int lastFrame)> ranges, HashSet<int> usedStartFrames, int x, int y)
        {
            List<(int firstFrame, int lastFrame)> availableRanges = new List<(int firstFrame, int lastFrame)>();
            for (int i = 0; i < ranges.Count; i++)
            {
                if (!usedStartFrames.Contains(ranges[i].firstFrame))
                    availableRanges.Add(ranges[i]);
            }

            if (availableRanges.Count == 0 || _decorationColumns <= 0)
                return false;

            (int firstFrame, int lastFrame) range = availableRanges[_random.Next(availableRanges.Count)];
            if (!CreateAndRegisterAnimation(range, x, y))
                return false;

            usedStartFrames.Add(range.firstFrame);
            return true;
        }

        /// <summary>
        /// Converts a flat tile index range into the matching row and column on the decoration sheet,
        /// creates an AnimationManager with a randomised frame interval so critters don't all animate
        /// in sync, then adds the result to the animated decorations list so it gets drawn every frame.
        /// </summary>
        private bool CreateAndRegisterAnimation((int firstFrame, int lastFrame) range, int x, int y)
        {
            int frameCount = (range.lastFrame - range.firstFrame) + 1;
            if (frameCount <= 0)
                return false;

            int startRow = range.firstFrame / _decorationColumns;
            int startColumn = range.firstFrame % _decorationColumns;

            int interval = _random.Next(10, 20);
            AnimationManager animation = new AnimationManager(
                frameCount,
                _decorationColumns,
                new Vector2(TileSize, TileSize),
                interval,
                true,
                startColumn * TileSize,
                startRow * TileSize);

            _animatedDecorations.Add(new AnimatedDecoration(x, y, animation));
            return true;
        }

        /// <summary>
        /// Adds a (firstFrame, lastFrame) pair to the ranges list only if both indices are valid.
        /// Just a safety guard so bad sheet coordinates don't silently produce broken animations.
        /// </summary>
        private static void AddValidAnimationRange(List<(int firstFrame, int lastFrame)> ranges, int firstFrame, int lastFrame)
        {
            if (firstFrame >= 0 && lastFrame >= firstFrame)
                ranges.Add((firstFrame, lastFrame));
        }

        // Spacing check
        // Returns true if any of the 8 surrounding tiles already has
        // a decoration, preventing clumping.
        /// <summary>
        /// Returns true if any of the 8 tiles immediately surrounding (x, y) already has a decoration
        /// placed on it. Used to enforce a minimum gap so decorations don't end up stacked or touching.
        /// </summary>
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

        /// <summary>
        /// AnimatedDecoration data class
        /// Pairs a tile coordinate with its animation so the Draw loop
        /// can update and render it each frame.
        /// </summary>
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
