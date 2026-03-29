using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BaseEnemy : GameObject
{
    public virtual RectangleCollider _collider { get; protected set; }

    public virtual Texture2D _texture { get; protected set; }

    public virtual float MaxHealth { get; protected set; }

    public virtual float CurrentHealth { get; protected set; }

    public virtual int Speed { get; protected set; }
    public virtual float ExperienceValue { get; protected set; }

    //public abstract BaseWeapon Weapon { get; protected set; }

    public BaseEnemy(float maxHealth, float currentHealth, int speed, float experienceValue)
    {
        MaxHealth = maxHealth;
        CurrentHealth = currentHealth;
        Speed = speed;
        ExperienceValue = experienceValue;

    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
    }

    public virtual void Damage(float amount)
    {
        CurrentHealth -= amount;
    }

    public abstract Vector2 GetPosition();
}