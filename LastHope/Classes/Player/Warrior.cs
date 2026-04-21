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

    private double timeSinceLastAttack = 0;
    private float _dashCooldown;
    private float _teleportCooldown;
    private Vector2 _moveInput;
    private bool _facingLeft;
    private RectangleCollider _collider;
    private float _hurtCooldown;

    private const float SlashDistance = 105f;
    private const float SlashCastHeightOffset = 10f;

    private const float BombThrowSpeed = 520f;
    private const float BombActionCooldown = 0.25f;
    private float _bombActionCooldown;

    private const float DecoyThrowSpeed = 420f;

    private float _greenGlowTimer = 0f;
    private SoundEffect _deathSound;
    private SoundEffect _attackSound;

    // --- Skill Tree States ---
    public bool DualWieldUnlocked { get; set; }
    public int HasteLevel { get; set; }
    public int CritLevel { get; set; }
    public int DmgLevel { get; set; }
    public bool WhirlwindUnlocked { get; set; }
    private float _currentAttackCooldown = 0.7f;
    private float _whirlwindCooldownTimer = 0f;
    private float _whirlwindDurationTimer = 0f;
    private float _whirlwindTickTimer = 0f;

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

        bool moving = _moveInput != Vector2.Zero;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
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

        // --- WHIRLWIND ACTIVE ABILITY LOGIC ---
        if (WhirlwindUnlocked)
        {
            if (_whirlwindCooldownTimer > 0f) _whirlwindCooldownTimer -= dt;
            
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
            else if (_inputManager.IsKeyPress(Keys.H) && _whirlwindCooldownTimer <= 0f)
            {
                _whirlwindDurationTimer = 2.0f; // Spin for 2 seconds
                _whirlwindCooldownTimer = 5.0f; // 5 second cooldown
                _whirlwindTickTimer = 0f;
            }
        }

        if (_inputManager is not null)
        {
            timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
            if (_inputManager.IsGameplayKeyPress(KeybindId.Attack) && timeSinceLastAttack >= _currentAttackCooldown)
            {
                UseWeapon();
                // _attackSound.Play();
                timeSinceLastAttack = 0;
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
            case "move_speed_modifier":
                _Speed += effect.ValuePerPoint;
                break;
            case "unlock_bleed_modifier":
                // Set future bleed flag
                break;
            case "block_chance":
                // Set future block flag
                break;
        }
        UpdateStats();
    }

    public void RevertAllSkillStats()
    {
        HasteLevel = 0;
        DmgLevel = 0;
        CritLevel = 0;
        _Speed = 220f; // Revert to base speed
        UpdateStats();
    }

    public void UpdateStats()
    {
        // Calculate new modifiers incrementally
        float newCrit = 0.1f + (CritLevel * 0.1f); // Up to 40%
        int newDmg = 20 + (DmgLevel * 5); // Up to 35
        if (DualWieldUnlocked) newDmg = (int)(newDmg * 1.05f); // +5% base damage buff
        
        _Weapon = new Weapon("Sword", damage: newDmg, critChance: newCrit);

        _currentAttackCooldown = 0.7f;
        if (DualWieldUnlocked) _currentAttackCooldown *= 0.9f; // Global 10% faster attack
        _currentAttackCooldown -= (HasteLevel * 0.1f); // Linear increase per point
        if (_currentAttackCooldown < 0.1f) _currentAttackCooldown = 0.1f;
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

        Rectangle axeSource = GetAxeSourceRect();
        var axeFlip = GetAxeSpriteEffects();
        float axeW = _axePixelSize;
        float y = AxeOffsetY;
        
        Vector2 leftAxeOffset = new Vector2(40f, y);
        Vector2 rightAxeOffset = new Vector2(_bodyWidth - axeW - 40f, y);

        if (DualWieldUnlocked)
        {
            // Draw Two Axes
            spriteBatch.Draw(AxeSprite, Position + leftAxeOffset, axeSource, Color.White, 0f, Vector2.Zero, AxeDrawScale, SpriteEffects.None, 0f);
            spriteBatch.Draw(AxeSprite, Position + rightAxeOffset, axeSource, Color.White, 0f, Vector2.Zero, AxeDrawScale, SpriteEffects.FlipHorizontally, 0f);
        }
        else
        {
            // Draw Original single Axe
            Vector2 axeOffset = _facingLeft ? rightAxeOffset : leftAxeOffset;
            spriteBatch.Draw(AxeSprite, Position + axeOffset, axeSource, Color.White, 0f, Vector2.Zero, AxeDrawScale, axeFlip, 0f);
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
                _deathSound.Play();
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
