using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine.Pathfinding;

/// <summary>
/// Tile-based navigation: walkability per cell, world/tile conversion, and A* paths.
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

    // Returns true only if every tile within `radiusTiles` of (tileX, tileY) is walkable.
    private bool IsWalkableWithClearance(int tileX, int tileY, int radiusTiles)
    {
        for (int dy = -radiusTiles; dy <= radiusTiles; dy++)
        {
            for (int dx = -radiusTiles; dx <= radiusTiles; dx++)
            {
                int nx = tileX + dx;
                int ny = tileY + dy;
                if (nx < 0 || nx >= WidthInTiles || ny < 0 || ny >= HeightInTiles)
                    return false;
                if (!_walkable[nx, ny])
                    return false;
            }
        }
        return true;
    }

    // BFS outward from `seed` for the closest tile accepted by `walkable`. Returns (-1,-1) if none found within the ring budget.
    private Point FindNearestWalkable(Point seed, Func<int, int, bool> walkable)
    {
        const int MaxRings = 12;
        if (walkable(seed.X, seed.Y))
            return seed;

        var visited = new HashSet<Point> { seed };
        var frontier = new Queue<Point>();
        frontier.Enqueue(seed);
        int ring = 0;
        int countThisRing = 1;
        int countNextRing = 0;

        Span<Point> neighbors = stackalloc Point[8];
        while (frontier.Count > 0 && ring < MaxRings)
        {
            Point p = frontier.Dequeue();
            countThisRing--;

            neighbors[0] = new Point(p.X + 1, p.Y);
            neighbors[1] = new Point(p.X - 1, p.Y);
            neighbors[2] = new Point(p.X, p.Y + 1);
            neighbors[3] = new Point(p.X, p.Y - 1);
            neighbors[4] = new Point(p.X + 1, p.Y + 1);
            neighbors[5] = new Point(p.X - 1, p.Y + 1);
            neighbors[6] = new Point(p.X + 1, p.Y - 1);
            neighbors[7] = new Point(p.X - 1, p.Y - 1);

            foreach (var n in neighbors)
            {
                if (n.X < 0 || n.X >= WidthInTiles || n.Y < 0 || n.Y >= HeightInTiles)
                    continue;
                if (!visited.Add(n))
                    continue;
                if (walkable(n.X, n.Y))
                    return n;
                frontier.Enqueue(n);
                countNextRing++;
            }

            if (countThisRing == 0)
            {
                ring++;
                countThisRing = countNextRing;
                countNextRing = 0;
            }
        }

        return new Point(-1, -1);
    }

    /// <summary>
    /// Computes a direction toward the next step on an A* path from <paramref name="fromWorld"/> to <paramref name="toWorld"/>.
    /// Pass <paramref name="entityRadius"/> (half the entity's hitbox width in world pixels) so the path keeps enough
    /// clearance from walls for the entity's body — without it, large entities get stuck on building corners because
    /// the path is planned for a point, not a volume.
    /// </summary>
    public bool TryGetMoveDirection(Vector2 fromWorld, Vector2 toWorld, out Vector2 direction, float entityRadius = 0f)
    {
        direction = Vector2.Zero;

        int radiusTiles = entityRadius > 0f ? (int)Math.Ceiling(entityRadius / TileSize) : 0;
        Func<int, int, bool> walkable = radiusTiles > 0
            ? (x, y) => IsWalkableWithClearance(x, y, radiusTiles)
            : (x, y) => _walkable[x, y];

        Point startTile = WorldToTile(fromWorld);
        Point goalTile = WorldToTile(toWorld);

        if (!walkable(startTile.X, startTile.Y))
            startTile = FindNearestWalkable(startTile, walkable);
        if (!walkable(goalTile.X, goalTile.Y))
            goalTile = FindNearestWalkable(goalTile, walkable);

        if (startTile.X < 0 || goalTile.X < 0)
        {
            Vector2 delta = toWorld - fromWorld;
            if (delta != Vector2.Zero)
            {
                direction = Vector2.Normalize(delta);
                return true;
            }
            return false;
        }

        List<Point>? tilePath = AStarPathfinder.FindPath(
            WidthInTiles,
            HeightInTiles,
            walkable,
            startTile,
            goalTile);

        if (tilePath == null || tilePath.Count == 0)
        {
            Vector2 delta = toWorld - fromWorld;
            if (delta != Vector2.Zero)
            {
                direction = Vector2.Normalize(delta);
                return true;
            }
            return false;
        }

        if (tilePath.Count == 1)
        {
            // Already at the (snapped) goal tile — steer straight at the final world target.
            Vector2 delta = toWorld - fromWorld;
            if (delta == Vector2.Zero)
                return false;
            direction = Vector2.Normalize(delta);
            return true;
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
