using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Last_Hope.Classes.Items;
using Last_Hope.Helpers;
using Last_Hope.Systems.ItemSystem;
using Last_Hope.SkillTree;
using Last_Hope.Classes.Abilities;

namespace Last_Hope;

public class Archer : BasePlayer
{
    public Texture2D BowSprite;
    public Texture2D ArcherSprite;
    public InputManager _inputManager { get; private set; }

    private const int FrameSize = 32;
    private const float ArcherDrawScale = 3f;
    private const float BowDrawScale = 1.8f;
    private const float WalkFrameDuration = 0.12f;

    private int _walkRow;
    private int _walkFrameIndex;
    private float _walkFrameTimer;
    public override float _bodyWidth => FrameSize * ArcherDrawScale;
    private float _bowPixelSize => FrameSize * BowDrawScale;
    private float BowOffsetY => (_bodyWidth - _bowPixelSize) * 0.5f;

    private const float EnemyContactDamage = 10f;
    private const float EnemyContactHurtInterval = 0.5f;
    private const bool DebugDrawHitbox = true;
    private double timeSinceLastAttack = 0;
    private bool _facingLeft;
    private RectangleCollider _collider;

    // Bow attack animation
    private const int BowSheetColumns = 3;
    private const float BowDrawDuration = 0.35f;
    private bool _isDrawingBow;
    private float _bowDrawTimer;
    private Vector2 _bowAimDirection;
    private const float ArrowSpeed = 600f;

    public BaseAbility ActiveAbility { get; set; }

    //Base Archer Stats
    public override float BaseMaxHp { get; } = 80f;
    public override int BaseDamage { get; } = 25;
    public override float BaseCritChance { get; } = 0.1f;
    public override float BaseHaste { get; } = 0.9f;// Attack cooldown
    public override float BaseSpeed { get; } = 200f;

    //Current Archer Stats
    public override float CurrentMaxHp { get; protected set; }
    public override int CurrentDamage { get; protected set; }
    public override float CurrentCritChance { get; protected set; }
    public override float CurrentHaste { get; protected set; }
    public override float CurrentSpeed { get; protected set; }

    // Skill tree related
    public bool hasPiercingArrows;
    public bool hasCritGuarantee;
    public bool hasRapidFire;
    private int _hitCounterAttackSpeed = 0;
    private int _hitCounterCritGuarantee = 0;
    private const int HitsForAttackSpeed = 1;
    private const int HitsForCritGuarentee = 1;
    private float _attackSpeedBoostTimer = 0f;
    private float _critGuaranteeTimer = 0f;
    private bool _hasPoisonTouch;
    private bool _hasPoisonSpread;
    private bool _hasIncreasedPoisonDamage;
    private const float AttackSpeedBoostDuration = 5f;
    private const float CritGuaranteeDuration = 2f;
    private const float AttackSpeedBoostAmount = 0.3f;
    private const float PoisonDamagePerTick = 5f;
    

    public Archer(Vector2 startPosition)
        : base(position: startPosition, weapon: new Bow("Bow", speed: ArrowSpeed, owner: null), level: 0, experience: 0, dashDistance: 140f)
    {
        _position = startPosition;
        var origin = new Point((int)startPosition.X, (int)startPosition.Y);
        _collider = new RectangleCollider(new Rectangle(origin, Point.Zero));
        SetCollider(_collider);
        Inventory = new ItemType[2] { ItemType.Bomb, ItemType.Decoy };
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        AimArrowSprite = content.Load<Texture2D>("AimArrow");
        BowSprite = content.Load<Texture2D>("Bow sheet");
        ArcherSprite = content.Load<Texture2D>("ArcherSheet");
        _deathSound = content.Load<SoundEffect>("sounds/Death sound");
        _inputManager = GameManager.GetGameManager().InputManager;
        _Weapon.SetOwner(this);

        SyncColliderToPosition();
        SetCollider(_collider);
        MovementHelper.ClampToMapBounds(_position, _bodyWidth);
        SyncColliderToPosition();
    }

