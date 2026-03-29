using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.BaseModel;

public abstract class BasePlayer : GameObject
{
    public float _Hp { get; protected set; }
    public BaseWeapon _Weapon { get; protected set; }
    public float _Speed { get; protected set; }
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

    protected BasePlayer(float hp, BaseWeapon weapon, float speed, int level, int experience)
    {
        _Hp = hp;
        _Weapon = weapon;
        _Speed = speed;
        _Level = level;
        _Experience = experience;
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

    public override void Update(GameTime gameTime)
    {
        if (_levelUpFlashTimer > 0f)
            _levelUpFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {

    }

    public abstract Vector2 GetPosition();
}
