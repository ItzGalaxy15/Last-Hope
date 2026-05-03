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

    public virtual float Speed { get; protected set; }
    public virtual float ExperienceValue { get; protected set; }

    //public abstract BaseWeapon Weapon { get; protected set; }

    // Fraction of (frameSize * spriteScale) used as the hitbox side length.
    // Override per-enemy to tune without touching hitbox math.
    protected virtual float HitboxFraction => 0.5f;

    // --- DEBUG TOGGLE ---
    // Set to false to completely disable all stun logic and visuals on enemies
    public bool EnableStuns = true;

    public float StunTimer { get; private set; }
    public bool IsStunned => EnableStuns && StunTimer > 0f;

    public void ApplyStun(float duration)
    {
        if (!EnableStuns) return;
        
        if (duration > StunTimer)
        {
            StunTimer = duration;
        }
    }

    protected new Color DrawTint
    {
        get
        {
            Color baseTint = base.DrawTint;
            if (baseTint != Color.White) return baseTint; // Hurt flash takes priority
            return IsStunned ? Color.Gold : Color.White;  // Golden tint to indicate Stunned
        }
    }

    public BaseEnemy(float maxHealth, float currentHealth, float speed, float experienceValue)
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

    // Sealed to enforce that ALL enemies use the central stun check below.
    public sealed override void Update(GameTime gameTime)
    {
        base.Update(gameTime); // Call GameObject base to ensure HurtFlash is updated

        if (EnableStuns && StunTimer > 0f)
        {
            StunTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (StunTimer < 0f) StunTimer = 0f;
            
            // 1 Clean Place: Centralized Stun Freeze. Skips all enemy logic while stunned.
            return; 
        }
        else if (!EnableStuns)
        {
            StunTimer = 0f;
        }

        UpdateBehavior(gameTime);
    }

    // Derived classes MUST implement this instead of overriding Update()
    protected abstract void UpdateBehavior(GameTime gameTime);

    public override void OnCollision(GameObject other)
    {
    }

    public virtual void Damage(float amount)
    {
        CurrentHealth -= amount;
        TriggerHurtFlash();
    }

    public abstract Vector2 GetPosition();

    protected bool WouldCollideAt(Vector2 testPosition, float hitboxSize, float offset)
    {
        Rectangle testRect = new Rectangle(
            (int)(testPosition.X + offset),
            (int)(testPosition.Y + offset),
            (int)hitboxSize,
            (int)hitboxSize
        );
        return CollisionWorld.CollidesWithStaticForMovement(testRect);
    }
}