    public override void Update(GameTime gameTime)
    {
        if (!GameManager.GetGameManager().playerAlive || _currentHp <= 0f)
            return;

        //if (IsStunned)
        //{
        //    float stunDt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        //    _hurtCooldown = TimerHelper.DecreaseTimer(_hurtCooldown, stunDt);
        //    base.Update(gameTime);
        //    return;
        //}

        Move(_moveInput, gameTime);
        SyncColliderToPosition();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _hurtCooldown = TimerHelper.DecreaseTimer(_hurtCooldown, dt);
        _itemActionCooldown = TimerHelper.DecreaseTimer(_itemActionCooldown, dt);
        _greenGlowTimer = TimerHelper.DecreaseTimer(_greenGlowTimer, dt);
        _dashCooldown = TimerHelper.DecreaseTimer(_dashCooldown, dt);
        _teleportCooldown = TimerHelper.DecreaseTimer(_teleportCooldown, dt);

        _attackSpeedBoostTimer = TimerHelper.DecreaseTimer(_attackSpeedBoostTimer, dt);
        _critGuaranteeTimer = TimerHelper.DecreaseTimer(_critGuaranteeTimer, dt);

        bool moving = _moveInput != Vector2.Zero;
        if (moving)
        {
            SetWalkRowFromDirection(_moveInput);
            
            _walkFrameTimer += dt;
            while (_walkFrameTimer >= WalkFrameDuration)
            {
                _walkFrameTimer -= WalkFrameDuration;
                _walkFrameIndex = (_walkFrameIndex + 1) % 4;
            }
        }
        else
        {
            _walkFrameTimer = 0f;
            _walkFrameIndex = 0;
        }

        // Update bow draw animation
        if (_isDrawingBow)
        {
            _bowDrawTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_bowDrawTimer >= BowDrawDuration)
            {
                _isDrawingBow = false;
                _bowDrawTimer = 0f;
                FireArrow();
            }
        }

        // --- MAJOR ACTIVE ABILITIES LOGIC ---
        ActiveAbility?.Update(this, gameTime);

        if (_inputManager is not null && ActiveAbility != null && ActiveAbility.CanExecute())
        {
            if (_inputManager.IsGameplayKeyPress(KeybindId.Ability1))
            {
                ActiveAbility.Execute(this);
            }
        }

        if (_inputManager is not null)
        {
            timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
            bool attackPressed = _inputManager.IsGameplayKeyPress(KeybindId.Attack)
                || (KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly && _inputManager.IsGameplayKeyPress(KeybindId.KeyboardAttack));

            float effectiveHaste = _attackSpeedBoostTimer > 0f ? Math.Max(0.1f, CurrentHaste - AttackSpeedBoostAmount) : CurrentHaste;

            if (attackPressed && timeSinceLastAttack >= effectiveHaste && !_isDrawingBow)
            {
                StartBowDraw();
                timeSinceLastAttack = 0;
            }

            // place item at feet
            if (_inputManager.IsGameplayKeyPress(KeybindId.PlaceItem) && _itemActionCooldown <= 0f)
            {
                ItemSystem.PlaceSelectedItem(this);
                _itemActionCooldown = ItemActionCooldown;
            }

            // throw item toward mouse
            if (_inputManager.IsGameplayKeyPress(KeybindId.ThrowItem) && _itemActionCooldown <= 0f)
            {
                ItemSystem.ThrowSelectedItemTowardMouse(this);
                _itemActionCooldown = ItemActionCooldown;
            }

            if (_inputManager.IsGameplayKeyPress(KeybindId.Dash) && _dashCooldown <= 0f)
            {
                Vector2 dashDirection = _moveInput != Vector2.Zero ? _moveInput : _lastMoveDirection;
                if (dashDirection != Vector2.Zero)
                {
                    Dash(dashDirection, _DashDistance);
                    SetWalkRowFromDirection(dashDirection);
                    _dashCooldown = DashCooldown;
                }
            }

            if (_inputManager.IsGameplayKeyPress(KeybindId.Teleport) && _teleportCooldown <= 0f)
            {
                if (Teleport())
                    _teleportCooldown = TeleportCooldownDuration;
            }
        }

