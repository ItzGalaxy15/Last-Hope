using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Last_Hope.Engine.Pathfinding;

/// <summary>
/// Grid pathfinding using A* algorithm (4-directional, uniform edge cost).
/// Uses Manhattan distance as heuristic for optimal grid pathfinding.
/// </summary>
public static class AStarPathfinder
{
    private static readonly Point[] Offsets =
    {
        new(0, -1),
        new(1, 0),
        new(0, 1),
        new(-1, 0),
        new(1, -1),
        new(1, 1),
        new(-1, 1),
        new(-1, -1),
    };

    /// <summary>
    /// Finds a shortest path from <paramref name="start"/> to <paramref name="goal"/> on a rectangular grid.
    /// </summary>
    /// <param name="width">Grid width in cells.</param>
    /// <param name="height">Grid height in cells.</param>
    /// <param name="isWalkable">Returns whether the cell at (x, y) can be entered.</param>
    /// <param name="start">Start cell (inclusive).</param>
    /// <param name="goal">Goal cell (inclusive).</param>
    /// <returns>Cells from start to goal inclusive, or <c>null</c> if unreachable or invalid.</returns>
    public static List<Point>? FindPath(
        int width,
        int height,
        Func<int, int, bool> isWalkable,
        Point start,
        Point goal)
    {
        if (width <= 0 || height <= 0)
            return null;

        if (!InBounds(start.X, start.Y, width, height) || !InBounds(goal.X, goal.Y, width, height))
            return null;

        if (!isWalkable(start.X, start.Y) || !isWalkable(goal.X, goal.Y))
            return null;

        if (start == goal)
            return new List<Point> { start };

        int startIdx = ToIndex(start.X, start.Y, width);
        int goalIdx = ToIndex(goal.X, goal.Y, width);

        int total = width * height;
        var dist = new float[total];
        var parent = new int[total];
        for (int i = 0; i < total; i++)
        {
            dist[i] = float.PositiveInfinity;
            parent[i] = -1;
        }

        dist[startIdx] = 0f;

        var pq = new PriorityQueue<int, float>();
        pq.Enqueue(startIdx, Heuristic(start.X, start.Y, goal.X, goal.Y));

        var closed = new bool[total];

        while (pq.Count > 0)
        {
            pq.TryDequeue(out int u, out float _);
            if (closed[u])
                continue;
            closed[u] = true;
            float d = dist[u];

            if (u == goalIdx)
                break;

            int ux = u % width;
            int uy = u / width;

            for (int k = 0; k < Offsets.Length; k++)
            {
                int vx = ux + Offsets[k].X;
                int vy = uy + Offsets[k].Y;
                if (!InBounds(vx, vy, width, height))
                    continue;
                if (!isWalkable(vx, vy))
                    continue;

                // Prevent cutting through wall corners on diagonal moves
                int dx = Offsets[k].X;
                int dy = Offsets[k].Y;
                if (dx != 0 && dy != 0)
                {
                    if (!isWalkable(ux + dx, uy) || !isWalkable(ux, uy + dy))
                        continue;
                }

                int v = ToIndex(vx, vy, width);
                float nd = d + GetEdgeCost(ux, uy, vx, vy);
                if (nd < dist[v])
                {
                    dist[v] = nd;
                    parent[v] = u;
                    float fCost = nd + Heuristic(vx, vy, goal.X, goal.Y);
                    pq.Enqueue(v, fCost);
                }
            }
        }

        if (float.IsPositiveInfinity(dist[goalIdx]))
            return null;

        return ReconstructPath(parent, width, startIdx, goalIdx);
    }

    /// <summary>
    /// Uniform cost for now; override via a different helper if you add weighted terrain.
    /// </summary>
    private static readonly float Sqrt2 = MathF.Sqrt(2f);

    private static float GetEdgeCost(int x0, int y0, int x1, int y1) =>
        (x0 != x1 && y0 != y1) ? Sqrt2 : 1f;

    private static float Heuristic(int x, int y, int goalX, int goalY)
    {
        int dx = Math.Abs(x - goalX);
        int dy = Math.Abs(y - goalY);
        return dx + dy + (Sqrt2 - 2f) * Math.Min(dx, dy);
    }

    private static List<Point> ReconstructPath(int[] parent, int width, int startIdx, int goalIdx)
    {
        var stack = new Stack<Point>();
        for (int at = goalIdx; at != -1; at = parent[at])
        {
            stack.Push(new Point(at % width, at / width));
            if (at == startIdx)
                break;
        }

        var list = new List<Point>(stack.Count);
        while (stack.Count > 0)
            list.Add(stack.Pop());

        return list;
    }

    private static int ToIndex(int x, int y, int width) => y * width + x;

    private static bool InBounds(int x, int y, int width, int height) =>
        x >= 0 && x < width && y >= 0 && y < height;
}
