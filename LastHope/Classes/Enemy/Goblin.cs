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
    private const bool DebugDrawHitbox = false;

    private Vector2 _precisePosition;
    private BaseWeapon _weapon;
    private float _attackCooldown = 2f;
    private float _attackTimer = 0f;
    private float _attackRange = 300f;

    private const float SpriteScale = 3f;

    private AnimationManager _walkingAnimation;
    private AnimationManager _attackingAnimation;
    private bool _isAttacking = false;
    private bool _isMoving = false;
    private bool _isFacingLeft = false;

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
        _weapon = weapon;
        _weapon.SetOwner(this);

        int size = (int)(FrameSize * SpriteScale);

        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);

        _precisePosition = position.ToVector2();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Goblin spritesheet attempt-sheet");

        _walkingAnimation = new AnimationManager(
            WalkFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            true,
            WalkStartColumn * FrameSize,
            WalkRightRow * FrameSize
        );

        _attackingAnimation = new AnimationManager(
            AttackFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            false,
            AttackRightStartColumn * FrameSize,
            AttackRow * FrameSize
        );

        InitHitbox(_precisePosition, FrameSize, SpriteScale);
    }

    public override void Update(GameTime gameTime)
    {
        var gameManager = GameManager.GetGameManager();
        var player = gameManager._player;
        var decoy = gameManager.ActiveDecoy;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (player == null && decoy == null)
            return;

        if (_attackTimer > 0f)
        {
            _attackTimer -= dt;
            if (_attackTimer < 0f)
                _attackTimer = 0f;
        }

        Vector2 targetPos = decoy != null ? decoy.GetPosition() : player.GetPosition();

        Vector2 toTarget = targetPos - GetPosition();
        float distanceToTarget = toTarget.Length();

        if (toTarget.X != 0)
            _isFacingLeft = toTarget.X < 0;

        Vector2 moveDirection = toTarget;
        if (moveDirection != Vector2.Zero)
            moveDirection.Normalize();

        Vector2 aimDirection = moveDirection;

        bool wasMoving = _isMoving;

        if (distanceToTarget > _attackRange)
        {
            if (gameManager.NavigationGrid != null &&
                gameManager.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir))
            {
                moveDirection = pathDir;
            }

            float moveAmount = Math.Min(Speed * dt, distanceToTarget - _attackRange);
            Vector2 velocity = moveDirection * moveAmount;

            Vector2 newPosX = new Vector2(_precisePosition.X + velocity.X, _precisePosition.Y);
            if (!WouldCollideAt(newPosX))
                _precisePosition = newPosX;

            Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + velocity.Y);
            if (!WouldCollideAt(newPosY))
                _precisePosition = newPosY;

            _collider.shape.Location = _precisePosition.ToPoint();
            _isMoving = true;
        }
        else
        {
            _isMoving = false;
        }

        if (wasMoving && !_isMoving)
        {
            ResetWalkAnimation();
        }

        if (distanceToTarget <= _attackRange && _attackTimer <= 0f)
        {
            _weapon.Attack(aimDirection, GetPosition());
            _attackTimer = _attackCooldown;

            int attackOffsetX = _isFacingLeft
                ? AttackLeftStartColumn * FrameSize
                : AttackRightStartColumn * FrameSize;

            _attackingAnimation = new AnimationManager(
                AttackFrameCount,
                SheetColumns,
                new Vector2(FrameSize, FrameSize),
                10,
                false,
                attackOffsetX,
                AttackRow * FrameSize
            );

            _isAttacking = true;
        }

        if (_isAttacking)
        {
            _attackingAnimation.Update();
            if (_attackingAnimation.isFinished)
            {
                _isAttacking = false;
                ResetWalkAnimation();
            }
        }
        else if (_isMoving)
        {
            _walkingAnimation.Update();
        }

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
        }
        else
        {
            sourceRect = _walkingAnimation.GetSourceRect();
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

    private void ResetWalkAnimation()
    {
        _walkingAnimation = new AnimationManager(
            WalkFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            true,
            WalkStartColumn * FrameSize,
            WalkRightRow * FrameSize
        );
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

    public override void OnCollision(GameObject other) { }

    public override Vector2 GetPosition()
    {
        return _collider.shape.Center.ToVector2();
    }

    private bool WouldCollideAt(Vector2 testPosition)
    {
        Rectangle current = _collider.shape;

        Rectangle testRect = new Rectangle(
            (int)testPosition.X,
            (int)testPosition.Y,
            current.Width,
            current.Height
        );

        var testCollider = new RectangleCollider(testRect);
        return CollisionWorld.CollidesWithStatic(testCollider);
    }
}