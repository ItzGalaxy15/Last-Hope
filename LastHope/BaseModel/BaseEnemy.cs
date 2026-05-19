using System.Dynamic;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Last_Hope.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BaseEnemy : GameObject
{
    public virtual RectangleCollider _collider { get; protected set; }
    public virtual Texture2D _texture { get; protected set; }

    //Poison variables
    public bool _isPoisoned;
    private float _poisonTimer;
    private float _poisonTickTimer;
    private const float PoisonDuration = 3f;
    private const float PoisonTickInterval = 0.5f;
    protected float PoisonDamagePerTick;
    protected bool _poisonSpreads;
    private float _poisonSpreadTimer;
    private const float PoisonSpreadDuration = 1f;

    //public abstract BaseWeapon Weapon { get; protected set; }

    // Fraction of (frameSize * spriteScale) used as the hitbox side length.
    // Override per-enemy to tune without touching hitbox math.
    protected virtual float HitboxFraction => 0.5f;

    // Base enemy stats
    public float _currentHp { get; protected set; }
    public abstract float BaseMaxHp { get; }
    public abstract int BaseDamage { get; }
    public abstract float BaseCritChance { get; }
    public abstract float BaseHaste { get; } // Attack cooldown
    public abstract float BaseSpeed { get; }
    public virtual float ExperienceValue { get; protected set; }

    // Current enemy stats (after buffs/debuffs)
    public abstract float CurrentMaxHp { get; protected set; }
    public abstract int CurrentDamage { get; protected set; }
    public abstract float CurrentCritChance { get; protected set; }
    public abstract float CurrentHaste { get; protected set; }
    public abstract float CurrentSpeed { get; protected set; }

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
            if (IsStunned) return Color.Gold; // Stunned enemies are gold-tinted
            if (_isPoisoned) return Color.Purple; // Poisoned enemies are purple-tinted
            return Color.White;
        }
    }

    public BaseEnemy()
    {
        MakeStats();
        _currentHp = CurrentMaxHp;
    }

    protected void MakeStats()
    {
        CurrentMaxHp = BaseMaxHp;
        CurrentDamage = BaseDamage;
        CurrentCritChance = BaseCritChance;
        CurrentHaste = BaseHaste;
        CurrentSpeed = BaseSpeed;
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

    public override bool IsYSorted => true;
    public override float GetSortY() => GetCollider()?.GetBoundingBox().Bottom ?? GetPosition().Y;

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
        _currentHp -= amount;
        TriggerHurtFlash();
        if (_currentHp < 0f)
        {
            GameManager.GetGameManager().RemoveGameObject(this);
        }
    }

    public virtual void isPoisoned(bool poisoned, float poisonDamage)
{
    if (poisoned && _isPoisoned)
    {
        _poisonTimer = PoisonDuration; // refresh timer without reapplying slow
        return;
    }

    _isPoisoned = poisoned;
    if (poisoned)
    {
        PoisonDamagePerTick = poisonDamage;
        _poisonTimer = PoisonDuration;
        _poisonTickTimer = PoisonTickInterval;
    }
}

    // This method needs to be called inside update loops of all enemies to apply poison damage and handle poison duration.
    protected void UpdatePoison(float dt)
    {
        if (!_isPoisoned) return;

        _poisonTimer = TimerHelper.DecreaseTimer(_poisonTimer, dt);
        _poisonTickTimer = TimerHelper.DecreaseTimer(_poisonTickTimer, dt);
        _poisonSpreadTimer = TimerHelper.DecreaseTimer(_poisonSpreadTimer, dt);

        if (_poisonSpreadTimer <= 0f)
            _poisonSpreads = false;

        if (_poisonTickTimer <= 0f)
        {
            _poisonTickTimer = PoisonTickInterval;
            Damage(PoisonDamagePerTick);
        }

        if (_poisonTimer <= 0f)
        {
            _isPoisoned = false;
            _poisonSpreads = false;
            CurrentSpeed = BaseSpeed;
        }
    }

    public void EnablePoisonSpreading()
    {
        _poisonSpreads = true;
        _poisonSpreadTimer = PoisonSpreadDuration;
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