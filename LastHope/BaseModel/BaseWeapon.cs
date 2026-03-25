using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope;

public abstract class BaseWeapon : GameObject
{
    public string Name { get; set; }
    public int Attack { get; set; }
    public float CritChance { get; set; }

    protected BaseWeapon(string name, int attack, float critChance)
    {
        Name = name;
        Attack = attack;
        CritChance = critChance;
    }

    public abstract void Slash(Vector2 direction, Vector2 origin);

    public override void Update(GameTime gameTime)
    {
    }
}