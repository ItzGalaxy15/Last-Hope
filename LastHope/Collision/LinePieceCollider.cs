// References:
//   Segment-vs-segment math reused by Intersects(RectangleCollider) (Step 2, performance):
//     Same standard-form (Ax+By+C=0) line intersection algorithm already cited inline
//     at Intersects(LinePieceCollider) below
//     (https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines).
//     The Step 2 refactor moves that algorithm into a static helper that operates on
//     raw Vector2 endpoints so no LinePieceCollider objects are allocated per call.

using System;
using Microsoft.Xna.Framework;

namespace Last_Hope.Collision;

public class LinePieceCollider : Collider, IEquatable<LinePieceCollider>
{

    public Vector2 Start;
    public Vector2 End;

    /// <summary>
    /// The length of the LinePiece, changing the length moves the end vector to adjust the length.
    /// </summary>
    public float Length
    {
        get
        {
            return (End - Start).Length();
        }
        set
        {
            End = Start + GetDirection() * value;
        }
    }

    /// <summary>
    /// The A component from the standard line formula Ax + By + C = 0
    /// </summary>
    public float StandardA
    {
        // A = Py - Qy
        get
        {
            var a = Start.Y - End.Y;
            return a;
        }
    }

    /// <summary>
    /// The B component from the standard line formula Ax + By + C = 0
    /// </summary>
    public float StandardB
    {
        // B = Qx - Px
        get
        {
            var b = End.X - Start.X;
            return b;
        }
    }

    /// <summary>
    /// The C component from the standard line formula Ax + By + C = 0
    /// </summary>
    public float StandardC
    {
        // C = Px X Qy - Py X Qx
        get
        {
            var c = Start.X * End.Y - Start.Y * End.X;
            return c;
        }
    }

