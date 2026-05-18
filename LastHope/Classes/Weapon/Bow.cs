using Last_Hope.Classes.Weapon;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;

namespace Last_Hope;

public class Bow : BaseWeapon
{
    private float _speed;
    private GameObject _owner;

    public Bow(string name, float speed, GameObject owner) : base(name)
    {
        _speed = speed;
        _owner = owner;
    }

    public override void Attack(Vector2 direction, Vector2 origin, int damage, float critChance)
    {
        if (_owner is BasePlayer)
        {
            var arrow = new Arrow(origin, direction, _speed, _owner, damage, critChance);
            GameManager.GetGameManager().AddGameObject(arrow);
        }
        else
        {
            var arrow = new EnemyArrow(origin, direction, _speed, _owner, damage, critChance);
            GameManager.GetGameManager().AddGameObject(arrow);
        }
    }

    public override void SetOwner(GameObject owner)
    {
        _owner = owner;
    }
}