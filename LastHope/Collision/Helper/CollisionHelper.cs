using Microsoft.Xna.Framework;

namespace Last_Hope.Collision;

//Helper to check if a position would collide with the world.
public static class CollisionHelper
{
    public static bool WouldCollideAt(Vector2 testPosition, float bodyWidth, float hitboxFraction)
    {
        float hitboxSize = bodyWidth * hitboxFraction;
        float offset = (bodyWidth - hitboxSize) / 2f;

        Rectangle testRect = new Rectangle(
            (int)(testPosition.X + offset),
            (int)(testPosition.Y + offset),
            (int)hitboxSize,
            (int)hitboxSize
        );

        return CollisionWorld.CollidesWithStaticForMovement(testRect);
    }
}