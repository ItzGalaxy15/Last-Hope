using LastHope.Audio;
using Last_Hope.Classes.Items;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Last_Hope.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Last_Hope.BaseModel;

public abstract class BasePlayer : GameObject
{
    public Texture2D AimArrowSprite;
    public Vector2 _position { get; protected set; }

    // Fraction of the body size used as the hitbox — tune this to adjust fairness.
    protected const float HitboxFraction = 0.55f;
    public abstract float _bodyWidth { get; }

    // Base Player stats
    public float _currentHp { get; protected set; }
    public abstract float BaseMaxHp { get; }
    public abstract int BaseDamage { get; }
    public abstract float BaseCritChance { get; }
    public abstract float BaseHaste { get; } // Attack cooldown
    public abstract float BaseSpeed { get; }
    public BaseWeapon _Weapon { get; protected set; }

    // Current Player stats (after buffs/debuffs)
    public abstract float CurrentMaxHp { get; protected set; }
    public abstract int CurrentDamage { get; protected set; }
    public abstract float CurrentCritChance { get; protected set; }
    public abstract float CurrentHaste { get; protected set; }
    public abstract float CurrentSpeed { get; protected set; }

    private float _stunTimer;
    private float _stunVisualTotalDuration;
    private float _stunCooldownTimer;
    private const float StunCooldownDuration = 3f;

    public bool IsStunned => _stunTimer > 0f;
    public float StunVisualProgress => _stunVisualTotalDuration > 0f
        ? MathHelper.Clamp(_stunTimer / _stunVisualTotalDuration, 0f, 1f)
        : 0f;

    public void ApplyStun(float duration)
    {
        if (duration <= 0f)
            return;

        if (_stunCooldownTimer > 0f)
            return;

        float newTimer = Math.Max(_stunTimer, duration);
        if (newTimer > _stunTimer)
            _stunVisualTotalDuration = newTimer;

        _stunTimer = newTimer;
        _stunCooldownTimer = StunCooldownDuration;
    }

    // Poison status (mirrors BaseEnemy's poison)
    private bool _isPoisoned;
    private float _poisonTimer;
    private float _poisonTickTimer;
    private float _poisonDamagePerTick;
    private const float PoisonDuration = 3f;
    private const float PoisonTickInterval = 0.5f;
    public bool IsPoisoned => _isPoisoned;

    public void ApplyPoison(float damagePerTick)
    {
        if (_isPoisoned)
        {
            _poisonTimer = PoisonDuration; // refresh
            return;
        }
        _isPoisoned = true;
        _poisonDamagePerTick = damagePerTick;
        _poisonTimer = PoisonDuration;
        _poisonTickTimer = PoisonTickInterval;
    }

    protected new Color DrawTint
    {
        get
        {
            Color baseTint = base.DrawTint;
            if (baseTint != Color.White) return baseTint; // hurt flash wins
            if (_isPoisoned) return Color.Purple;
            return Color.White;
        }
    }

    // Shared input state
    protected Vector2 _moveInput;
    protected Vector2 _aimInput;
    public Vector2 AimInput => _aimInput;
    protected Vector2 _lastMoveDirection;

    // Aim arrow parameters
    private const float AimArrowDistance = 70f;
    private const float AimArrowScale = 2f;

    // Dash parameters
    public float _DashDistance {get; protected set; }
    protected abstract void ApplyDashOffset(Vector2 delta);

    /// <summary>Two-slot utility hotbar. When null, pickups and the item HUD skip this player.</summary>
    public ItemType[]? Inventory { get; protected set; }
    public int ExtraLives { get; protected set; } = 0;

    // Teleportation parameters
    private const float TeleportMinTileDistance = 20f;
    private const float TeleportEnemyClearance = 160f;

    // global cooldowns constants
    protected const float ItemActionCooldown = 1f;
    protected const float TeleportCooldownDuration = 60f;
    protected const float DashCooldown = 0.75f;

    // global cooldown timers
    protected float _itemActionCooldown;
    protected float _teleportCooldown;
    protected float _dashCooldown;
    protected float _greenGlowTimer;
    protected float _hurtCooldown;

    // Sounds
    protected SoundEffect _deathSound;


    // Level EXP
    public int _Level { get; protected set; }
    public float _Experience { get; protected set; }
    private const float XpPerLevel = 10f;
    private const float LevelUpFlashDuration = 0.45f;
    private float _levelUpFlashTimer;
    public int Level => _Level;
    public float LevelUpFlashProgress => MathHelper.Clamp(_levelUpFlashTimer / LevelUpFlashDuration, 0f, 1f);

