using System.Collections.Generic;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;

public static class CollisionWorld
{
    private static readonly List<Collider> _staticColliders = new List<Collider>();

    /// <summary>Adds a collider to the static list so walls and buildings block movement checks.</summary>
    public static void RegisterStatic(Collider collider) => _staticColliders.Add(collider);

    /// <summary>Empties the static collider list. Called when loading a new level so old colliders don't carry over.</summary>
    public static void ClearStatic() => _staticColliders.Clear();

    /// <summary>
    /// Loops through every registered static collider and returns true if the given collider
    /// overlaps any of them. Used for wall and building collision detection.
    /// </summary>
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
    /// Convenience overload that takes a Rectangle directly so the player and enemies don't have
    /// to construct a RectangleCollider themselves when testing a proposed move position.
    /// </summary>
    public static bool CollidesWithStaticForMovement(Rectangle bounds)
        => CollidesWithStatic(new RectangleCollider(bounds));
}