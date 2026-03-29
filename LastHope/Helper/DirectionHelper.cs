using System;
using Microsoft.Xna.Framework;

namespace LastHope.Helper;

public static class DirectionHelper
{
    public static Vector2 Normalize(Vector2 direction)
    {
        if (direction == Vector2.Zero)
        {
            return Vector2.Zero;
        }
        return Vector2.Normalize(direction);
    }

    public static float GetAngle(Vector2 direction)
    {
        return (float)Math.Atan2(direction.Y, direction.X);
    }
}