    public LinePieceCollider(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    public LinePieceCollider(Vector2 start, Vector2 direction, float length)
    {
        Start = start;
        End = start + direction * length;
    }

    /// <summary>
    /// Should return the angle between a given direction and the up vector.
    /// </summary>
    /// <param name="direction">The Vector2 pointing out from (0,0) to calculate the angle to.</param>
    /// <returns> The angle in radians between the the up vector and the direction to the cursor.</returns>
    public static float GetAngle(Vector2 direction)
    {
        var angle = (float)Math.Atan2(direction.X, -(direction.Y));
        return angle;
    }


    /// <summary>
    /// Calculates the normalized vector pointing from point1 to point2
    /// </summary>
    /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
    public static Vector2 GetDirection(Vector2 point1, Vector2 point2)
    {
        var direction = point2 - point1;
        var normalizedDirection = Vector2.Normalize(direction);
        return normalizedDirection;
    }


    /// <summary>
    /// Gets whether or not the Line intersects another Line
    /// </summary>
    /// <param name="other">The Line to check for intersection</param>
    /// <returns>true there is any overlap between the line and the Line.</returns>
    public override bool Intersects(LinePieceCollider other)
    {
        //https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
        var intersectPoint = GetIntersection(other);
        float delta = this.StandardA * other.StandardB - other.StandardA * this.StandardB;
        if (delta == 0)
        {
            return false;
        }

        if (this.Contains(intersectPoint) && other.Contains(intersectPoint))
        {
            return true;
        }
        return false;
    }


    /// <summary>
    /// Gets whether or not the line intersects a Circle.
    /// </summary>
    /// <param name="other">The Circle to check for intersection.</param>
    /// <returns>true there is any overlap between the two Circles.</returns>
    public override bool Intersects(CircleCollider other)
    {
        var nearestPoint = NearestPointOnLine(other.Center);
        if (other.Contains(nearestPoint))
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets whether or not the Line intersects the Rectangle.
    /// </summary>
    /// <param name="other">The Rectangle to check for intersection.</param>
    /// <returns>true there is any overlap between the Line and the Rectangle.</returns>
    /// <remarks>
    /// Hot path during Whirlwind (12 arc segments per Slash). Uses the static
    /// SegmentsIntersect helper instead of allocating four LinePieceColliders per call.
    /// </remarks>
    public override bool Intersects(RectangleCollider other)
    {
        float left = other.shape.Left;
        float top = other.shape.Top;
        float right = other.shape.Right;
        float bottom = other.shape.Bottom;

        Vector2 tl = new Vector2(left, top);
        Vector2 tr = new Vector2(right, top);
        Vector2 br = new Vector2(right, bottom);
        Vector2 bl = new Vector2(left, bottom);

        if (SegmentsIntersect(Start, End, tl, tr)) return true; // top edge
        if (SegmentsIntersect(Start, End, tl, bl)) return true; // left edge
        if (SegmentsIntersect(Start, End, tr, br)) return true; // right edge
        if (SegmentsIntersect(Start, End, bl, br)) return true; // bottom edge
        if (other.Contains(Start)) return true;
        if (other.Contains(End)) return true;
        return false;
    }

    /// <summary>
    /// Allocation-free segment-vs-segment intersection on raw Vector2 endpoints.
    /// Same standard-form line intersection algorithm as Intersects(LinePieceCollider),
    /// extracted so RectangleCollider's four edges can be tested without allocating
    /// new LinePieceCollider instances.
    /// </summary>
    private static bool SegmentsIntersect(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        float aA = a1.Y - a2.Y;
        float aB = a2.X - a1.X;
        float aC = a1.X * a2.Y - a1.Y * a2.X;

        float bA = b1.Y - b2.Y;
        float bB = b2.X - b1.X;
        float bC = b1.X * b2.Y - b1.Y * b2.X;

        float delta = aA * bB - bA * aB;
        if (delta == 0f) return false;

        float ix = (bB * aC - aB * bC) / delta;
        float iy = (aA * bC - bA * aC) / delta;

        return PointWithinSegmentBox(ix, iy, a1, a2)
            && PointWithinSegmentBox(ix, iy, b1, b2);
    }

    private static bool PointWithinSegmentBox(float x, float y, Vector2 a, Vector2 b)
    {
        bool xIn = (x >= a.X && x <= b.X) || (x >= b.X && x <= a.X);
        bool yIn = (y >= a.Y && y <= b.Y) || (y >= b.Y && y <= a.Y);
        return xIn && yIn;
    }

    /// <summary>
    /// Calculates the intersection point between 2 lines.
    /// </summary>
    /// <param name="Other">The line to intersect with</param>
    /// <returns>A Vector2 with the point of intersection.</returns>
    public Vector2 GetIntersection(LinePieceCollider Other)
    {
        float delta = this.StandardA * Other.StandardB - Other.StandardA * this.StandardB;
        if (delta == 0)
        {
            return Vector2.Zero;
        }
        float x = (Other.StandardB * this.StandardC - this.StandardB * Other.StandardC) / delta;
        float y = (this.StandardA * Other.StandardC - Other.StandardA * this.StandardC) / delta;
        Vector2 result = new Vector2(x, y);
        return result;
    }

    /// <summary>
    /// Finds the nearest point on a line to a given vector, taking into account if the line is .
    /// </summary>
    /// <param name="other">The Vector you want to find the nearest point to.</param>
    /// <returns>The nearest point on the line.</returns>
    public Vector2 NearestPointOnLine(Vector2 other)
    {
        //https://stackoverflow.com/questions/3120357/get-closest-point-to-a-line
        var AP = other - Start;
        var AB = End - Start;

        var magnitudeAB = AB.LengthSquared();
        var ABAPproduct = Vector2.Dot(AP, AB);
        var distance = ABAPproduct / magnitudeAB;
        if (distance < 0)
        {
            return Start;
        }
        else if (distance > 1)
        {
            return End;
        }
        else
        {
            return Start + AB * distance;
        }
    }

    /// <summary>
    /// Returns the enclosing Axis Aligned Bounding Box containing the control points for the line.
    /// As an unbound line has infinite length, the returned bounding box assumes the line to be bound.
    /// </summary>
    /// <returns></returns>
    public override Rectangle GetBoundingBox()
    {
        Point topLeft = new Point((int)Math.Min(Start.X, End.X), (int)Math.Min(Start.Y, End.Y));
        Point size = new Point((int)Math.Max(Start.X, End.X), (int)Math.Max(Start.Y, End.Y)) - topLeft;
        return new Rectangle(topLeft, size);
    }


    /// <summary>
    /// Gets whether or not the provided coordinates lie on the line.
    /// </summary>
    /// <param name="coordinates">The coordinates to check.</param>
    /// <returns>true if the coordinates are on the line.</returns>
    public override bool Contains(Vector2 coordinates)
    {
        var onTheLine = (coordinates.X >= this.Start.X && coordinates.X <= this.End.X || coordinates.X >= this.End.X && coordinates.X <= this.Start.X)
                        && (coordinates.Y >= this.Start.Y && coordinates.Y <= this.End.Y || coordinates.Y >= this.End.Y && coordinates.Y <= this.Start.Y);
        return onTheLine;
    }

    public bool Equals(LinePieceCollider other)
    {
        return other.Start == this.Start && other.End == this.End;
    }

    public override bool Intersects(ArcCollider other)
    {
        return other.Intersects(this);
    }

    /// <summary>
    /// Calculates the normalized vector pointing from point1 to point2
    /// </summary>
    /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
    public static Vector2 GetDirection(Point point1, Point point2)
    {
        return GetDirection(point1.ToVector2(), point2.ToVector2());
    }


    /// <summary>
    /// Calculates the normalized vector pointing from point1 to point2
    /// </summary>
    /// <returns> A Vector2 containing the direction from point1 to point2. </returns>
    public Vector2 GetDirection()
    {
        return GetDirection(Start, End);
    }


    /// <summary>
    /// Should return the angle between a given direction and the up vector.
    /// </summary>
    /// <param name="direction">The Vector2 pointing out from (0,0) to calculate the angle to.</param>
    /// <returns> The angle in radians between the the up vector and the direction to the cursor.</returns>
    public float GetAngle()
    {
        return GetAngle(GetDirection());
    }
}