using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Last_Hope.Collision;

public class ArcCollider : Collider
{
    public List<LinePieceCollider> ArcSegments { get; private set; }
    public Vector2 Center { get; set; }
    public float Radius { get; set; }
    public float ArcAngle { get; set; }
    public int Segments { get; set; }

    /// <summary>
    /// Creates an arc-shaped collider for slash attacks
    /// </summary>
    /// <param name="center">Center point of the arc</param>
    /// <param name="direction">Direction the arc points towards</param>
    /// <param name="radius">Distance from center to arc</param>
    /// <param name="width">Length of each segment</param>
    /// <param name="arcAngle">Total angle of the arc in radians (default ~216°)</param>
    /// <param name="segments">Number of segments in the arc (default 12)</param>
    public ArcCollider(Vector2 center, Vector2 direction, float radius, float width, float arcAngle = (float)Math.PI * 1.2f, int segments = 12)
    {
        Center = center;
        Radius = radius;
        ArcAngle = arcAngle;
        Segments = segments;
        ArcSegments = new List<LinePieceCollider>();

        GenerateArcSegments(direction, width);
    }

    private void GenerateArcSegments(Vector2 direction, float width)
    {
        float baseAngle = (float)Math.Atan2(direction.Y, direction.X);

        for (int i = 0; i < Segments; i++)
        {
            float segmentAngle = baseAngle - (ArcAngle / 2f) + (ArcAngle / Segments) * i;

            Vector2 segmentPos = Center + new Vector2(
                (float)Math.Cos(segmentAngle),
                (float)Math.Sin(segmentAngle)
            ) * Radius;

            Vector2 segmentDirection = new Vector2(
                (float)Math.Cos(segmentAngle),
                (float)Math.Sin(segmentAngle)
            );

            ArcSegments.Add(new LinePieceCollider(segmentPos, segmentDirection, width));
        }
    }

    public override bool Contains(Vector2 coordinates)
    {
        foreach (var segment in ArcSegments)
        {
            if (segment.Contains(coordinates))
                return true;
        }
        return false;
    }

    public override bool Intersects(CircleCollider other)
    {
        foreach (var segment in ArcSegments)
        {
            if (segment.Intersects(other))
                return true;
        }
        return false;
    }

    public override bool Intersects(RectangleCollider other)
    {
        foreach (var segment in ArcSegments)
        {
            if (segment.Intersects(other))
                return true;
        }
        return false;
    }

    public override bool Intersects(LinePieceCollider other)
    {
        foreach (var segment in ArcSegments)
        {
            if (segment.Intersects(other))
                return true;
        }
        return false;
    }

    public override bool Intersects(ArcCollider other)
    {
        foreach (var segment in ArcSegments)
        {
            foreach (var otherSegment in other.ArcSegments)
            {
                if (segment.Intersects(otherSegment))
                    return true;
            }
        }
        return false;
    }

    public override Rectangle GetBoundingBox()
    {
        float minX = Center.X - Radius;
        float maxX = Center.X + Radius;
        float minY = Center.Y - Radius;
        float maxY = Center.Y + Radius;

        return new Rectangle((int)minX, (int)minY, (int)(maxX - minX), (int)(maxY - minY));
    }
}