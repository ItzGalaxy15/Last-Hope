using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BasePlayer : GameObject
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

    public override void OnCollision(GameObject gameObject)
    {

    }

    public override void Update(GameTime gameTime)
    {

    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {

    }

    public abstract Vector2 GetPosition();
}
