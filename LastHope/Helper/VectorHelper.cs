using System;
using Microsoft.Xna.Framework;

namespace Last_Hope.Helpers;

public static class VectorHelper
{
    public static Vector2 RotateVector(Vector2 vector, float degrees)
    {
        // https://stackoverflow.com/questions/4780119/2d-euclidean-vector-rotations used as reference for this method
        float radians = MathHelper.ToRadians(degrees);
        
        float cos = (float)MathF.Cos(radians);
        float sin = (float)MathF.Sin(radians);
        
        return new Vector2(
            vector.X * cos - vector.Y * sin,
            vector.X * sin + vector.Y * cos
        );
    }
}
