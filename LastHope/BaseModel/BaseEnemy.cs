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

    // Fraction of (frameSize * spriteScale) used as the hitbox side length.
    // Override per-enemy to tune without touching hitbox math.
    protected virtual float HitboxFraction => 0.5f;
    public float StunTimer { get; private set; }

    public void ApplyStun(float duration)
    {
        if (duration > StunTimer)
        {
            StunTimer = duration;
        }
    }

    public BaseEnemy(float maxHealth, float currentHealth, int speed, float experienceValue)
    {
        MaxHealth = maxHealth;
        CurrentHealth = currentHealth;
        Speed = speed;
        ExperienceValue = experienceValue;
    }

    // Centers a square hitbox on `center`, sized as frameSize * spriteScale * HitboxFraction.
    protected void InitHitbox(Vector2 center, int frameSize, float spriteScale)
    {
        float hitboxSize = frameSize * spriteScale * HitboxFraction;
        _collider.shape = new Rectangle(
            (int)(center.X - hitboxSize / 2f),
            (int)(center.Y - hitboxSize / 2f),
            (int)hitboxSize,
            (int)hitboxSize
        );
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }
    public virtual void Update(GameTime gameTime)
    {
        if (StunTimer > 0f)
        {
            StunTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            return;
        }
      
    }

    public override void OnCollision(GameObject other)
    {
    }

    public virtual void Damage(float amount)
    {
        CurrentHealth -= amount;
        TriggerHurtFlash();
    }

    public abstract Vector2 GetPosition();
}