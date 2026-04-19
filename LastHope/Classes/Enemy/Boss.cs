using System;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Spell;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Boss : BaseEnemy
{
    private const float SpriteScale = 2.5f;
    private const bool DebugDrawHitbox = false;

    private Vector2 _precisePosition;

    private AnimationManager _walkAnimation;
    private AnimationManager _chargeAnimation;
    private AnimationManager _launchAnimation;

    private bool _isCharging = false;
    private bool _isLaunching = false;
    private bool _isFacingLeft = false;
    private bool _lastFacingLeft = false;   // tracks direction changes for walk anim reset
    private bool _attackFacingLeft = false; // direction locked at charge start

    private float _attackCooldownTimer = 0f;
    private const float AttackCooldownSeconds = 3f;
    private const float AttackRange = 300f;

    // Sprite sheet: 64x64 per frame, 8 columns, 2 rows
    private const int FrameSize = 64;
    private const int SheetColumns = 8;
    private const int FacingRightRow = 0;
    private const int FacingLeftRow = 1;

    // Right-facing row (row 0): neutral(0), walk(1-2), charge(3-6), launch(7)
    private const int RightWalkStart = 1;
    private const int RightChargeStart = 3;
    private const int RightLaunch = 7;

    // Left-facing row (row 1) is mirrored: launch(0), charge(1-4), walk(5-6), neutral(7)
    private const int LeftWalkStart = 5;
    private const int LeftChargeStart = 1;
    private const int LeftLaunch = 0;

    private const int WalkFrameCount = 2;
    private const int ChargeFrameCount = 4;
    private const int LaunchFrameCount = 1;

    public Boss(Point position)
        : base(maxHealth: 400, currentHealth: 400, speed: 60, experienceValue: 100)
    {
        int size = (int)(FrameSize * SpriteScale);
        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);
        _precisePosition = position.ToVector2();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);

        _texture = content.Load<Texture2D>("demon");

        _walkAnimation = CreateWalkAnimation(_isFacingLeft);
        _chargeAnimation = CreateChargeAnimation(_isFacingLeft);
        _launchAnimation = CreateLaunchAnimation(_isFacingLeft);

        _collider.shape.Size = new Point((int)(FrameSize * SpriteScale), (int)(FrameSize * SpriteScale));
        _precisePosition = _collider.shape.Location.ToVector2();
    }

    public override void Update(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        var player = gm._player;
        var decoy = gm.ActiveDecoy;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (player == null && decoy == null)
            return;

        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer = Math.Max(0f, _attackCooldownTimer - dt);

        Vector2 targetPos = decoy != null
            ? decoy.GetPosition()
            : player.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? player.GetPosition();

        Vector2 toTarget = targetPos - GetPosition();
        float distanceToTarget = toTarget.Length();

        if (toTarget.X != 0)
            _isFacingLeft = toTarget.X < 0;

        // Recreate walk animation when direction changes (only while walking freely)
        if (_isFacingLeft != _lastFacingLeft && !_isCharging && !_isLaunching)
        {
            _lastFacingLeft = _isFacingLeft;
            _walkAnimation = CreateWalkAnimation(_isFacingLeft);
        }

        if (_isLaunching)
        {
            _launchAnimation.Update();
            if (_launchAnimation.isFinished)
            {
                _isLaunching = false;
                _attackCooldownTimer = AttackCooldownSeconds;
                _walkAnimation = CreateWalkAnimation(_isFacingLeft);
                _lastFacingLeft = _isFacingLeft;
            }
        }
        else if (_isCharging)
        {
            _chargeAnimation.Update();
            if (_chargeAnimation.isFinished)
            {
                _isCharging = false;
                _isLaunching = true;
                _launchAnimation = CreateLaunchAnimation(_attackFacingLeft);

                Vector2 dir = toTarget;
                if (dir != Vector2.Zero) dir.Normalize();
                gm.AddGameObject(new Fireball(GetPosition(), dir, this));
            }
        }
        else
        {
            if (distanceToTarget <= AttackRange && _attackCooldownTimer <= 0f)
            {
                _isCharging = true;
                _attackFacingLeft = _isFacingLeft;
                _chargeAnimation = CreateChargeAnimation(_attackFacingLeft);
            }
            else
            {
                Vector2 direction;
                if (gm.NavigationGrid != null &&
                    gm.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir))
                {
                    direction = pathDir;
                }
                else
                {
                    direction = toTarget;
                    if (direction != Vector2.Zero) direction.Normalize();
                }

                Vector2 movement = direction * Speed * dt;

                Vector2 newPosX = new Vector2(_precisePosition.X + movement.X, _precisePosition.Y);
                if (!WouldCollideAt(newPosX))
                    _precisePosition = newPosX;

                Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + movement.Y);
                if (!WouldCollideAt(newPosY))
                    _precisePosition = newPosY;

                _collider.shape.Location = _precisePosition.ToPoint();
                _walkAnimation.Update();
            }
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _collider.shape.Center.ToVector2();

        if (DebugDrawHitbox)
            DrawHitbox(spriteBatch, _collider.shape, Color.Red);

        // Row and X offsets are baked into each AnimationManager — no Y override needed
        Rectangle sourceRect;
        if (_isLaunching)
            sourceRect = _launchAnimation.GetSourceRect();
        else if (_isCharging)
            sourceRect = _chargeAnimation.GetSourceRect();
        else
            sourceRect = _walkAnimation.GetSourceRect();

        // Cyan contrasts strongly with the demon's red — the default pinkish-red flash is invisible on it
        Color tint = DrawTint != Color.White ? Color.Cyan : Color.White;

        spriteBatch.Draw(
            _texture,
            center,
            sourceRect,
            tint,
            0f,
            new Vector2(FrameSize / 2f, FrameSize / 2f),
            SpriteScale,
            SpriteEffects.None,
            0f
        );

        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other) { }

    public override Vector2 GetPosition() => _collider.shape.Center.ToVector2();

    private AnimationManager CreateWalkAnimation(bool facingLeft) => new AnimationManager(
        WalkFrameCount, SheetColumns,
        new Vector2(FrameSize, FrameSize),
        10, true,
        (facingLeft ? LeftWalkStart : RightWalkStart) * FrameSize,
        (facingLeft ? FacingLeftRow : FacingRightRow) * FrameSize
    );

    private AnimationManager CreateChargeAnimation(bool facingLeft) => new AnimationManager(
        ChargeFrameCount, SheetColumns,
        new Vector2(FrameSize, FrameSize),
        6, false,
        (facingLeft ? LeftChargeStart : RightChargeStart) * FrameSize,
        (facingLeft ? FacingLeftRow : FacingRightRow) * FrameSize
    );

    private AnimationManager CreateLaunchAnimation(bool facingLeft) => new AnimationManager(
        LaunchFrameCount, SheetColumns,
        new Vector2(FrameSize, FrameSize),
        20, false,
        (facingLeft ? LeftLaunch : RightLaunch) * FrameSize,
        (facingLeft ? FacingLeftRow : FacingRightRow) * FrameSize
    );

    private bool WouldCollideAt(Vector2 testPosition)
    {
        Rectangle current = _collider.shape;
        Rectangle testRect = new Rectangle(
            (int)testPosition.X, (int)testPosition.Y,
            current.Width, current.Height);
        return CollisionWorld.CollidesWithStatic(new RectangleCollider(testRect));
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
}
