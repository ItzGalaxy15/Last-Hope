using Last_Hope.Classes.Weapon;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope;

public class Weapon : BaseWeapon
{
    public GameObject _owner { get; private set; }
    private const float SlashRadius = 100f;
    private const float SlashWidth = 60f;

    public Weapon(string name, int damage, float critChance) : base(name, damage, critChance)
    {
    }

    public override void Attack(Vector2 direction, Vector2 origin)
    {
        System.Console.WriteLine("Weapon.Slash called");
        ArcCollider arcCollider = new ArcCollider(origin, direction, SlashRadius, SlashWidth);
        var slash = new Slash(arcCollider, Damage, CritChance, origin, direction);
        GameManager.GetGameManager().AddGameObject(slash);
        System.Console.WriteLine("Slash added to GameManager");
    }

    public override void SetOwner(GameObject owner)
    {
        _owner = owner;
    }
}