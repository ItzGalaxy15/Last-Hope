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
using System.Linq;

namespace Last_Hope;

public class Warrior : BasePlayer
{
    public Vector2 Position { get; private set; }
    public Texture2D AxeSprite;
    public Texture2D WarriorSprite;
    public InputManager _inputManager { get; private set; }

    private const int FrameSize = 32;
    private const float WarriorDrawScale = 3f;
    private const float AxeDrawScale = 1.8f;
    private const float WalkFrameDuration = 0.12f;

    private int _walkRow;
    private int _walkFrameIndex;
    private float _walkFrameTimer;
    private float _bodyWidth => FrameSize * WarriorDrawScale;
    private float _axePixelSize => FrameSize * AxeDrawScale;
    private float AxeOffsetY => (_bodyWidth - _axePixelSize) * 0.5f;

    private const float DashCooldown = 0.75f;
    private const float TeleportCooldownDuration = 60f;
    private const float TeleportEnemyClearance = 160f;
    private const float EnemyContactDamage = 10f;
    private const float EnemyContactHurtInterval = 0.5f;
    private const bool DebugDrawHitbox = false;

    // --- TIMERS & COOLDOWNS ---
    private double _timeSinceLastAttack = 0;
    private float _currentAttackCooldown = 0.7f;
    private float _dashCooldown;
    private float _teleportCooldown;
    private float _hurtCooldown;
    private float _bombActionCooldown;
    private float _greenGlowTimer = 0f;
    private float _whirlwindDurationTimer = 0f;
    private float _whirlwindTickTimer = 0f;
    private float _abilityCooldownTimer = 0f; // Shared cooldown logic for major active abilities
    private float _speedBuffTimer = 0f;
    private float _dmgBuffTimer = 0f;
    private float _defBuffTimer = 0f;

    // --- STATE & PHYSICS ---
    private Vector2 _moveInput;
    private bool _facingLeft;
    private RectangleCollider _collider;

    private const float SlashDistance = 105f;
    private const float SlashCastHeightOffset = 10f;

    // --- COMBAT & SKILL CONFIGURATION ---
    private const float MajorAbilityCooldown = 8.0f;
    private const float AxeSlamDamageMultiplier = 3.0f;
    private const float AxeSlamRange = 140f;
    private const float ShieldSlamDamageMultiplier = 1.5f;
    private const float ShieldSlamStunDuration = 3.0f;
    private const float ShieldSlamRange = 100f;
    
    private const float BuffDurationSeconds = 10.0f;
    private const double ProcChance = 0.10;
    private const float AdrenalineRegenRate = 5.0f;

    private const float BombThrowSpeed = 520f;
    private const float BombActionCooldown = 0.25f;

    private const float DecoyThrowSpeed = 420f;
    private SoundEffect _deathSound;
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

    public Warrior(Vector2 startPosition)
        : base(maxHp: 100f, weapon: new Weapon("Sword", damage: 20, critChance: 0.1f), speed: 220f, level: 0, experience: 0, dashDistance: 140f)
    {
        Position = startPosition;
        var origin = new Point((int)startPosition.X, (int)startPosition.Y);
        _collider = new RectangleCollider(new Rectangle(origin, Point.Zero));
        SetCollider(_collider);
        Inventory = new ItemType[2] { ItemType.Bomb, ItemType.Decoy };
    }

    public override Vector2 GetPosition()
    {
        return Position;
    }

    public void Move(Vector2 direction, GameTime gameTime)
    {
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 velocity = direction * _Speed * dt;

        // --- X movement ---
        Vector2 newPosX = new Vector2(Position.X + velocity.X, Position.Y);
        if (!WouldCollideAt(newPosX))
        {
            Position = newPosX;
        }

        // --- Y movement ---
        Vector2 newPosY = new Vector2(Position.X, Position.Y + velocity.Y);
        if (!WouldCollideAt(newPosY))
        {
            Position = newPosY;
        }

        ClampToMapBounds();
    }

