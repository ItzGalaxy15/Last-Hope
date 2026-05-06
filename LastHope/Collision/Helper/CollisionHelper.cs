using Microsoft.Xna.Framework;

namespace Last_Hope.Collision;

//Helper to check if a position would collide with the world.
public static class CollisionHelper
{
    public static Rectangle CreateHitbox(Vector2 position, float bodyWidth, float hitboxFraction)
    {
        float size = bodyWidth * hitboxFraction;
        float offset = (bodyWidth - size) / 2f;

        return new Rectangle(
            (int)(position.X + offset),
            (int)(position.Y + offset),
            (int)size,
            (int)size
        );
    }
    public static bool WouldCollideAt(Vector2 testPosition, float bodyWidth, float hitboxFraction)
    {
        Rectangle testRect = CreateHitbox(testPosition, bodyWidth, hitboxFraction);
        return CollisionWorld.CollidesWithStaticForMovement(testRect);
    }
}