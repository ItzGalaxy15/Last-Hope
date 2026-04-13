using System.Collections.Generic;
using Last_Hope.Collision;

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
}