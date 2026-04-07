using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine.Pathfinding;

/// <summary>
/// Tile-based navigation: walkability per cell, world/tile conversion, and Dijkstra paths.
/// Mark cells non-walkable when you add blocking collision (e.g. <see cref="SetWalkable"/>).
/// </summary>
public sealed class NavigationGrid
{
    private readonly bool[,] _walkable;

    public NavigationGrid(int widthInTiles, int heightInTiles, int tileSize)
    {
        if (widthInTiles <= 0 || heightInTiles <= 0)
            throw new ArgumentOutOfRangeException("Map dimensions must be positive.");
        if (tileSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileSize));

        WidthInTiles = widthInTiles;
        HeightInTiles = heightInTiles;
        TileSize = tileSize;
        _walkable = new bool[widthInTiles, heightInTiles];
        for (int y = 0; y < heightInTiles; y++)
        {
            for (int x = 0; x < widthInTiles; x++)
                _walkable[x, y] = true;
        }
    }

    public int WidthInTiles { get; }
    public int HeightInTiles { get; }
    public int TileSize { get; }

    public bool IsWalkable(int tileX, int tileY) =>
        tileX >= 0 && tileX < WidthInTiles && tileY >= 0 && tileY < HeightInTiles && _walkable[tileX, tileY];

    public void SetWalkable(int tileX, int tileY, bool walkable)
    {
        if (tileX < 0 || tileX >= WidthInTiles || tileY < 0 || tileY >= HeightInTiles)
            return;
        _walkable[tileX, tileY] = walkable;
    }

    public Point WorldToTile(Vector2 worldPosition)
    {
        int tx = (int)Math.Floor(worldPosition.X / TileSize);
        int ty = (int)Math.Floor(worldPosition.Y / TileSize);
        return new Point(
            Math.Clamp(tx, 0, WidthInTiles - 1),
            Math.Clamp(ty, 0, HeightInTiles - 1));
    }

    public Vector2 TileCenterToWorld(Point tile) =>
        new Vector2(tile.X * TileSize + TileSize * 0.5f, tile.Y * TileSize + TileSize * 0.5f);

    /// <summary>
    /// Computes a direction toward the next step on a Dijkstra path from <paramref name="fromWorld"/> to <paramref name="toWorld"/>.
    /// Falls back to straight-line behavior if either endpoint has no walkable tile or no path exists.
    /// </summary>
    public bool TryGetMoveDirection(Vector2 fromWorld, Vector2 toWorld, out Vector2 direction)
    {
        direction = Vector2.Zero;
        Point startTile = WorldToTile(fromWorld);
        Point goalTile = WorldToTile(toWorld);

        if (!IsWalkable(startTile.X, startTile.Y) || !IsWalkable(goalTile.X, goalTile.Y))
        {
            Vector2 delta = toWorld - fromWorld;
            if (delta != Vector2.Zero)
            {
                direction = Vector2.Normalize(delta);
                return true;
            }
            return false;
        }

        List<Point>? tilePath = DijkstraPathfinder.FindPath(
            WidthInTiles,
            HeightInTiles,
            (x, y) => _walkable[x, y],
            startTile,
            goalTile);

        if (tilePath == null || tilePath.Count < 2)
        {
            Vector2 delta = toWorld - fromWorld;
            if (delta != Vector2.Zero)
            {
                direction = Vector2.Normalize(delta);
                return true;
            }
            return false;
        }

        // First hop after the tile we're currently in (same as start): steer toward its center.
        Point nextTile = tilePath[1];
        Vector2 nextCenter = TileCenterToWorld(nextTile);
        Vector2 toNext = nextCenter - fromWorld;
        if (toNext.LengthSquared() < 1f)
        {
            if (tilePath.Count > 2)
            {
                toNext = TileCenterToWorld(tilePath[2]) - fromWorld;
            }
            else
            {
                toNext = toWorld - fromWorld;
            }
        }

        if (toNext == Vector2.Zero)
            return false;

        direction = Vector2.Normalize(toNext);
        return true;
    }
}
