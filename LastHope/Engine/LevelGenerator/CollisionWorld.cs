using System.Collections.Generic;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;

public static class CollisionWorld
{
    private static readonly List<Collider> _staticColliders = new List<Collider>();

    public static IReadOnlyList<Collider> StaticColliders => _staticColliders;

    public static void RegisterStatic(Collider collider) => _staticColliders.Add(collider);

    public static void ClearStatic() => _staticColliders.Clear();

    public static bool CollidesWithStatic(Collider mover)
    {
        foreach (var collider in _staticColliders)
        {
            if (mover.CheckIntersection(collider))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Static overlap for axis-separated movement. Shrinks <paramref name="bounds"/> slightly so the mover's
    /// outer corners do not snag on single-pixel corners of adjacent static tiles (more noticeable on large AABBs).
    /// </summary>
    public static bool CollidesWithStaticForMovement(Rectangle bounds)
    {
        int minSide = System.Math.Min(bounds.Width, bounds.Height);
        int skin = System.Math.Max(2, minSide / 28);
        int innerW = bounds.Width - 2 * skin;
        int innerH = bounds.Height - 2 * skin;
        if (innerW < 1 || innerH < 1)
            return CollidesWithStatic(new RectangleCollider(bounds));

        Rectangle shrunk = new Rectangle(bounds.X + skin, bounds.Y + skin, innerW, innerH);
        return CollidesWithStatic(new RectangleCollider(shrunk));
    }
}