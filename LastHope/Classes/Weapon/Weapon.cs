using Last_Hope.Classes.Weapon;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope;

public class Weapon : BaseWeapon
{
    public GameObject _owner { get; private set; }
    private const float SlashRadius = 20f;
    private const float SlashWidth = 20f;
    private const float SlashVisualExpand = 45f;
    private const float SlashHitboxExpand = 55f;

    public Weapon(string name ) : base(name)
    {
    }

    public override void Attack(Vector2 direction, Vector2 origin, int damage, float critChance)
    {
        Vector2 pivot = origin - direction * (SlashRadius + SlashWidth / 2f);
        ArcCollider arcCollider = new ArcCollider(pivot, direction, SlashRadius, SlashWidth);
        var slash = new Slash(arcCollider, damage, critChance, origin, direction, SlashVisualExpand, SlashHitboxExpand);
        GameManager.GetGameManager().AddGameObject(slash);
    }

    /// <summary>
    /// Spawns a cosmetic-only Slash (no collider, no damage) used when the damage
    /// is being applied by some other means (e.g. Whirlwind's single radial AoE).
    /// The Slash's CheckCollision short-circuits on the null collider, so these
    /// objects never enter the narrow-phase collision loop.
    /// </summary>
    public static void AttackVisual(Vector2 direction, Vector2 origin)
    {
        var slash = new Slash(null, 0, 0f, origin, direction, SlashVisualExpand, SlashHitboxExpand);
        GameManager.GetGameManager().AddGameObject(slash);
    }

    public override void SetOwner(GameObject owner)
    {
        _owner = owner;
    }
}