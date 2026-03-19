using System;
using SpaceDefence.Collision;
using Microsoft.Xna.Framework;

namespace Last_Hope.Collision;

public class CircleCollider : Collider, IEquatable<CircleCollider>
{
    public float X;
    public float Y;
    public Vector2 Center 
    { 
        get { 
            return new Vector2(X, Y); 
        } 

        set { 
            X = value.X; Y = value.Y; 
        } 
    }
    public float Radius;

    /// <summary>
    /// Creates a new Circle object.
    /// </summary>
    /// <param name="x">The X coordinate of the circle's center</param>
    /// <param name="y">The Y coordinate of the circle's center</param>
    /// <param name="radius">The radius of the circle</param>
    public CircleCollider(float x, float y, float radius)
    {
        this.X = x; 
        this.Y = y; 
        this.Radius = radius;
    }

    /// <summary>
    /// Creates a new Circle object.
    /// </summary>
    /// <param name="center">The coordinates of the circle's center</param>
    /// <param name="radius">The radius of the circle</param>
    public CircleCollider(Vector2 center, float radius)
    {
        this.Center = center;
        this.Radius = radius;
    }


    /// <summary>
    /// Gets whether or not the provided coordinates lie within the bounds of this Circle.
    /// </summary>
    /// <param name="coordinates">The coordinates to check.</param>
    /// <returns>true if the coordinates are within the circle.</returns>
    public override bool Contains(Vector2 coordinates)
    {
        return (Center-coordinates).Length() < Radius;
    }

    /// <summary>
    /// Gets whether or not the Circle intersects another Circle.
    /// </summary>
    /// <param name="other">The Circle to check for intersection.</param>
    /// <returns>true there is any overlap between the two Circles.</returns>
    public override bool Intersects(CircleCollider other)
    {
        // ||B - A|| < r1 + r2
        if ((Center - other.Center).Length() < Radius + other.Radius)
        {
            return true;
        }
        return false;
    }


    /// <summary>
    /// Gets whether or not the Circle intersects the Rectangle.
    /// </summary>
    /// <param name="other">The Rectangle to check for intersection.</param>
    /// <returns>true there is any overlap between the Circle and the Rectangle.</returns>
    public override bool Intersects(RectangleCollider other)
    {
        // if Cy < Ab & Cy > At => Cx + r > Al & Cx - r < Ar
        // if Cx < Ar & Cx > Al => Cy + r > At & Cy - r < Ab
        if (Y < other.shape.Bottom & Y > other.shape.Top)
        {
            if (X + Radius > other.shape.Left & X - Radius < other.shape.Right)
            {
                return true;
            }
        }
        else if (X < other.shape.Right & X > other.shape.Left)
        {
            if (Y + Radius > other.shape.Top & Y - Radius < other.shape.Bottom)
            {
                return true;
            }

        }

        Vector2 TopLeft = new Vector2(other.shape.Left, other.shape.Top);
        Vector2 TopRight = new Vector2(other.shape.Right, other.shape.Top);
        Vector2 BottomLeft = new Vector2(other.shape.Left, other.shape.Bottom);
        Vector2 BottomRight = new Vector2(other.shape.Right, other.shape.Bottom);

        if (this.Contains(TopLeft) || this.Contains(TopRight) || this.Contains(BottomRight) || this.Contains(BottomLeft)) return true;
        return false;
    }
    /// <summary>
    /// Gets whether or not the Circle intersects the Line
    /// </summary>
    /// <param name="other">The Line to check for intersection</param>
    /// <returns>true there is any overlap between the Circle and the Line.</returns>
    public override bool Intersects(LinePieceCollider other)
    {
        // Implemented in the line code.
        return other.Intersects(this);
    }

    /// <summary>
    /// Get the enclosing Rectangle that surrounds the Circle.
    /// </summary>
    /// <returns></returns>
    public override Rectangle GetBoundingBox()
    {
        return new Rectangle((int)(X - Radius), (int)(Y - Radius), (int)(2 * Radius), (int)(2 * Radius));
    }

    public bool Equals(CircleCollider other)
    {
        return other.X == X && other.Y == Y && other.Radius == Radius;
    }
}