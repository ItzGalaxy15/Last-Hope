using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Goblin : BaseEnemy
{
    // Visualize hitbox for debugging
    private const bool DebugDrawHitbox = true;

    // Core variables
    private Vector2 _precisePosition;
    private BaseWeapon _weapon;
    private float _attackCooldown = 2f;
    private float _attackTimer = 0f;
    private float _attackRange = 300f;

    // Scale
    private const float SpriteScale = 3f;

    // Animation variables
    private AnimationManager _walkingAnimation;
    private AnimationManager _attackingAnimation;
    private bool _isAttacking = false;
    private bool _isMoving = false;
    private bool _isFacingLeft = false;

    // Goblin spritesheet layout:
    // Row 0: walking right, columns 0-3
    // Row 1: walking left,  columns 0-3
    // Row 2: attack left columns 0-1, attack right columns 2-3
    private const int WalkRightRow = 0;
    private const int WalkLeftRow = 1;
    private const int AttackRow = 2;
    private const int WalkStartColumn = 0;
    private const int WalkFrameCount = 4;
    private const int AttackRightStartColumn = 2;
    private const int AttackLeftStartColumn = 0;
    private const int AttackFrameCount = 2;
    private const int SheetColumns = 4;
    private const int FrameSize = 32;

    public Goblin(Point position, BaseWeapon weapon) : base(maxHealth: 10, currentHealth: 10, speed: 100, experienceValue: 12)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
        _weapon = weapon;
        _weapon.SetOwner(this);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Goblin spritesheet attempt-sheet");

        _walkingAnimation = new AnimationManager(
            numFrames: WalkFrameCount,
            numColumns: SheetColumns,
            size: new Vector2(FrameSize, FrameSize),
            interval: 10,
            loop: true,
            offsetX: WalkStartColumn * FrameSize,
            offsetY: WalkRightRow * FrameSize
        );

        _attackingAnimation = new AnimationManager(
            numFrames: AttackFrameCount,
            numColumns: SheetColumns,
            size: new Vector2(FrameSize, FrameSize),
            interval: 10,
            loop: false,
            offsetX: AttackRightStartColumn * FrameSize,
            offsetY: AttackRow * FrameSize
        );

        // Use a single frame size (32x32) scaled, not the full texture dimensions
        var scaledSize = new Point((int)(FrameSize * SpriteScale), (int)(FrameSize * SpriteScale));
        _collider.shape.Size = scaledSize;
        _collider.shape.Location -= new Point(scaledSize.X / 2, scaledSize.Y / 2);
        _precisePosition = _collider.shape.Location.ToVector2();
        SetCollider(_collider);
    }

    public override void Update(GameTime gameTime)
    {
        var gameManager = GameManager.GetGameManager();
        var player = gameManager._player;
        var decoy = gameManager.ActiveDecoy;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (player == null && decoy == null)
            return;

        // Tick attack cooldown
        if (_attackTimer > 0f)
        {
            _attackTimer -= dt;
            if (_attackTimer < 0f)
                _attackTimer = 0f;
        }

        Vector2 targetPos = decoy != null ? decoy.GetPosition() : player.GetPosition();

        // Compute direction and REAL distance before normalizing
        Vector2 toTarget = targetPos - GetPosition();
        float distanceToTarget = toTarget.Length();

        if (toTarget.X != 0)
            _isFacingLeft = toTarget.X < 0;

        Vector2 moveDirection = toTarget;
        if (moveDirection != Vector2.Zero)
            moveDirection.Normalize();

        Vector2 aimDirection = moveDirection;

        // Move only when outside attack range
        bool wasMoving = _isMoving;
        if (distanceToTarget > _attackRange)
        {
            if (gameManager.NavigationGrid != null && gameManager.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir))
                moveDirection = pathDir;

            // Don't overshoot: cap movement so we stop at the edge of attack range
            float moveAmount = Math.Min(Speed * dt, distanceToTarget - _attackRange);
            _precisePosition += moveDirection * moveAmount;
            _collider.shape.Location = _precisePosition.ToPoint();
            _isMoving = true;
        }
        else
        {
            _isMoving = false;
        }

        // Reset walk animation to frame 0 when the goblin stops, so idle shows the first frame
        if (wasMoving && !_isMoving)
        {
            _walkingAnimation = new AnimationManager(
                numFrames: WalkFrameCount,
                numColumns: SheetColumns,
                size: new Vector2(FrameSize, FrameSize),
                interval: 10,
                loop: true,
                offsetX: WalkStartColumn * FrameSize,
                offsetY: WalkRightRow * FrameSize
            );
        }

        // Attack when in range and cooldown has elapsed
        if (distanceToTarget <= _attackRange && _attackTimer <= 0f)
        {
            _weapon.Attack(aimDirection, GetPosition());
            _attackTimer = _attackCooldown;

            // Reset attack animation with correct facing offset
            int attackOffsetX = _isFacingLeft ? AttackLeftStartColumn * FrameSize : AttackRightStartColumn * FrameSize;
            _attackingAnimation = new AnimationManager(
                numFrames: AttackFrameCount,
                numColumns: SheetColumns,
                size: new Vector2(FrameSize, FrameSize),
                interval: 10,
                loop: false,
                offsetX: attackOffsetX,
                offsetY: AttackRow * FrameSize
            );
            _isAttacking = true;
        }

        // Update animations
        if (_isAttacking)
        {
            _attackingAnimation.Update();
            if (_attackingAnimation.isFinished)
            {
                _isAttacking = false;
                // Reset walking animation
                _walkingAnimation = new AnimationManager(
                    numFrames: WalkFrameCount,
                    numColumns: SheetColumns,
                    size: new Vector2(FrameSize, FrameSize),
                    interval: 10,
                    loop: true,
                    offsetX: WalkStartColumn * FrameSize,
                    offsetY: WalkRightRow * FrameSize
                );
            }
        }
        else if (_isMoving)
        {
            _walkingAnimation.Update();
        }
        // When idle (_isMoving == false), the animation simply doesn't advance,
        // leaving it frozen on frame 0 as the idle pose.

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _collider.shape.Center.ToVector2();

        if (DebugDrawHitbox && _collider is not null)
            DrawHitbox(spriteBatch, _collider.shape, Color.Yellow);

        Rectangle sourceRect;

        if (_isAttacking)
        {
            sourceRect = _attackingAnimation.GetSourceRect();
            // Facing is already baked into the animation offset, no row override needed
        }
        else
        {
            sourceRect = _walkingAnimation.GetSourceRect();
            // Override the row based on facing direction
            sourceRect.Y = (_isFacingLeft ? WalkLeftRow : WalkRightRow) * FrameSize;
        }

        spriteBatch.Draw(
            _texture,
            center,
            sourceRect,
            DrawTint,
            0f,
            new Vector2(FrameSize / 2f, FrameSize / 2f),
            SpriteScale,
            SpriteEffects.None,
            0f
        );

        base.Draw(gameTime, spriteBatch);
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

    public override void OnCollision(GameObject other)
    {
    }

    public override Vector2 GetPosition()
    {
        return _collider.shape.Center.ToVector2();
    }
}