        base.Update(gameTime);
    }

    private void StartBowDraw()
    {
        if (_inputManager is null)
            return;

        Vector2 direction;
        if (KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly && _aimInput != Vector2.Zero)
        {
            direction = _aimInput;
            direction.Normalize();
        }
        else
        {
            Vector2 center = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
            Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
            direction = mousePosition - center;
            if (direction == Vector2.Zero)
                return;
            direction.Normalize();
        }

        _bowAimDirection = direction;
        _isDrawingBow = true;
        _bowDrawTimer = 0f;
    }

    private void FireArrow()
    {
        Vector2 center = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        float effectiveCrit = _critGuaranteeTimer > 0f ? 1f : CurrentCritChance;

        _Weapon.Attack(_bowAimDirection, center, CurrentDamage, effectiveCrit);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var archerSource = new Rectangle(_walkFrameIndex * FrameSize, _walkRow * FrameSize, FrameSize, FrameSize);
        Color drawColor = DrawTint;
        if (_greenGlowTimer > 0f)
            drawColor = Color.Lerp(drawColor, Color.LimeGreen, 0.5f);

        spriteBatch.Draw(ArcherSprite, _position, archerSource, drawColor, 0f, Vector2.Zero, ArcherDrawScale, SpriteEffects.None, 0f);

        // Draw bow sprite
        int bowFrame = 0;
        if (_isDrawingBow)
        {
            float progress = _bowDrawTimer / BowDrawDuration;
            bowFrame = Math.Min((int)(progress * BowSheetColumns), BowSheetColumns - 1);
        }

        Rectangle bowSource = new Rectangle(bowFrame * FrameSize, 0, FrameSize, FrameSize);
        SpriteEffects bowFlip = _facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float bowW = _bowPixelSize;
        float y = BowOffsetY;
        Vector2 bowOffset = _facingLeft
            ? new Vector2(_bodyWidth - bowW - 40f, y)
            : new Vector2(40f, y);

        spriteBatch.Draw(BowSprite, _position + bowOffset, bowSource, Color.White, 0f, Vector2.Zero, BowDrawScale, bowFlip, 0f);

        if (DebugDrawHitbox && _collider is not null)
            HitboxHelper.DrawHitbox(spriteBatch, _collider.shape, Color.LimeGreen);

        DrawAimArrow(spriteBatch);
        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
        if (other is not BaseEnemy enemy || _hurtCooldown > 0f)
            return;

        _hurtCooldown = EnemyContactHurtInterval;
        //_hurtCooldown = enemy is Troll ? 1f : EnemyContactHurtInterval;

        float damageToTake = EnemyContactDamage;
        if (enemy is Boss)
            damageToTake *= 2;

        Damage(damageToTake);

        if (_hasPoisonTouch)
        {
            if (_hasIncreasedPoisonDamage)
            {
                enemy.isPoisoned(true, PoisonDamagePerTick * 2);
            }
            else
            {
                enemy.isPoisoned(true, PoisonDamagePerTick);
            }
            if (_hasPoisonSpread)
            {
                enemy.EnablePoisonSpreading();
            }
        }
    }

    public override void Damage(float amount)
    {
        _currentHp -= amount;
        TriggerHurtFlash();
        CheckDeath();
    }

    // --- SKILL TREE INTEGRATION ---
    public void ApplyNodeEffect(NodeEffect effect)
    {
        switch (effect.EffectId)
        {
            case "damage":
                CurrentDamage += (int)effect.ValuePerPoint;
                break;
            case "speed":
                CurrentSpeed += effect.ValuePerPoint;
                break;
            case "haste":
                CurrentHaste = Math.Max(0.1f, CurrentHaste - effect.ValuePerPoint);  
                break;
            case "crit_chance":
                CurrentCritChance = Math.Min(CurrentCritChance + effect.ValuePerPoint, 1f);
                break;
            case "hp":
                CurrentMaxHp += effect.ValuePerPoint;
                Heal(effect.ValuePerPoint);
                break;
            case "unlock_piercing_arrows":
                hasPiercingArrows = true;
                ((Bow)_Weapon).piercingArrows = true;
                break;
            case "unlock_crit_guarantee":
                hasCritGuarantee = true;
                ((Bow)_Weapon).OnHitCallBack = OnArrowHit;
                break;
            case "unlock_rapid_fire":
                hasRapidFire = true;
                ((Bow)_Weapon).OnHitCallBack = OnArrowHit;
                break;
            case "unlock_giant_arrow":
                ActiveAbility = new GiantArrowAbility();
                break;
            case "unlock_poison_arrows":
                ((Bow)_Weapon).poisonArrows = true;
                break;
            case "unlock_poison_spread":
                _hasPoisonSpread = true;
                ((Bow)_Weapon).spreadPoison = true;
                break;
            case "poison_damage":
                _hasIncreasedPoisonDamage = true;
                ((Bow)_Weapon).increasedPoisonDamage = true;
                break;
            case "unlock_poison_touch":
                _hasPoisonTouch = true;
                break;
            case "unlock_arrow_storm":
                ActiveAbility = new ArrowStormAbility();
                break;
        }
        UpdateStats();
    }

    protected override void OnLevelUp()
    {
        float prevDamageBonus = _levelDamageBonus;
        base.OnLevelUp();
        CurrentMaxHp += LevelStatBonus;
        // Damage is int; apply only when the float crosses the next integer
        int dmgIncrease = (int)_levelDamageBonus - (int)prevDamageBonus;
        if (dmgIncrease > 0) CurrentDamage += dmgIncrease;
        CurrentCritChance = Math.Min(1f, CurrentCritChance + LevelStatBonus);
        CurrentHaste = Math.Max(0.1f, CurrentHaste - LevelStatBonus);
        CurrentSpeed += LevelStatBonus;
    }

    public void RevertAllSkillStats()
    {
        CurrentMaxHp = BaseMaxHp + _levelHpBonus;
        CurrentDamage = BaseDamage + (int)_levelDamageBonus;
        CurrentCritChance = BaseCritChance + _levelCritBonus;
        CurrentHaste = Math.Max(0.1f, BaseHaste - _levelHasteBonus);
        CurrentSpeed = BaseSpeed + _levelSpeedBonus;

        hasPiercingArrows = false;
        hasCritGuarantee = false;
        hasRapidFire = false;
        ((Bow)_Weapon).OnHitCallBack = null;
        ((Bow)_Weapon).piercingArrows = false;
        ((Bow)_Weapon).poisonArrows = false;
        ActiveAbility = null;

        UpdateStats();
    }

    public void UpdateStats()
    {
        
    }

    private void OnArrowHit(BaseEnemy enemy)
    {
        if (hasRapidFire)
            OnArrowHitAttackSpeed(enemy);
        if (hasCritGuarantee)
            OnArrowHitCritChance(enemy);
    }
    private void OnArrowHitAttackSpeed(BaseEnemy enemy)
    {
        _hitCounterAttackSpeed++;
        if (_hitCounterAttackSpeed >= HitsForAttackSpeed)
        {
            _hitCounterAttackSpeed = 0;
            AttackSpeedKeystone();
        }
    }

    private void OnArrowHitCritChance(BaseEnemy enemy)
    {
        _hitCounterCritGuarantee++;
        if (_hitCounterCritGuarantee >= HitsForCritGuarentee)
        {
            _hitCounterCritGuarantee = 0;
            CritChanceKeystone();
        }
    }

    private void AttackSpeedKeystone()
    {
        _attackSpeedBoostTimer = AttackSpeedBoostDuration;
    }

    private void CritChanceKeystone()
    {
        _critGuaranteeTimer = CritGuaranteeDuration;
    }

    private void SetWalkRowFromDirection(Vector2 dir)
    {
        if (dir == Vector2.Zero)
            return;

        float ax = Math.Abs(dir.X);
        float ay = Math.Abs(dir.Y);

        if (ay >= ax)
        {
            _walkRow = dir.Y > 0f ? 0 : 1;
        }
        else
        {
            _walkRow = dir.X > 0f ? 2 : 3;
            _facingLeft = dir.X < 0f;
        }
    }

    private void SyncColliderToPosition()
    {
        if (_collider is null)
            return;

        _collider.shape = CollisionHelper.CreateHitbox(_position, _bodyWidth, HitboxFraction);
        SetCollider(_collider);
    }

    protected override void ApplyDashOffset(Vector2 delta)
    {
        _position += delta;
        MovementHelper.ClampToMapBounds(_position, _bodyWidth);
        SyncColliderToPosition();
    }

    protected override void ApplyTeleportPosition(Vector2 newPosition)
    {
        _position = newPosition;
        MovementHelper.ClampToMapBounds(_position, _bodyWidth);
        SyncColliderToPosition();
    }
}