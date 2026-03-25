using Microsoft.Xna.Framework;
using System;

namespace Last_Hope.Collision
{
    public class RectangleCollider : Collider, IEquatable<RectangleCollider>
    {
        public Rectangle shape;

        public RectangleCollider(Rectangle shape)
        {
            this.shape = shape;
        }

        public override bool Contains(Vector2 loc)
        {
            return shape.Contains(loc);
        }

        public bool Equals(RectangleCollider other)
        {
            return shape == other.shape;
        }

        public override Rectangle GetBoundingBox()
        {
            return shape;
        }

        public override bool Intersects(CircleCollider other)
        {
            return other.Intersects(this);
        }

        public override bool Intersects(RectangleCollider other)
        {
            // Inclusive AABBs: MonoGame's Rectangle.Intersects uses strict <, so edge-touching
            // pairs never count; that often misses valid gameplay contacts.
            Rectangle a = shape;
            Rectangle b = other.shape;
            return a.Left <= b.Right && b.Left <= a.Right
                && a.Top <= b.Bottom && b.Top <= a.Bottom;
        }

        public override bool Intersects(LinePieceCollider other)
        {
            return other.Intersects(this);
        }

        public override bool Intersects(ArcCollider other)
        {
            return other.Intersects(this);
        }
    }
}