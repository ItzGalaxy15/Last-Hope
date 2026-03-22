using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BaseEnemy : GameObject
{

    public virtual int MaxHealth { get; protected set; }

    public virtual int CurrentHealth { get; protected set; }

    public virtual int Speed { get; protected set; }

    //public abstract BaseWeapon Weapon { get; protected set; }

    public override abstract void Load(ContentManager content);

    public override abstract void Update(GameTime gameTime);

    public override abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

    public abstract void onCollision(GameObject other);
}