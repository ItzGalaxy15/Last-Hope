using Last_Hope.Classes.Weapon;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Weapon : BaseWeapon
{
    private const float SlashRadius = 100f;
    private const float SlashWidth = 60f;

    public Weapon(string name, int attack, float critChance) : base(name, attack, critChance)
    {
    }

    public override void Slash(Vector2 direction, Vector2 origin)
    {
        ArcCollider arcCollider = new ArcCollider(origin, direction, SlashRadius, SlashWidth);
        GameManager.GetGameManager().AddGameObject(new Slash(arcCollider, Attack, CritChance));
    }
}