    private void ClampToMapBounds()
    {
        var grid = GameManager.GetGameManager().NavigationGrid;
        if (grid == null)
            return;

        float mapW = grid.WidthInTiles * grid.TileSize;
        float mapH = grid.HeightInTiles * grid.TileSize;

        Position = new Vector2(
            MathHelper.Clamp(Position.X, 0f, mapW - _bodyWidth),
            MathHelper.Clamp(Position.Y, 0f, mapH - _bodyWidth)
        );
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        AxeSprite = content.Load<Texture2D>("AxeSheet");
        try { SwordSprite = content.Load<Texture2D>("SwordSheet"); } catch { SwordSprite = AxeSprite; } // Fallback to avoid crashes
        try { ShieldSprite = content.Load<Texture2D>("ShieldSheet"); } catch { ShieldSprite = AxeSprite; } // Fallback to avoid crashes
        
        WarriorSprite = content.Load<Texture2D>("WarriorSheet");
        _deathSound = content.Load<SoundEffect>("sounds/Death sound");
        _attackSound = content.Load<SoundEffect>("sounds/Warrior Attack");
        _inputManager = GameManager.GetGameManager().InputManager;

        SyncColliderToPosition();
        SetCollider(_collider);
        ClampToMapBounds();
        SyncColliderToPosition();
    }

