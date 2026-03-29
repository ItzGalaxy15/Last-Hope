using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

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

    private const float AttackCooldown = 1f;
    private const float DashCooldown = 0.75f;
    private const float EnemyContactDamage = 10f;
    private const float EnemyContactHurtInterval = 0.5f;
    private const bool DebugDrawHitbox = true;

    private double timeSinceLastAttack = 0;
    private float _dashCooldown;
    private Vector2 _moveInput;
    private bool _facingLeft;
    private RectangleCollider _collider;
    private float _hurtCooldown;

    private const float SlashDistance = 80f;
    private const float SlashCastHeightOffset = 10f;

    public Warrior(Vector2 startPosition)
        : base(hp: 10f, weapon: new Weapon("Sword", damage: 20, critChance: 1.0f), speed: 220f, level: 0, experience: 0, dashDistance: 140f)
    {
        Position = startPosition;
        var origin = new Point((int)startPosition.X, (int)startPosition.Y);
        _collider = new RectangleCollider(new Rectangle(origin, Point.Zero));
        SetCollider(_collider);
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
        Position += direction * _Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        AxeSprite = content.Load<Texture2D>("AxeSheet");
        WarriorSprite = content.Load<Texture2D>("WarriorSheet");
        _inputManager = GameManager.GetGameManager().InputManager;

        SyncColliderToPosition();
        SetCollider(_collider);
    }

    public override void HandleInput(InputManager inputManager)
    {
        _moveInput = Vector2.Zero;
        if (inputManager.IsKeyDown(Keys.W) || inputManager.IsKeyDown(Keys.Up))
            _moveInput.Y -= 1f;
        if (inputManager.IsKeyDown(Keys.S) || inputManager.IsKeyDown(Keys.Down))
            _moveInput.Y += 1f;
        if (inputManager.IsKeyDown(Keys.A) || inputManager.IsKeyDown(Keys.Left))
            _moveInput.X -= 1f;
        if (inputManager.IsKeyDown(Keys.D) || inputManager.IsKeyDown(Keys.Right))
            _moveInput.X += 1f;
    }

    public override void Update(GameTime gameTime)
    {
        if (!GameManager.GetGameManager().playerAlive || _Hp <= 0f)
            return;

        Move(_moveInput, gameTime);
        SyncColliderToPosition();

        if (_hurtCooldown > 0f)
            _hurtCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

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

        if (_inputManager is not null)
        {
            timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
            if (_inputManager.IsKeyPress(Keys.B) && timeSinceLastAttack >= AttackCooldown)
            {
                UseWeapon();
                timeSinceLastAttack = 0;
            }

            if (_dashCooldown > 0f)
                _dashCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_inputManager.IsKeyPress(Keys.LeftShift) && _dashCooldown <= 0f)
            {
                Vector2 mousePosition = _inputManager.CurrentMouseState.Position.ToVector2();
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

    public void UseWeapon()
    {
        if (_inputManager is null)
            return;

        // Anchor at warrior body center, then lift upward.
        Vector2 castAnchor = Position + new Vector2(_bodyWidth * 0.5f, _bodyWidth * 0.5f - SlashCastHeightOffset);

        Vector2 mousePosition = _inputManager.CurrentMouseState.Position.ToVector2();
        Vector2 direction = mousePosition - castAnchor;
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();

        Vector2 slashOrigin = castAnchor + direction * SlashDistance;
        _Weapon.Attack(direction, slashOrigin);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var warriorSource = new Rectangle(_walkFrameIndex * FrameSize, _walkRow * FrameSize, FrameSize, FrameSize);
        spriteBatch.Draw(WarriorSprite, Position, warriorSource, Color.White, 0f, Vector2.Zero, WarriorDrawScale, SpriteEffects.None, 0f);

        Rectangle axeSource = GetAxeSourceRect();
        var axeFlip = GetAxeSpriteEffects();
        float axeW = _axePixelSize;
        float y = AxeOffsetY;
        Vector2 axeOffset = _facingLeft
            ? new Vector2(_bodyWidth - axeW - 40f, y)
            : new Vector2(40f, y);

        spriteBatch.Draw(AxeSprite, Position + axeOffset, axeSource, Color.White, 0f, Vector2.Zero, AxeDrawScale, axeFlip, 0f);

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
        if (other is not BaseEnemy || _hurtCooldown > 0f)
            return;

        _Hp -= EnemyContactDamage;
        _hurtCooldown = EnemyContactHurtInterval;
        if (_Hp <= 0f)
        {
            _Hp = 0f;
            GameManager.GetGameManager().playerAlive = false;
        }
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

        const int pad = 4;

        // Match Draw() footprint: warrior body + axe with current facing.
        float axeOffsetX = _facingLeft ? _bodyWidth - _axePixelSize - 40f : 40f;
        float axeOffsetY = AxeOffsetY;

        float minX = MathF.Min(0f, axeOffsetX);
        float maxX = MathF.Max(_bodyWidth, axeOffsetX + _axePixelSize);
        float minY = MathF.Min(0f, axeOffsetY);
        float maxY = MathF.Max(_bodyWidth, axeOffsetY + _axePixelSize);

        int left = (int)MathF.Floor(Position.X + minX) - pad;
        int top = (int)MathF.Floor(Position.Y + minY) - pad;
        int right = (int)MathF.Ceiling(Position.X + maxX) + pad;
        int bottom = (int)MathF.Ceiling(Position.Y + maxY) + pad;

        _collider.shape = new Rectangle(left, top, right - left, bottom - top);
        SetCollider(_collider);
    }

    public override void Damage(float amount)
    {
        _Hp -= amount;
    }

    protected override void ApplyDashOffset(Vector2 delta)
    {
        Position += delta;
        SyncColliderToPosition();
    }
}
