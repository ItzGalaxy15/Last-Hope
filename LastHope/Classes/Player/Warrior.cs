using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Classes.Items;
using Last_Hope.SkillTree; // Import Skill Tree structures
using LastHope.Audio;
using Last_Hope.Helpers;
using Last_Hope.Systems.ItemSystem;
using Last_Hope.Classes.Abilities;
using System.Linq;

namespace Last_Hope;

public class Warrior : BasePlayer
{
    public Texture2D AxeSprite;
    public Texture2D WarriorSprite;
    public InputManager _inputManager { get; private set; }

    private const int FrameSize = 32;
    private const float WarriorDrawScale = 3f;
    private const float AxeDrawScale = 1.8f;
    private const float WalkFrameDuration = 0.12f;
    private const int AbilityFrames = 4;
    private const int AbilityColumns = 4;
    private const int AxeAbilityRow = 0;
    private const int ShieldAbilityRow = 1;
    private const int ShieldSlamSheetRow = 0;
    private const float AbilityDrawScale = 3f;

    private int _walkRow;
    private int _walkFrameIndex;
    private float _walkFrameTimer;
    public override float _bodyWidth => FrameSize * WarriorDrawScale;
    private float _axePixelSize => FrameSize * AxeDrawScale;
    private float AxeOffsetY => (_bodyWidth - _axePixelSize) * 0.5f;
    private const float EnemyContactDamage = 10f;
    private const float EnemyContactHurtInterval = 0.5f;
    private const bool DebugDrawHitbox = false;

    // --- TIMERS & COOLDOWNS ---
    private double _timeSinceLastAttack = 0;
    private float _speedBuffTimer = 0f;
    private float _dmgBuffTimer = 0f;
    private float _defBuffTimer = 0f;

    // --- STATE & PHYSICS ---
    private bool _facingLeft;
    private RectangleCollider _collider;

    private const float SlashDistance = 105f;
    private const float SlashCastHeightOffset = 10f;

    // --- COMBAT & SKILL CONFIGURATION ---
    private const float BuffDurationSeconds = 10.0f;
    private const double ProcChance = 0.10;
    private const float AdrenalineRegenRate = 5.0f;
    private SoundEffect _attackSound;

    // --- Skill Tree States ---
    public Texture2D SwordSprite { get; private set; }
    public Texture2D ShieldSprite { get; private set; }

    public bool IsSwordActive { get; set; }
    public bool IsAxeActive { get; set; }
    public bool IsShieldActive { get; set; }

    public bool DualWieldUnlocked { get; set; }
    public bool HasFlowingStrikes { get; set; }
    public bool HasBloodlust { get; set; }
    public bool HasAdrenalineRush { get; set; }

    public bool WhirlwindUnlocked { get; set; }
    public bool AxeSlamUnlocked { get; set; }
    public bool ShieldSlamUnlocked { get; set; }
    
    public int DodgeLevel { get; set; }
    public int ArmorPenLevel { get; set; }
    public int BlockLevel { get; set; }
    public bool HasElusiveRhythm { get; set; }
    public bool HasDeepWounds { get; set; }
    public bool HasBulwark { get; set; }

    public int HasteLevel { get; set; }
    public int DmgLevel { get; set; }

    public BaseAbility ActiveAbility { get; set; }

    // Base Warrior stats
    public override float BaseMaxHp { get; } = 100f;
    public override int BaseDamage { get; } = 20;
    public override float BaseCritChance { get; } = 0.1f;
    public override float BaseHaste { get; } = 0.7f; // Attack cooldown
    public override float BaseSpeed { get; } = 220f;

    // Current Warrior stats
    public override float CurrentMaxHp { get; protected set; }
    public override int CurrentDamage { get; protected set; }
    public override float CurrentCritChance { get; protected set; }
    public override float CurrentHaste { get; protected set; }
    public override float CurrentSpeed { get; protected set; }
    
    public Texture2D WarriorAbilitySprite { get; private set; }
    public Texture2D ShieldSlamSprite { get; private set; }
    private AnimationManager _abilityAnimation;
    private bool _isCastingAbility;
    private bool _abilityHitTriggered;
    private BaseAbility _castingAbility;
    private Texture2D _abilitySprite;
    private float _abilityRotation;
    private Vector2 _abilityAimDir;
    private Vector2 _abilityOrigin;
    private Vector2 _abilityDrawPos;
    private bool _abilityRotate;
    private float _abilityDrawScale;

