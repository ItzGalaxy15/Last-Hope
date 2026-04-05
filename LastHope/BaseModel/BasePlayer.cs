using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BasePlayer : GameObject
{
    public float _maxHp { get; protected set; }
    public float _currentHp { get; protected set; }
    public BaseWeapon _Weapon { get; protected set; }
    public float _Speed { get; protected set; }

    public float _DashDistance {get; protected set; }
    protected abstract void ApplyDashOffset(Vector2 delta);

    public int ExtraLives { get; protected set; } = 0;

    // Level EXP
    public int _Level { get; protected set; }
    public float _Experience { get; protected set; }
    private const float XpPerLevel = 10f;
    private const float LevelUpFlashDuration = 0.45f;
    private float _levelUpFlashTimer;
    public int Level => _Level;
    public float LevelUpFlashProgress => MathHelper.Clamp(_levelUpFlashTimer / LevelUpFlashDuration, 0f, 1f);

    public float ExperienceProgress
    {
        get
        {
            float progress = (_Experience % XpPerLevel) / XpPerLevel;
            return MathHelper.Clamp(progress, 0f, 1f);
        }
    }

    public float HealthProgress
    {
        get
        {
            float HealthProgress = (_currentHp / _maxHp);
            return MathHelper.Clamp(HealthProgress, 0f, 1f);
        }
    }

    protected BasePlayer(float maxHp, BaseWeapon weapon, float speed, int level, int experience, float dashDistance)
    {
        _maxHp = maxHp;
        _currentHp = maxHp;
        _Weapon = weapon;
        _Speed = speed;
        _Level = level;
        _Experience = experience;
        _DashDistance = dashDistance;
    }

    public void Heal(float amount)
    {
        _currentHp += amount;
        if (_currentHp > _maxHp)
        {
            _currentHp = _maxHp;
        }
    }

    public void AddLife(int count = 1)
    {
        ExtraLives += count;
    }

    public void AddExperience(float amount)
    {
        _Experience += amount;
        CheckLevelUp();
    }

    private void CheckLevelUp()
    {
        int newLevel = (int)(_Experience / XpPerLevel);
        if (newLevel > _Level)
        {
            _Level = newLevel;
            OnLevelUp();
        }
    }

    protected virtual void OnLevelUp()
    {
        _levelUpFlashTimer = LevelUpFlashDuration;
        // Override in subclasses to handle level up effects
    }

    protected void Dash(Vector2 direction, float distance)
    {
        if (direction == Vector2.Zero)
            return;
        direction.Normalize();
        ApplyDashOffset(direction * distance);
    }


    public override void Update(GameTime gameTime)
    {
        if (_levelUpFlashTimer > 0f)
            _levelUpFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {

    }

    public abstract Vector2 GetPosition();

    public abstract void Damage(float amount);
}
