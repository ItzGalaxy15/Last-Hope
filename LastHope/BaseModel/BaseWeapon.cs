using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope;

public abstract class BaseWeapon : GameObject
{
    public string Name { get; set; }
    public int Damage { get; set; }
    public float CritChance { get; set; }

    protected BaseWeapon(string name)
    {
        Name = name;
    }

    public abstract void Attack(Vector2 direction, Vector2 origin, int damage, float critChance);

    public override void Update(GameTime gameTime)
    {
    }

    public abstract void SetOwner(GameObject owner);
}