    public Warrior(Vector2 startPosition)
        : base(position: startPosition, weapon: new Weapon("Sword"), level: 0, experience: 0, dashDistance: 140f)
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
        AxeSprite = content.Load<Texture2D>("AxeSheet");
        try { SwordSprite = content.Load<Texture2D>("SwordSheet"); } catch { SwordSprite = AxeSprite; } // Fallback to avoid crashes
        try { ShieldSprite = content.Load<Texture2D>("ShieldSprite"); } catch { ShieldSprite = AxeSprite; } // Fallback to avoid crashes
        
        WarriorSprite = content.Load<Texture2D>("WarriorSheet");
        try { WarriorAbilitySprite = content.Load<Texture2D>("WarriorAbilitySheet"); } catch { WarriorAbilitySprite = null; }
        try { ShieldSlamSprite = content.Load<Texture2D>("ShieldSlam"); } catch { ShieldSlamSprite = null; }
        _deathSound = content.Load<SoundEffect>("sounds/Death sound");
        _attackSound = content.Load<SoundEffect>("sounds/Warrior Attack");
        _inputManager = GameManager.GetGameManager().InputManager;

        SyncColliderToPosition();
        SetCollider(_collider);
        MovementHelper.ClampToMapBounds(_position, _bodyWidth);
        SyncColliderToPosition();
    }

    public override void Update(GameTime gameTime)
    {
        if (!GameManager.GetGameManager().playerAlive || _currentHp <= 0f)
            return;

        if (_isCastingAbility)
        {
            _abilityAnimation?.Update();

            if (_abilityAnimation != null && _abilityAnimation.ActiveFrame == 3 && !_abilityHitTriggered)
            {
                _abilityHitTriggered = true;
                _castingAbility?.PerformHit(this);
            }

            if (_abilityAnimation != null && _abilityAnimation.isFinished)
            {
                _isCastingAbility = false;
                _castingAbility = null;
            }
        }
        else
        {
            Move(_moveInput, gameTime);
            SyncColliderToPosition();
        }

        bool buffsChanged = false;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _hurtCooldown = TimerHelper.DecreaseTimer(_hurtCooldown, dt);
        _itemActionCooldown = TimerHelper.DecreaseTimer(_itemActionCooldown, dt);
        _greenGlowTimer = TimerHelper.DecreaseTimer(_greenGlowTimer, dt);
        _dashCooldown = TimerHelper.DecreaseTimer(_dashCooldown, dt);
        _teleportCooldown = TimerHelper.DecreaseTimer(_teleportCooldown, dt);

        if (_speedBuffTimer > 0f) {
            _speedBuffTimer -= dt;
            if (_speedBuffTimer <= 0f) buffsChanged = true;
        }
        if (_dmgBuffTimer > 0f) {
            _dmgBuffTimer -= dt;
            if (_dmgBuffTimer <= 0f) buffsChanged = true;
        }
        if (_defBuffTimer > 0f) {
            _defBuffTimer -= dt;
            Heal(AdrenalineRegenRate * dt);
            if (_defBuffTimer <= 0f) buffsChanged = true;
        }

        if (buffsChanged) UpdateStats();

        bool moving = _moveInput != Vector2.Zero && !_isCastingAbility;
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

        // --- MAJOR ACTIVE ABILITIES LOGIC ---
        ActiveAbility?.Update(this, gameTime);

        if (_inputManager is not null && ActiveAbility != null && ActiveAbility.CanExecute() && !_isCastingAbility)
        {
            if (_inputManager.IsGameplayKeyPress(KeybindId.Ability1))
            {
                ActiveAbility.Execute(this);
            }
        }

        if (_inputManager is not null)
        {
            _timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
            if (_inputManager.IsGameplayKeyPress(KeybindId.Attack) && _timeSinceLastAttack >= CurrentHaste && !_isCastingAbility)
            {
                UseWeapon();
                AudioManager.PlaySfx(_attackSound);
                _timeSinceLastAttack = 0;
            }

            // place item at feet
            if (_inputManager.IsGameplayKeyPress(KeybindId.PlaceItem) && _itemActionCooldown <= 0f && !_isCastingAbility)
            {
                ItemSystem.PlaceSelectedItem(this);
                _itemActionCooldown = ItemActionCooldown;
            }

            // throw item toward mouse
            if (_inputManager.IsGameplayKeyPress(KeybindId.ThrowItem) && _itemActionCooldown <= 0f && !_isCastingAbility)
            {
                ItemSystem.ThrowSelectedItemTowardMouse(this);
                _itemActionCooldown = ItemActionCooldown;
            }

            if (_inputManager.IsGameplayKeyPress(KeybindId.Dash) && _dashCooldown <= 0f && !_isCastingAbility)
            {
                Vector2 dashDirection = _moveInput != Vector2.Zero ? _moveInput : _lastMoveDirection;
                if (dashDirection != Vector2.Zero)
                {
                    Dash(dashDirection, _DashDistance);
                    SetWalkRowFromDirection(dashDirection);
                    _dashCooldown = DashCooldown;
                }
            }

            if (_inputManager.IsGameplayKeyPress(KeybindId.Teleport) && _teleportCooldown <= 0f && !_isCastingAbility)
            {
                if (Teleport())
                    _teleportCooldown = TeleportCooldownDuration;
            }
        }

        base.Update(gameTime);
    }

    public void HitFrontalArea(Vector2 direction, float range, int damage, float stunDuration)
    {
        Vector2 center = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        HitFrontalArea(center, direction, range, damage, stunDuration);
    }

    public void HitFrontalArea(Vector2 center, Vector2 direction, float range, int damage, float stunDuration)
    {
        var gm = GameManager.GetGameManager();

        foreach (var obj in gm._gameObjects.ToList())
        {
            if (obj is BaseEnemy enemy)
            {
                var collider = enemy.GetCollider();
                if (collider != null)
                {
                    Vector2 toEnemy = collider.GetBoundingBox().Center.ToVector2() - center;
                    if (toEnemy.Length() <= range)
                    {
                        toEnemy.Normalize();
                        // 90-degree cone check
                        if (Vector2.Dot(direction, toEnemy) > 0.5f)
                        {
                            enemy.Damage(damage);
                            if (stunDuration > 0f) enemy.ApplyStun(stunDuration);
                        }
                    }
                }
            }
        }
    }

    public void UseWeapon()
    {
        Vector2 castAnchor = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset);

        Vector2 direction;
        if (KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly && _aimInput != Vector2.Zero)
        {
            direction = _aimInput;
            direction.Normalize();
        }
        else
        {
            Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
            direction = mousePosition - castAnchor;
            if (direction == Vector2.Zero)
                return;
            direction.Normalize();
        }

        Vector2 slashOrigin = castAnchor + direction * SlashDistance;
        _Weapon.Attack(direction, slashOrigin, CurrentDamage, CurrentCritChance);

        // --- ON ATTACK PROCS ---
        GameManager gm = GameManager.GetGameManager();
        if (HasFlowingStrikes && gm.RNG.NextDouble() < ProcChance)
        {
            _speedBuffTimer = BuffDurationSeconds;
            UpdateStats();
        }
        
        if (HasBloodlust && gm.RNG.NextDouble() < ProcChance)
        {
            _dmgBuffTimer = BuffDurationSeconds;
            UpdateStats();
        }
        
        if (HasDeepWounds && gm.RNG.NextDouble() < ProcChance)
        {
            // Instantly simulate a true-damage bleed burst in a wider frontal area
            // (Will interact with ArmorPen passively if/when full armor systems are finalized)
            HitFrontalArea(direction, SlashDistance * 1.5f, 15 + (ArmorPenLevel * 5), 0f); 
        }
    }

    public void FireRadialSlashes()
    {
        Vector2 castAnchor = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset);
        for (int i = 0; i < 8; i++) // 8 directions
        {
            float angle = i * MathHelper.TwoPi / 8f;
            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 slashOrigin = castAnchor + dir * SlashDistance;
            _Weapon.Attack(dir, slashOrigin, BaseDamage, BaseCritChance);
        }
    }

    public void HitRadialArea(float radius, int damage, float stunDuration)
    {
        var gm = GameManager.GetGameManager();
        Vector2 center = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);

        foreach (var obj in gm._gameObjects.ToList())
        {
            if (obj is BaseEnemy enemy)
            {
                var collider = enemy.GetCollider();
                if (collider != null)
                {
                    Vector2 toEnemy = collider.GetBoundingBox().Center.ToVector2() - center;
                    if (toEnemy.Length() <= radius)
                    {
                        enemy.Damage(damage);
                        if (stunDuration > 0f) enemy.ApplyStun(stunDuration);
                    }
                }
            }
        }
    }

    public void SetSpinningAnimation(GameTime gameTime)
    {
        _walkRow = (int)(gameTime.TotalGameTime.TotalMilliseconds / 50) % 4;
    }

    public Vector2 GetCastAnchor()
    {
        return _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset);
    }

    private Vector2 GetAbilityCastAnchor()
    {
        return _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
    }

    private Vector2 GetAbilityCastAnchorForAbility(BaseAbility ability)
    {
        if (ability is AxeSlamAbility)
            return GetCastAnchor();

        if (ability is ShieldSlamAbility)
            return _position;

        return GetAbilityCastAnchor();
    }

    public void ResetAttackTimer()
    {
        _timeSinceLastAttack = 0;
    }

    public void PlayAttackSound()
    {
        AudioManager.PlaySfx(_attackSound);
    }

    // --- SKILL TREE INTEGRATION ---
    public void ApplyNodeEffect(NodeEffect effect)
    {
        switch (effect.EffectId)
        {
            case "haste_percent":
                HasteLevel++;
                break;
            case "base_damage":
                DmgLevel++;
                break;
            case "max_hp":
                Heal(effect.ValuePerPoint); // Keep it simple for now, can modify MaxHp property later if created
                break;
            case "unlock_sword_stance":
                IsSwordActive = true; DualWieldUnlocked = true; IsAxeActive = false; IsShieldActive = false;
                break;
            case "unlock_axe_stance":
                IsAxeActive = true; IsSwordActive = false; IsShieldActive = false; DualWieldUnlocked = false;
                break;
            case "unlock_shield_stance":
                IsShieldActive = true; IsSwordActive = false; IsAxeActive = false; DualWieldUnlocked = false;
                break;
            case "proc_flowing_strikes":
                HasFlowingStrikes = true;
                break;
            case "proc_bloodlust":
                HasBloodlust = true;
                break;
            case "proc_adrenaline_rush":
                HasAdrenalineRush = true;
                break;
            case "unlock_whirlwind":
                WhirlwindUnlocked = true;
                ActiveAbility = new WhirlwindAbility();
                break;
            case "unlock_axe_slam":
                AxeSlamUnlocked = true;
                ActiveAbility = new AxeSlamAbility();
                break;
            case "unlock_shield_slam":
                ShieldSlamUnlocked = true;
                ActiveAbility = new ShieldSlamAbility();
                break;
            case "dodge_chance":
                DodgeLevel++;
                break;
            case "armor_pen":
                ArmorPenLevel++;
                break;
            case "block_chance":
                BlockLevel++;
                break;
            case "proc_elusive_rhythm":
                HasElusiveRhythm = true;
                break;
            case "proc_deep_wounds":
                HasDeepWounds = true;
                break;
            case "proc_bulwark":
                HasBulwark = true;
                break;
        }
        UpdateStats();
    }

    public void RevertAllSkillStats()
    {
        HasteLevel = 0;
        DmgLevel = 0;
        
        IsSwordActive = false;
        IsAxeActive = false;
        IsShieldActive = false;
        DualWieldUnlocked = false;
        
        HasFlowingStrikes = false;
        HasBloodlust = false;
        HasAdrenalineRush = false;
        
        WhirlwindUnlocked = false;
        AxeSlamUnlocked = false;
        ShieldSlamUnlocked = false;
        ActiveAbility = null;
        
        DodgeLevel = 0;
        ArmorPenLevel = 0;
        BlockLevel = 0;
        HasElusiveRhythm = false;
        HasDeepWounds = false;
        HasBulwark = false;

        CurrentMaxHp = BaseMaxHp + _levelHpBonus;
        CurrentCritChance = BaseCritChance + _levelCritBonus;
        CurrentSpeed = BaseSpeed + _levelSpeedBonus;
        // Damage and Haste restored via UpdateStats below

        UpdateStats();
    }

    public void UpdateStats()
    {
        // Damage Calculations
        CurrentDamage = (int)(BaseDamage + _levelDamageBonus) + DmgLevel * 5;
        if (IsSwordActive)
        {
            CurrentDamage = (int)(CurrentDamage * 0.8f);
        }
        else if (IsAxeActive)
        {
            CurrentDamage = (int)(CurrentDamage * 1.5f);
        }
        if (_dmgBuffTimer > 0f)
        {
            CurrentDamage = (int)(CurrentDamage * 1.5f); // 50% damage boost from bloodlust buff
        }

        // Haste Calculations (lower = faster attacks; level bonus reduces cooldown)
        float baseHasteForStance = IsAxeActive ? 1.2f : IsSwordActive ? 0.5f : BaseHaste;
        CurrentHaste = baseHasteForStance - _levelHasteBonus - HasteLevel * 0.1f;
        if (_speedBuffTimer > 0f)
            CurrentHaste *= 0.5f;
        CurrentHaste = Math.Max(0.1f, CurrentHaste);
    }

    protected override void OnLevelUp()
    {
        base.OnLevelUp();
        CurrentMaxHp += LevelStatBonus;
        CurrentCritChance = Math.Min(1f, CurrentCritChance + LevelStatBonus);
        CurrentSpeed += LevelStatBonus;
        UpdateStats(); // picks up _levelDamageBonus and _levelHasteBonus
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var warriorSource = new Rectangle(_walkFrameIndex * FrameSize, _walkRow * FrameSize, FrameSize, FrameSize);
        
        Color drawColor = DrawTint;
        if (_greenGlowTimer > 0f)
        {
            drawColor = Color.Lerp(drawColor, Color.LimeGreen, 0.5f);
        }

        Color freezeTint = new Color(150, 220, 255);
        float freezeIntensity = IsStunned ? (0.35f + 0.65f * StunVisualProgress) : 0f;
        if (freezeIntensity > 0f)
            drawColor = Color.Lerp(drawColor, freezeTint, freezeIntensity);

        Color equipmentColor = freezeIntensity > 0f
            ? Color.Lerp(Color.White, freezeTint, freezeIntensity)
            : Color.White;
        Color offHandColor = freezeIntensity > 0f
            ? Color.Lerp(Color.LightGray, freezeTint, freezeIntensity)
            : Color.LightGray;
        
        Vector2 center = _position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        Vector2 weaponOrigin = new Vector2(FrameSize * 0.5f, FrameSize * 0.5f);
        
        // Dynamic horizontal offset from the center of the Warrior
        float handOffsetX = 28f;
        Vector2 rightHand = center + new Vector2(handOffsetX, 0);
        Vector2 leftHand = center + new Vector2(-handOffsetX, 0);

        Texture2D activeTexture = IsSwordActive ? SwordSprite : AxeSprite;
        Rectangle weaponSource = GetWeaponSourceRect();
        SpriteEffects weaponFlip = GetAxeSpriteEffects();

        Rectangle shieldSource = new Rectangle(0, 0, FrameSize, FrameSize); // Default Down (1st 32x32)
        float shieldScale = AxeDrawScale * 0.85f; // A tiny bit smaller
        
        Vector2 shieldPos;
        Vector2 weaponPos;

        if (_walkRow == 3) // Left
        {
            shieldPos = leftHand;
            weaponPos = leftHand;
        }
        else if (_walkRow == 2) // Right
        {
            shieldPos = rightHand;
            weaponPos = rightHand;
        }
        else // Up or Down
        {
            shieldPos = rightHand;
            weaponPos = leftHand;
        }

        if (IsShieldActive)
        {
            if (_walkRow == 3) // Left
            {
                shieldSource = new Rectangle(FrameSize * 1, 0, FrameSize, FrameSize); // 2nd 32x32
            }
            else if (_walkRow == 2) // Right
            {
                shieldSource = new Rectangle(FrameSize * 2, 0, FrameSize, FrameSize); // 3rd 32x32
            }
            else if (_walkRow == 1) // Up
            {
                shieldSource = new Rectangle(0, FrameSize * 1, FrameSize, FrameSize); // 1st 32x32 out of row 2
            }

            // Draw shield behind the player for Up, Left, and Right
            if (_walkRow != 0)
            {
                spriteBatch.Draw(ShieldSprite, shieldPos, shieldSource, equipmentColor, 0f, weaponOrigin, shieldScale, SpriteEffects.None, 0f);
            }
        }

        bool isBehindAbilityCasting = _isCastingAbility && (_castingAbility is ShieldSlamAbility || _castingAbility is AxeSlamAbility);
        if (isBehindAbilityCasting && _abilityAnimation != null && _abilitySprite != null)
        {
            Rectangle abilitySource = _abilityAnimation.GetSourceRect();
            float rotation = _abilityRotate ? _abilityRotation : 0f;
            spriteBatch.Draw(_abilitySprite, _abilityDrawPos, abilitySource, drawColor, rotation, _abilityOrigin, _abilityDrawScale, SpriteEffects.None, 0f);
        }

        spriteBatch.Draw(WarriorSprite, _position, warriorSource, drawColor, 0f, Vector2.Zero, WarriorDrawScale, SpriteEffects.None, 0f);

        if (IsShieldActive && _walkRow == 0)
        {
            // Draw shield in front of the player for Down
            spriteBatch.Draw(ShieldSprite, shieldPos, shieldSource, equipmentColor, 0f, weaponOrigin, shieldScale, SpriteEffects.None, 0f);
        }

        if (_isCastingAbility && _abilityAnimation != null && _abilitySprite != null && !isBehindAbilityCasting)
        {
            Rectangle abilitySource = _abilityAnimation.GetSourceRect();
            float rotation = _abilityRotate ? _abilityRotation : 0f;
            spriteBatch.Draw(_abilitySprite, _abilityDrawPos, abilitySource, drawColor, rotation, _abilityOrigin, _abilityDrawScale, SpriteEffects.None, 0f);
        }

        if (IsSwordActive && DualWieldUnlocked)
        {
            if (_walkRow == 3 || _walkRow == 2) // Left or Right
            {
                Vector2 sidePos = _walkRow == 3 ? leftHand : rightHand;
                Vector2 offsetPos = sidePos + new Vector2(_walkRow == 3 ? 8f : -8f, -4f);
                SpriteEffects swordFlip = weaponFlip ^ SpriteEffects.FlipHorizontally;

                spriteBatch.Draw(activeTexture, offsetPos, weaponSource, offHandColor, 0f, weaponOrigin, AxeDrawScale, swordFlip, 0f);
                spriteBatch.Draw(activeTexture, sidePos, weaponSource, equipmentColor, 0f, weaponOrigin, AxeDrawScale, swordFlip, 0f);
            }
            else // Up or Down
            {
                Rectangle upDownSource = new Rectangle(FrameSize * 2, 0, FrameSize, FrameSize);
                spriteBatch.Draw(activeTexture, rightHand, upDownSource, equipmentColor, 0f, weaponOrigin, AxeDrawScale, weaponFlip, 0f);
                spriteBatch.Draw(activeTexture, leftHand, upDownSource, equipmentColor, 0f, weaponOrigin, AxeDrawScale, weaponFlip, 0f);
            }
        }
        else if (IsShieldActive)
        {
            spriteBatch.Draw(activeTexture, weaponPos, weaponSource, equipmentColor, 0f, weaponOrigin, AxeDrawScale, weaponFlip, 0f);
        }
        else
        {
            Vector2 singleWeaponPos = _walkRow == 3 ? leftHand : rightHand;
            float drawScale = IsAxeActive ? AxeDrawScale * 1.3f : AxeDrawScale;
            spriteBatch.Draw(activeTexture, singleWeaponPos, weaponSource, equipmentColor, 0f, weaponOrigin, drawScale, weaponFlip, 0f);
        }

        if (DebugDrawHitbox && _collider is not null)
            HitboxHelper.DrawHitbox(spriteBatch, _collider.shape, Color.LimeGreen);

        DrawAimArrow(spriteBatch);
        base.Draw(gameTime, spriteBatch);
    }

    private Rectangle GetWeaponSourceRect()
    {
        bool horizontal = _walkRow == 2 || _walkRow == 3;
        if (horizontal)
            return new Rectangle(FrameSize, 0, FrameSize, FrameSize);

        return new Rectangle(0, FrameSize, FrameSize, FrameSize);
    }

    private SpriteEffects GetAxeSpriteEffects()
    {
        if (_walkRow == 0) // Down
            return SpriteEffects.FlipVertically;

        bool horizontal = _walkRow == 2 || _walkRow == 3;
        if (horizontal && _facingLeft)
            return SpriteEffects.FlipHorizontally;

        return SpriteEffects.None;
    }

    public override void OnCollision(GameObject other)
    {
        if (other is not BaseEnemy enemy || _hurtCooldown > 0f)
            return;

        _hurtCooldown = EnemyContactHurtInterval;
        //_hurtCooldown = enemy is Troll ? 1f : EnemyContactHurtInterval;

        float damageToTake = EnemyContactDamage;
        if (enemy is Boss) 
        {
            damageToTake *= 2;
        }

        Damage(damageToTake);
    }

    public override void Damage(float amount)
    {
        GameManager gm = GameManager.GetGameManager();
        
        // --- DODGE LOGIC ---
        if (DodgeLevel > 0 && gm.RNG.NextDouble() < (DodgeLevel * 0.05)) // 5% per point
        {
            if (HasElusiveRhythm)
            {
                _speedBuffTimer = BuffDurationSeconds;
                UpdateStats();
            }
            return; // Completely evade the attack
        }

        // --- BLOCK LOGIC ---
        if (BlockLevel > 0 && gm.RNG.NextDouble() < (BlockLevel * 0.10)) // 10% per point
        {
            amount *= 0.5f; // Mitigate half damage
            if (HasBulwark) Heal(5f);
        }

        // --- ON HIT TAKEN PROCS ---
        if (HasAdrenalineRush)
        {
            _defBuffTimer = BuffDurationSeconds;
            // Auto-heal logic handles in update loop when timer > 0
        }

        _currentHp -= amount;
        TriggerHurtFlash();
        CheckDeath();
    }

    public void StartAbilityAnimation(BaseAbility ability)
    {
        _isCastingAbility = true;
        _abilityHitTriggered = false;
        _castingAbility = ability;

        bool isShieldSlam = ability is ShieldSlamAbility;
        if (isShieldSlam)
        {
            _abilitySprite = ShieldSlamSprite ?? WarriorAbilitySprite;
            _abilityAimDir = new Vector2(1f, 0f);
            _abilityRotation = 0f;
            _abilityRotate = false;
            _abilityDrawPos = _position;
            _abilityOrigin = Vector2.Zero;
            _abilityDrawScale = AbilityDrawScale * 1.25f;

            _abilityAnimation = new AnimationManager(
                AbilityFrames,
                AbilityColumns,
                new Vector2(FrameSize, FrameSize),
                8, // interval
                false, // loop
                0,
                ShieldSlamSheetRow * FrameSize
            );

            return;
        }

        _abilitySprite = WarriorAbilitySprite;

        Vector2 castAnchor = GetAbilityCastAnchorForAbility(ability);
        _abilityAimDir = GetAbilityAimDirection(castAnchor);
        _abilityRotation = (float)Math.Atan2(_abilityAimDir.Y, _abilityAimDir.X) + MathHelper.PiOver2;
        _abilityRotate = true;
        _abilityDrawPos = castAnchor;
        _abilityOrigin = new Vector2(FrameSize * 0.5f, FrameSize);
        _abilityDrawScale = ability is AxeSlamAbility ? AbilityDrawScale * 1.15f : AbilityDrawScale;

        _abilityAnimation = new AnimationManager(
            AbilityFrames,
            AbilityColumns,
            new Vector2(FrameSize, FrameSize),
            8, // interval
            false, // loop
            0,
            AxeAbilityRow * FrameSize
        );
    }

    public Vector2 GetAbilityAimDirection() => _abilityAimDir;

    private Vector2 GetAbilityAimDirection(Vector2 castAnchor)
    {
        Vector2 direction;
        if (KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly)
        {
            direction = _aimInput != Vector2.Zero ? _aimInput : _lastMoveDirection;
        }
        else
        {
            Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
            direction = mousePosition - castAnchor;
        }

        if (direction == Vector2.Zero)
        {
            direction = _lastMoveDirection != Vector2.Zero ? _lastMoveDirection : new Vector2(1f, 0f);
        }
        else
        {
            direction.Normalize();
        }

        return direction;
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