    // Accumulated stat bonuses from leveling (never reset)
    protected float _levelHpBonus;
    protected float _levelDamageBonus;
    protected float _levelCritBonus;
    protected float _levelHasteBonus;
    protected float _levelSpeedBonus;
    public float LevelHpBonus     => _levelHpBonus;
    public float LevelDamageBonus => _levelDamageBonus;
    public float LevelCritBonus   => _levelCritBonus;
    public float LevelHasteBonus  => _levelHasteBonus;
    public float LevelSpeedBonus  => _levelSpeedBonus;
    protected const float LevelStatBonus = 0.01f;
    private const int TalentPointInterval = 5;

    public event Action OnTalentPointEarned;

    // UI bar properties
    public float DashCooldownProgress => MathHelper.Clamp(_dashCooldown / DashCooldown, 0f, 1f);
    public float TeleportCooldownProgress => MathHelper.Clamp(_teleportCooldown / TeleportCooldownDuration, 0f, 1f);

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
            float HealthProgress = _currentHp / CurrentMaxHp;
            return MathHelper.Clamp(HealthProgress, 0f, 1f);
        }
    }

    protected BasePlayer(Vector2 position, BaseWeapon weapon, int level, int experience, float dashDistance)
    {
        _position = position;
        _Weapon = weapon;
        _Level = level;
        _Experience = experience;
        _DashDistance = dashDistance;
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

    public void Heal(float amount)
    {
        _currentHp += amount;
        if (_currentHp > CurrentMaxHp)
        {
            _currentHp = CurrentMaxHp;
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
        while (_Level < newLevel)
        {
            _Level++;
            OnLevelUp();
        }
    }

    protected virtual void OnLevelUp()
    {
        _levelUpFlashTimer = LevelUpFlashDuration;
        _levelHpBonus     += LevelStatBonus;
        _levelDamageBonus += LevelStatBonus;
        _levelCritBonus   += LevelStatBonus;
        _levelHasteBonus  += LevelStatBonus;
        _levelSpeedBonus  += LevelStatBonus;
        if (_Level % TalentPointInterval == 0)
            OnTalentPointEarned?.Invoke();
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

            if (CollisionHelper.WouldCollideAt(next, _bodyWidth, HitboxFraction))
                break;

            current = next;
            moved += step.Length();
        }

        ApplyDashOffset(current - start);
    }
    // read this about teleport and had similar issues: https://community.monogame.net/t/finding-spawn-points/8100/4
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

            if (CollisionHelper.WouldCollideAt(candidate, _bodyWidth, HitboxFraction))
                continue;

            if (!IsPositionSafe(candidate))
                continue;

            ApplyTeleportPosition(candidate);
            return true;
        }

        return false;
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_levelUpFlashTimer > 0f)
            _levelUpFlashTimer = TimerHelper.DecreaseTimer(_levelUpFlashTimer, dt);

        if (_stunTimer > 0f)
        {
            _stunTimer = TimerHelper.DecreaseTimer(_stunTimer, dt);
            if (_stunTimer <= 0f)
            {
                _stunTimer = 0f;
                _stunVisualTotalDuration = 0f;
            }
        }

        if (_stunCooldownTimer > 0f)
            _stunCooldownTimer = TimerHelper.DecreaseTimer(_stunCooldownTimer, dt);

        if (_isPoisoned)
        {
            _poisonTimer = TimerHelper.DecreaseTimer(_poisonTimer, dt);
            _poisonTickTimer = TimerHelper.DecreaseTimer(_poisonTickTimer, dt);

            if (_poisonTickTimer <= 0f)
            {
                _poisonTickTimer = PoisonTickInterval;
                Damage(_poisonDamagePerTick);
            }

            if (_poisonTimer <= 0f)
                _isPoisoned = false;
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {

    }

    public override bool IsYSorted => true;
    public override float GetSortY() => GetPosition().Y + _bodyWidth;

    public Vector2 GetPosition()
    {
        return _position;
    }

    public override void HandleInput(InputManager inputManager)
    {
        _moveInput = Vector2.Zero;

        if (IsStunned)
            return;

        if (inputManager.IsGameplayKeyDown(KeybindId.MoveUp))    _moveInput.Y -= 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveDown))  _moveInput.Y += 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveLeft))  _moveInput.X -= 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveRight)) _moveInput.X += 1f;

        Vector2 rawAim = Vector2.Zero;
        if (inputManager.IsGameplayKeyDown(KeybindId.AimUp))    rawAim.Y -= 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.AimDown))  rawAim.Y += 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.AimLeft))  rawAim.X -= 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.AimRight)) rawAim.X += 1f;

        if (rawAim != Vector2.Zero)
            _aimInput = rawAim;
        else if (_moveInput != Vector2.Zero)
            _aimInput = _moveInput;

        if (_moveInput != Vector2.Zero)
        {
            Vector2 normalized = _moveInput;
            normalized.Normalize();
            _lastMoveDirection = normalized;
        }
    }

    public void Move(Vector2 direction, GameTime gameTime)
    {
        if (IsStunned || direction == Vector2.Zero)
        {
            return;
        }

        direction.Normalize();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 velocity = direction * CurrentSpeed * dt;

        // --- X movement ---
        Vector2 newPosX = new Vector2(_position.X + velocity.X, _position.Y);
        if (!CollisionHelper.WouldCollideAt(newPosX, _bodyWidth, HitboxFraction))
        {
            _position = newPosX;
        }

        // --- Y movement ---
        Vector2 newPosY = new Vector2(_position.X, _position.Y + velocity.Y);
        if (!CollisionHelper.WouldCollideAt(newPosY, _bodyWidth, HitboxFraction))
        {
            _position = newPosY;
        }

        _position = MovementHelper.ClampToMapBounds(_position, _bodyWidth);

        var gm = GameManager.GetGameManager();
        if (gm.ForestBoundaryX > 0f)
        {
            if (gm.CurrentZone == Zone.Village)
            {
                if (gm.IsForestLocked && _position.X < gm.ForestBoundaryX)
                    _position = new Vector2(gm.ForestBoundaryX, _position.Y);
                else if (gm.VillageCleared && _position.X < gm.ForestBoundaryX)
                    gm.CurrentZone = Zone.Forest;
            }
            else
            {
                if (_position.X > gm.ForestBoundaryX)
                    _position = new Vector2(gm.ForestBoundaryX, _position.Y);
            }
        }
    }

    protected bool IsPositionSafe(Vector2 position)
    {
        var gm = GameManager.GetGameManager();
        Vector2 center = position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        foreach (var obj in gm._gameObjects)
        {
            if (obj is not BaseEnemy) continue;
            var collider = obj.GetCollider();
            if (collider == null) continue;
            if (Vector2.Distance(center, collider.GetBoundingBox().Center.ToVector2()) < TeleportEnemyClearance)
                return false;
        }
        return true;
    }

    protected void CheckDeath()
    {
        if (_currentHp <= 0f)
        {
            if (ExtraLives > 0)
            {
                Revive();
            }
            else
            {
                Die();
            }
        }
    }

    protected void Revive()
    {
        ExtraLives--;
        _currentHp = CurrentMaxHp; // Prevent death

        _greenGlowTimer = 1.5f;
        _hurtCooldown = 1.5f; // Grant 1.5 seconds of invincibility to escape
    }

    protected void Die()
    {
        _currentHp = 0f;
        AudioManager.PlaySfx(_deathSound);
        GameManager.GetGameManager().playerAlive = false;
        GameManager.GetGameManager()._state = GameState.GameOver;
    }

    protected void DrawAimArrow(SpriteBatch spriteBatch)
    {
        if (AimArrowSprite == null)
            return;

        Vector2 center = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        Vector2 direction;

        if (KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly)
        {
            direction = _aimInput;
            if (direction == Vector2.Zero)
                return;
            direction.Normalize();
        }
        else
        {
            Vector2 mousePos = GameManager.GetGameManager().GetWorldMousePosition();
            direction = mousePos - center;
            if (direction == Vector2.Zero)
                return;
            direction.Normalize();
        }

        Vector2 arrowPos = center + direction * AimArrowDistance;

        float rotation = (float)Math.Atan2(direction.Y, direction.X) + MathHelper.PiOver2;

        Vector2 origin = new Vector2(
            AimArrowSprite.Width * 0.5f,
            AimArrowSprite.Height * 0.5f
        );

        spriteBatch.Draw(
            AimArrowSprite,
            arrowPos,
            null,
            Color.White,
            rotation,
            origin,
            AimArrowScale,
            SpriteEffects.None,
            0f
        );
    }

    public abstract void Damage(float amount);

    protected abstract void ApplyTeleportPosition(Vector2 newPosition);
}