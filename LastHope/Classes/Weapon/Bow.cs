using Last_Hope.Classes.Weapon;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope;

public class Bow : BaseWeapon
{
    private float _speed;
    private GameObject _owner;

    public Bow(string name, int damage, float critChance, float speed, GameObject owner) : base(name, damage, critChance)
    {
        _speed = speed;
        _owner = owner;
    }

    public override void Attack(Vector2 direction, Vector2 origin)
    {
        var arrow = new Arrow(origin, direction, _speed, _owner, Damage, CritChance);
        GameManager.GetGameManager().AddGameObject(arrow);
    }
}