using Last_Hope.Classes.Items;
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

    /// <summary>Two-slot utility hotbar. When null, pickups and the item HUD skip this player.</summary>
    public ItemType[]? Inventory { get; protected set; }

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

    public bool TryPickupItem(ItemType item)
    {
        if (Inventory is null || Inventory.Length == 0)
            return false;

        for (int i = 0; i < Inventory.Length; i++)
        {
            if (Inventory[i] == ItemType.None)
            {
                Inventory[i] = item;
                return true;
            }
        }

        return false;
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

        Vector2 start = GetPosition();
        Vector2 step = direction * 8f; // faster than movement steps
        float moved = 0f;

        Vector2 current = start;

        while (moved < distance)
        {
            Vector2 next = current + step;

            if (WouldCollideAt(next))
                break;

            current = next;
            moved += step.Length();
        }

        ApplyDashOffset(current - start);
    }

    private const float TeleportMinTileDistance = 60f;

    protected bool Teleport()
    {
        var gm = GameManager.GetGameManager();
        var grid = gm.NavigationGrid;
        if (grid == null) return false;

        float mapW = grid.WidthInTiles * grid.TileSize;
        float mapH = grid.HeightInTiles * grid.TileSize;
        float minDist = TeleportMinTileDistance * grid.TileSize;

        Vector2 current = GetPosition();
        var rng = gm.RNG;

        const int MaxAttempts = 50;
        for (int i = 0; i < MaxAttempts; i++)
        {
            float x = (float)(rng.NextDouble() * mapW);
            float y = (float)(rng.NextDouble() * mapH);
            Vector2 candidate = new Vector2(x, y);

            if (Vector2.Distance(current, candidate) < minDist)
                continue;

            if (WouldCollideAt(candidate))
                continue;

            if (!IsPositionSafe(candidate))
                continue;

            ApplyTeleportPosition(candidate);
            return true;
        }

        return false;
    }

    protected virtual bool IsPositionSafe(Vector2 position) => true;
    protected abstract void ApplyTeleportPosition(Vector2 newPosition);


    public override void Update(GameTime gameTime)
    {
        if (_levelUpFlashTimer > 0f)
            _levelUpFlashTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {

    }
    

    public abstract Vector2 GetPosition();

    public abstract void Damage(float amount);

    protected abstract bool WouldCollideAt(Vector2 testPosition);
}
