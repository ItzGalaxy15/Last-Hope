using Last_Hope.Classes.Weapon;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using System;
using Last_Hope.Helpers;

namespace Last_Hope;

public class Bow : BaseWeapon
{
    private float _speed;
    public bool piercingArrows;
    public bool poisonArrows;
    public bool spreadPoison;
    public bool increasedPoisonDamage;
    public bool explosiveArrows;
    public bool increasedExplosionRadius;
    public bool increasedExplosionDamage;
    public bool tripleShot;
    public Action<BaseEnemy> OnHitCallBack { get; set; }
    private GameObject _owner;

    private const float TripleShotAngle = 10f;

    public Bow(string name, float speed, GameObject owner) : base(name)
    {
        _speed = speed;
        _owner = owner;
    }

    public override void Attack(Vector2 direction, Vector2 origin, int damage, float critChance)
    {
        if (_owner is BasePlayer)
        {
            if (tripleShot)
            {
                
                CreateArrow(VectorHelper.RotateVector(direction, TripleShotAngle), origin, damage, critChance);
                CreateArrow(direction, origin, damage, critChance);
                CreateArrow(VectorHelper.RotateVector(direction, -TripleShotAngle), origin, damage, critChance);
            }
            else
            {
                CreateArrow(direction, origin, damage, critChance);
            }
        }
        else
        {
            var arrow = new EnemyArrow(origin, direction, _speed, _owner, damage, critChance);
            GameManager.GetGameManager().AddGameObject(arrow);
        }
    }

    private void CreateArrow(Vector2 direction, Vector2 origin, int damage, float critChance)
    {
        var arrow = new Arrow(
            origin, direction, _speed, _owner, damage, critChance, 
            piercingArrows,
            poisonArrows, spreadPoison, increasedPoisonDamage,
            explosiveArrows, increasedExplosionRadius, increasedExplosionDamage,
            OnHitCallBack);
        GameManager.GetGameManager().AddGameObject(arrow);
    }

    public override void SetOwner(GameObject owner)
    {
        _owner = owner;
    }
}