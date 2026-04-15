using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Last_Hope.Classes.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Orc : BaseEnemy
{
    private const float SpriteScale = 3f;
    private const bool DebugDrawHitbox = true;

    private Vector2 _precisePosition;
    private AnimationManager _walkingAnimation;
    private AnimationManager _attackAnimation;
    private bool _isAttacking = false;
    private bool _isFacingLeft = false;
    private float _attackCooldownTimer = 0f;
    private const float AttackCooldownSeconds = 0.5f;

    private const int OrcFacingRightRow = 0;
    private const int WalkingStartColumn = 0;
    private const int WalkingFrameCount = 3;
    private const int AttackStartColumn = 3;
    private const int AttackFrameCount = 1;
    private const int SheetColumns = 8;
    private const int FrameSize = 32;

    public Orc(Point position)
        : base(maxHealth: 100, currentHealth: 100, speed: 50, experienceValue: 4)
    {
        int size = (int)(FrameSize * SpriteScale);


        _collider = new RectangleCollider(new Rectangle(position, new Point(size, size)));
        SetCollider(_collider);

        _precisePosition = position.ToVector2();
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("orc");

        _walkingAnimation = new AnimationManager(
            WalkingFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            true,
            WalkingStartColumn * FrameSize,
            OrcFacingRightRow * FrameSize
        );

        _attackAnimation = new AnimationManager(
            AttackFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            8,
            false,
            AttackStartColumn * FrameSize,
            OrcFacingRightRow * FrameSize
        );

        // ✅ IMPORTANT: keep spawn position, only ensure size is correct
        var scaledSize = new Point((int)(FrameSize * SpriteScale), (int)(FrameSize * SpriteScale));
        _collider.shape.Size = scaledSize;

        _precisePosition = _collider.shape.Location.ToVector2();
    }

    public override void Update(GameTime gameTime)
    {
        var gameManager = GameManager.GetGameManager();
        var player = gameManager._player;
        var decoy = gameManager.ActiveDecoy;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (player == null && decoy == null)
            return;

        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= dt;
            if (_attackCooldownTimer < 0f)
                _attackCooldownTimer = 0f;
        }

        Vector2 targetPos = decoy != null
            ? decoy.GetPosition()
            : player.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? player.GetPosition();

        Vector2 toTarget = targetPos - GetPosition();

        if (toTarget.X != 0)
            _isFacingLeft = toTarget.X < 0;

        Vector2 direction;

        if (gameManager.NavigationGrid != null &&
            gameManager.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir))
        {
            direction = pathDir;
        }
        else
        {
            direction = toTarget;
            if (direction != Vector2.Zero)
                direction.Normalize();
        }

        Vector2 movement = direction * Speed * dt;

        // X axis
        Vector2 newPosX = new Vector2(_precisePosition.X + movement.X, _precisePosition.Y);
        if (!WouldCollideAt(newPosX))
            _precisePosition = newPosX;

        // Y axis
        Vector2 newPosY = new Vector2(_precisePosition.X, _precisePosition.Y + movement.Y);
        if (!WouldCollideAt(newPosY))
            _precisePosition = newPosY;

        _collider.shape.Location = _precisePosition.ToPoint();

        if (!_isAttacking)
        {
            _walkingAnimation.Update();
        }
        else
        {
            _attackAnimation.Update();
            if (_attackAnimation.isFinished)
            {
                _isAttacking = false;
                ResetWalkAnimation();
            }
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _collider.shape.Center.ToVector2();

        if (DebugDrawHitbox && _collider is not null)
            DrawHitbox(spriteBatch, _collider.shape, Color.Red);

        Rectangle sourceRect;
        int row = _isFacingLeft ? OrcFacingRightRow + 1 : OrcFacingRightRow;

        if (_isAttacking)
        {
            sourceRect = _attackAnimation.GetSourceRect();
            sourceRect.Y = row * FrameSize;
        }
        else
        {
            sourceRect = _walkingAnimation.GetSourceRect();
            sourceRect.Y = row * FrameSize;
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
            WalkingFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            10,
            true,
            WalkingStartColumn * FrameSize,
            OrcFacingRightRow * FrameSize
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

    public override void OnCollision(GameObject other)
    {
        if ((other is not BasePlayer && other is not Decoy) || _isAttacking || _attackCooldownTimer > 0f)
            return;

        if (other is Decoy decoy)
        {
            decoy.Damage(10f);
        }

        _isAttacking = true;
        _attackCooldownTimer = AttackCooldownSeconds;

        _attackAnimation = new AnimationManager(
            AttackFrameCount,
            SheetColumns,
            new Vector2(FrameSize, FrameSize),
            8,
            false,
            AttackStartColumn * FrameSize,
            OrcFacingRightRow * FrameSize
        );
    }

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