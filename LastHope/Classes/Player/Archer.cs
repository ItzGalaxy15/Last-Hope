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

namespace Last_Hope;

public class Archer : BasePlayer
{
    public Vector2 Position { get; private set; }
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
    private float _bodyWidth => FrameSize * ArcherDrawScale;
    private float _bowPixelSize => FrameSize * BowDrawScale;
    private float BowOffsetY => (_bodyWidth - _bowPixelSize) * 0.5f;

    private const float AttackCooldown = 0.7f;
    private const float DashCooldown = 0.75f;
    private const float EnemyContactDamage = 10f;
    private const float EnemyContactHurtInterval = 0.5f;
    private const bool DebugDrawHitbox = true;
    private const float HitboxFraction = 0.55f;
    private const float TeleportEnemyClearance = 160f;

    private double timeSinceLastAttack = 0;
    private float _dashCooldown;
    private Vector2 _moveInput;
    private bool _facingLeft;
    private RectangleCollider _collider;
    private float _hurtCooldown;

    // Bow attack animation
    private const int BowSheetColumns = 3;
    private const float BowDrawDuration = 0.35f;
    private bool _isDrawingBow;
    private float _bowDrawTimer;
    private Vector2 _bowAimDirection;

    private const float BombThrowSpeed = 520f;
    private const float BombActionCooldown = 0.25f;
    private float _bombActionCooldown;

    private float _greenGlowTimer;
    private SoundEffect _deathSound;

    private const float DecoyThrowSpeed = 420f;
    private const float ArrowSpeed = 600f;

    public Archer(Vector2 startPosition)
        : base(maxHp: 100f, weapon: new Bow("Bow", damage: 20, critChance: 1.0f, speed: 600f, owner: null), speed: 220f, level: 0, experience: 0, dashDistance: 140f)
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

        Vector2 newPosX = new Vector2(Position.X + velocity.X, Position.Y);
        if (!WouldCollideAt(newPosX))
            Position = newPosX;

        Vector2 newPosY = new Vector2(Position.X, Position.Y + velocity.Y);
        if (!WouldCollideAt(newPosY))
            Position = newPosY;

        ClampToMapBounds();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        BowSprite = content.Load<Texture2D>("Bow sheet");
        ArcherSprite = content.Load<Texture2D>("ArcherSheet");
        _deathSound = content.Load<SoundEffect>("sounds/Death sound");
        _inputManager = GameManager.GetGameManager().InputManager;
        _Weapon.SetOwner(this);

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
        if (moving)
        {
            SetWalkRowFromDirection(_moveInput);
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
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

        if (_inputManager is not null)
        {
            timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
            if (_inputManager.LeftMousePress() && timeSinceLastAttack >= AttackCooldown && !_isDrawingBow)
            {
                StartBowDraw();
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

    private void StartBowDraw()
    {
        if (_inputManager is null)
            return;

        Vector2 center = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
        Vector2 direction = mousePosition - center;
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();
        _bowAimDirection = direction;
        _isDrawingBow = true;
        _bowDrawTimer = 0f;
    }

    private void FireArrow()
    {
        Vector2 center = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);
        _Weapon.Attack(_bowAimDirection, center);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var archerSource = new Rectangle(_walkFrameIndex * FrameSize, _walkRow * FrameSize, FrameSize, FrameSize);
        Color drawColor = DrawTint;
        if (_greenGlowTimer > 0f)
            drawColor = Color.Lerp(drawColor, Color.LimeGreen, 0.5f);

        spriteBatch.Draw(ArcherSprite, Position, archerSource, drawColor, 0f, Vector2.Zero, ArcherDrawScale, SpriteEffects.None, 0f);

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

        spriteBatch.Draw(BowSprite, Position + bowOffset, bowSource, Color.White, 0f, Vector2.Zero, BowDrawScale, bowFlip, 0f);

        if (DebugDrawHitbox && _collider is not null)
            DrawHitbox(spriteBatch, _collider.shape, Color.LimeGreen);

        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
        if (other is not BaseEnemy enemy || _hurtCooldown > 0f)
            return;

        _hurtCooldown = EnemyContactHurtInterval;

        float damageToTake = EnemyContactDamage;
        if (enemy is Boss)
            damageToTake *= 2;

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
            (int)hitboxSize);
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
                _currentHp = 0.1f;
                Heal(9999f);
                _greenGlowTimer = 1.5f;
                _hurtCooldown = 1.5f;
            }
            else
            {
                _currentHp = 0f;
                _deathSound?.Play();
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

    private void PlaceSelectedItem()
    {
        GameManager gm = GameManager.GetGameManager();
        Vector2 spawnPosition = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f);

        ItemType[] inv = Inventory!;
        ItemType currentItem = inv[gm.SelectedItemSlot];
        if (currentItem == ItemType.None)
            return;

        if (currentItem == ItemType.Decoy)
            SpawnDecoy(gm, spawnPosition, Vector2.Zero);
        else if (currentItem == ItemType.Bomb)
            gm.AddGameObject(new Bomb(spawnPosition, Vector2.Zero));
        else if (currentItem == ItemType.HealingPotion)
            Heal(50f);
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
        if (currentItem == ItemType.None)
            return;

        if (currentItem == ItemType.Decoy)
            SpawnDecoy(gm, spawnPosition, direction * DecoyThrowSpeed);
        else if (currentItem == ItemType.Bomb)
            gm.AddGameObject(new Bomb(spawnPosition, direction * BombThrowSpeed));
        else if (currentItem == ItemType.HealingPotion)
            Heal(50f);
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
}