    public override void HandleInput(InputManager inputManager)
    {
        _moveInput = Vector2.Zero;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveUp) || inputManager.IsKeyDown(Keys.Up))
            _moveInput.Y -= 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveDown) || inputManager.IsKeyDown(Keys.Down))
            _moveInput.Y += 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveLeft) || inputManager.IsKeyDown(Keys.Left))
            _moveInput.X -= 1f;
        if (inputManager.IsGameplayKeyDown(KeybindId.MoveRight) || inputManager.IsKeyDown(Keys.Right))
            _moveInput.X += 1f;
    }

    public override void Update(GameTime gameTime)
    {
        if (!GameManager.GetGameManager().playerAlive || _currentHp <= 0f)
            return;

        Move(_moveInput, gameTime);
        SyncColliderToPosition();

        if (_hurtCooldown > 0f)
            _hurtCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_bombActionCooldown > 0f)
            _bombActionCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_greenGlowTimer > 0f)
            _greenGlowTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        bool buffsChanged = false;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

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

        if (_abilityCooldownTimer > 0f)
            _abilityCooldownTimer -= dt;

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

        // --- MAJOR ACTIVE ABILITIES LOGIC ---
        if (WhirlwindUnlocked)
        {
            if (_whirlwindDurationTimer > 0f)
            {
                _whirlwindDurationTimer -= dt;
                _whirlwindTickTimer -= dt;
                
                // Overwrite the walk animation to spin rapidly
                _walkRow = (int)(gameTime.TotalGameTime.TotalMilliseconds / 50) % 4; 
                
                if (_whirlwindTickTimer <= 0f)
                {
                    _whirlwindTickTimer = 0.15f; // Emits radial burst every 0.15s
                    FireRadialSlashes();
                }
            }
        }

        if (_inputManager is not null && _abilityCooldownTimer <= 0f)
        {
            if (_inputManager.IsGameplayKeyPress(KeybindId.Ability1))
            {
                if (WhirlwindUnlocked)
                {
                    _whirlwindDurationTimer = 2.0f;
                    _abilityCooldownTimer = MajorAbilityCooldown;
                    _whirlwindTickTimer = 0f;
                }
                else if (AxeSlamUnlocked)
                {
                    PerformAxeSlam();
                }
                else if (ShieldSlamUnlocked)
                {
                    PerformShieldSlam();
                }
            }
        }

        if (_inputManager is not null)
        {
            _timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
            if (_inputManager.IsGameplayKeyPress(KeybindId.Attack) && _timeSinceLastAttack >= _currentAttackCooldown)
            {
                UseWeapon();
                AudioManager.PlaySfx(_attackSound);
                _timeSinceLastAttack = 0;
            }

            // G = place bomb at feet
            if (_inputManager.IsGameplayKeyPress(KeybindId.PlaceItem) && _bombActionCooldown <= 0f)
            {
                PlaceSelectedItem();
                _bombActionCooldown = BombActionCooldown;
            }

            // T = throw bomb toward mouse
            if (_inputManager.IsGameplayKeyPress(KeybindId.ThrowItem) && _bombActionCooldown <= 0f)
            {
                ThrowSelectedItemTowardMouse();
                _bombActionCooldown = BombActionCooldown;
            }

            if (_dashCooldown > 0f)
                _dashCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_inputManager.IsGameplayKeyPress(KeybindId.Dash) && _dashCooldown <= 0f)
            {
                Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
                Vector2 towardMouse = mousePosition - Position;
                if (towardMouse != Vector2.Zero)
                {
                    Dash(towardMouse, _DashDistance);
                    SetWalkRowFromDirection(towardMouse);
                    _dashCooldown = DashCooldown;
                }
            }

            if (_teleportCooldown > 0f)
                _teleportCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_inputManager.IsKeyPress(Keys.R) && _teleportCooldown <= 0f)
            {
                if (Teleport())
                    _teleportCooldown = TeleportCooldownDuration;
            }
        }

        base.Update(gameTime);
    }

    private static void DrawHitbox(SpriteBatch spriteBatch, Rectangle rect, Color color)
    {
        Texture2D pixel = GameManager.GetGameManager().Pixel;
        const int thickness = 2;

        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }

    private void PerformAxeSlam()
    {
        _abilityCooldownTimer = MajorAbilityCooldown;
        Vector2 aimDir = GameManager.GetGameManager().GetWorldMousePosition() - Position;
        if (aimDir != Vector2.Zero) aimDir.Normalize();

        int damage = (int)(_Weapon.Damage * AxeSlamDamageMultiplier);
        HitFrontalArea(aimDir, AxeSlamRange, damage, 0f);

        AudioManager.PlaySfx(_attackSound);
        _timeSinceLastAttack = 0;
        _Weapon.Attack(aimDir, Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset));
    }

    private void PerformShieldSlam()
    {
        _abilityCooldownTimer = MajorAbilityCooldown;
        Vector2 aimDir = GameManager.GetGameManager().GetWorldMousePosition() - Position;
        if (aimDir != Vector2.Zero) aimDir.Normalize();

        int damage = (int)(_Weapon.Damage * ShieldSlamDamageMultiplier);
        HitFrontalArea(aimDir, ShieldSlamRange, damage, ShieldSlamStunDuration);

        AudioManager.PlaySfx(_attackSound);
        _timeSinceLastAttack = 0;
        _Weapon.Attack(aimDir, Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset));
    }

    private void HitFrontalArea(Vector2 direction, float range, int damage, float stunDuration)
    {
        var gm = GameManager.GetGameManager();
        Vector2 center = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);

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
        if (_inputManager is null)
            return;

        // Anchor at warrior body center, then lift upward.
        Vector2 castAnchor = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset);

        Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
        Vector2 direction = mousePosition - castAnchor;
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();

        Vector2 slashOrigin = castAnchor + direction * SlashDistance;
        _Weapon.Attack(direction, slashOrigin);

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

    private void FireRadialSlashes()
    {
        Vector2 castAnchor = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset);
        for (int i = 0; i < 8; i++) // 8 directions
        {
            float angle = i * MathHelper.TwoPi / 8f;
            Vector2 dir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            Vector2 slashOrigin = castAnchor + dir * SlashDistance;
            _Weapon.Attack(dir, slashOrigin);
        }
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
                break;
            case "unlock_axe_slam":
                AxeSlamUnlocked = true;
                break;
            case "unlock_shield_slam":
                ShieldSlamUnlocked = true;
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
        
        DodgeLevel = 0;
        ArmorPenLevel = 0;
        BlockLevel = 0;
        HasElusiveRhythm = false;
        HasDeepWounds = false;
        HasBulwark = false;

        _Speed = 220f; // Revert to base speed
        UpdateStats();
    }

    public void UpdateStats()
    {
        float newCrit = 0.1f;
        int newDmg = 20 + (DmgLevel * 5); // Up to 35
        
        if (IsSwordActive) newDmg = (int)(newDmg * 0.8f);
        else if (IsAxeActive) newDmg = (int)(newDmg * 1.5f);

        if (_dmgBuffTimer > 0f) newDmg = (int)(newDmg * 1.5f); // 50% damage boost from bloodlust buff
        
        _Weapon = new Weapon("Primary", damage: newDmg, critChance: newCrit);

        float baseCooldown = 0.7f;
        if (IsAxeActive) baseCooldown = 1.2f;
        else if (IsSwordActive) baseCooldown = 0.5f;

        baseCooldown -= (HasteLevel * 0.1f);
        if (_speedBuffTimer > 0f) baseCooldown *= 0.5f; // Double attack speed from flowing strikes buff

        _currentAttackCooldown = Math.Max(0.1f, baseCooldown);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var warriorSource = new Rectangle(_walkFrameIndex * FrameSize, _walkRow * FrameSize, FrameSize, FrameSize);
        
        Color drawColor = DrawTint;
        if (_greenGlowTimer > 0f)
        {
            drawColor = Color.Lerp(drawColor, Color.LimeGreen, 0.5f);
        }
        
        spriteBatch.Draw(WarriorSprite, Position, warriorSource, drawColor, 0f, Vector2.Zero, WarriorDrawScale, SpriteEffects.None, 0f);

        Vector2 center = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        Vector2 weaponOrigin = new Vector2(FrameSize * 0.5f, FrameSize * 0.5f);
        
        // Dynamic horizontal offset from the center of the Warrior
        float handOffsetX = 28f;
        Vector2 rightHand = center + new Vector2(handOffsetX, 0);
        Vector2 leftHand = center + new Vector2(-handOffsetX, 0);

        Texture2D activeTexture = IsSwordActive ? SwordSprite : AxeSprite;
        Rectangle weaponSource = GetAxeSourceRect();
        SpriteEffects weaponFlip = GetAxeSpriteEffects();

        if (IsSwordActive && DualWieldUnlocked)
        {
            spriteBatch.Draw(activeTexture, rightHand, weaponSource, Color.White, 0f, weaponOrigin, AxeDrawScale, SpriteEffects.FlipHorizontally, 0f);
            spriteBatch.Draw(activeTexture, leftHand, weaponSource, Color.White, 0f, weaponOrigin, AxeDrawScale, SpriteEffects.None, 0f);
        }
        else if (IsShieldActive)
        {
            Vector2 weaponPos = _facingLeft ? leftHand : rightHand;
            Vector2 shieldPos = _facingLeft ? rightHand : leftHand;
            
            spriteBatch.Draw(activeTexture, weaponPos, weaponSource, Color.White, 0f, weaponOrigin, AxeDrawScale, weaponFlip, 0f);
            
            Rectangle shieldSource = new Rectangle(0, 0, ShieldSprite.Width, ShieldSprite.Height);
            Vector2 shieldOrigin = new Vector2(ShieldSprite.Width * 0.5f, ShieldSprite.Height * 0.5f);
            SpriteEffects shieldFlip = _facingLeft ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            spriteBatch.Draw(ShieldSprite, shieldPos, shieldSource, Color.White, 0f, shieldOrigin, AxeDrawScale, shieldFlip, 0f);
        }
        else
        {
            Vector2 weaponPos = _facingLeft ? leftHand : rightHand;
            float drawScale = IsAxeActive ? AxeDrawScale * 1.3f : AxeDrawScale;
            spriteBatch.Draw(activeTexture, weaponPos, weaponSource, Color.White, 0f, weaponOrigin, drawScale, weaponFlip, 0f);
        }

        if (DebugDrawHitbox && _collider is not null)
            DrawHitbox(spriteBatch, _collider.shape, Color.LimeGreen);

        base.Draw(gameTime, spriteBatch);
    }

    private Rectangle GetAxeSourceRect()
    {
        bool horizontal = _walkRow == 2 || _walkRow == 3;
        if (horizontal)
            return new Rectangle(FrameSize, 0, FrameSize, FrameSize);

        return new Rectangle(0, FrameSize, FrameSize, FrameSize);
    }

    private SpriteEffects GetAxeSpriteEffects()
    {
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

        float damageToTake = EnemyContactDamage;
        if (enemy is Boss) 
        {
            damageToTake *= 2;
        }

        Damage(damageToTake);
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

    // Fraction of the body size used as the hitbox — tune this to adjust fairness.
    private const float HitboxFraction = 0.55f;

    private void SyncColliderToPosition()
    {
        if (_collider is null)
            return;

        float hitboxSize = _bodyWidth * HitboxFraction;
        float offset = (_bodyWidth - hitboxSize) / 2f;

        _collider.shape = new Rectangle(
            (int)(Position.X + offset),
            (int)(Position.Y + offset),
            (int)hitboxSize,
            (int)hitboxSize
        );
        SetCollider(_collider);
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

        if (_currentHp <= 0f)
        {
            if (ExtraLives > 0)
            {
                ExtraLives--;
                _currentHp = 0.1f; // Prevent death
                Heal(9999f); // Restore to Max HP
                _greenGlowTimer = 1.5f;
                _hurtCooldown = 1.5f; // Grant 1.5 seconds of invincibility to escape
            }
            else
            {
                _currentHp = 0f;
                AudioManager.PlaySfx(_deathSound);
                GameManager.GetGameManager().playerAlive = false;
                GameManager.GetGameManager()._state = GameState.GameOver;
            }
        }
    }

    protected override void ApplyDashOffset(Vector2 delta)
    {
        Position += delta;
        ClampToMapBounds();
        SyncColliderToPosition();
    }

    protected override void ApplyTeleportPosition(Vector2 newPosition)
    {
        Position = newPosition;
        ClampToMapBounds();
        SyncColliderToPosition();
    }

    protected override bool IsPositionSafe(Vector2 position)
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

    private void PlaceSelectedItem()
    {
        GameManager gm = GameManager.GetGameManager();
        Vector2 spawnPosition = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);

        ItemType[] inv = Inventory!;
        ItemType currentItem = inv[gm.SelectedItemSlot];
        if (currentItem == ItemType.None) return;

        if (currentItem == ItemType.Decoy)
        {
            SpawnDecoy(gm, spawnPosition, Vector2.Zero);
        }
        else if (currentItem == ItemType.Bomb)
        {
            gm.AddGameObject(new Bomb(spawnPosition, Vector2.Zero));
        }
        else if (currentItem == ItemType.HealingPotion)
        {
            Heal(50f);
        }
        else if (currentItem == ItemType.OneUp)
        {
            AddLife(1);
            _greenGlowTimer = 1.5f;
            gm.HasUsedOneUp = true;
        }
        
        inv[gm.SelectedItemSlot] = ItemType.None;
    }

    private void ThrowSelectedItemTowardMouse()
    {
        GameManager gm = GameManager.GetGameManager();
        Vector2 spawnPosition = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        Vector2 mouseWorld = gm.GetWorldMousePosition();
        Vector2 direction = mouseWorld - spawnPosition;
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();

        ItemType[] inv = Inventory!;
        ItemType currentItem = inv[gm.SelectedItemSlot];
        if (currentItem == ItemType.None) return;

        if (currentItem == ItemType.Decoy)
        {
            SpawnDecoy(gm, spawnPosition, direction * DecoyThrowSpeed);
        }
        else if (currentItem == ItemType.Bomb)
        {
            gm.AddGameObject(new Bomb(spawnPosition, direction * BombThrowSpeed));
        }
        else if (currentItem == ItemType.HealingPotion)
        {
            Heal(50f);
        }
        else if (currentItem == ItemType.OneUp)
        {
            AddLife(1);
            _greenGlowTimer = 1.5f;
            gm.HasUsedOneUp = true;
        }
        
        inv[gm.SelectedItemSlot] = ItemType.None;
    }

    private static void SpawnDecoy(GameManager gm, Vector2 spawnPosition, Vector2 initialVelocity)
    {
        if (gm.ActiveDecoy is not null)
            gm.RemoveGameObject(gm.ActiveDecoy);

        Decoy decoy = new Decoy(spawnPosition, initialVelocity, lifetimeSeconds: 5f);
        gm.AddGameObject(decoy);
        gm.ActiveDecoy = decoy;
    }

    protected override bool WouldCollideAt(Vector2 testPosition)
    {
        float hitboxSize = _bodyWidth * HitboxFraction;
        float offset = (_bodyWidth - hitboxSize) / 2f;

        Rectangle testRect = new Rectangle(
            (int)(testPosition.X + offset),
            (int)(testPosition.Y + offset),
            (int)hitboxSize,
            (int)hitboxSize
        );

        return CollisionWorld.CollidesWithStaticForMovement(testRect);
    }
}
