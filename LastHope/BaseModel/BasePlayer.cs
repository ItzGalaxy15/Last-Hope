using System;
using System.Numerics;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BasePlayer
{
    public float _Hp { get; protected set; }
    public BaseWeapon _Weapon { get; protected set; }
    public float _Speed { get; protected set; }

    protected BasePlayer(float hp, BaseWeapon weapon, float speed)
    {
        _Hp = hp;
        _Weapon = weapon;
        _Speed = speed;
    }

    public virtual void OnCollision(RectangleCollider other)
    {

    }

    public virtual void Update(GameTime gameTime)
    {

    }

    public virtual void Draw(GameTime gameTime)
    {

    }


}