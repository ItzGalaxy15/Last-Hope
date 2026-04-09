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

    // enemies.png layout: row 0 faces right, row 1 faces left.
    // Each row: cols 0-2 = walking, col 3 = attack.
    private const int OrcFacingRightRow = 0;
    private const int WalkingStartColumn = 0;
    private const int WalkingFrameCount = 3;
    private const int AttackStartColumn = 3;
    private const int AttackFrameCount = 1;
    private const int SheetColumns = 8;

    public Orc(Point position) : base(maxHealth: 100, currentHealth: 100, speed: 50, experienceValue: 20)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("orc");

        _walkingAnimation = new AnimationManager(
            numFrames: WalkingFrameCount,
            numColumns: SheetColumns,
            size: new Vector2(32, 32),
            interval: 10,
            loop: true,
            offsetX: WalkingStartColumn * 32,
            offsetY: OrcFacingRightRow * 32
        );

        _attackAnimation = new AnimationManager(
            numFrames: AttackFrameCount,
            numColumns: SheetColumns,
            size: new Vector2(32, 32),
            interval: 8,
            loop: false,
            offsetX: AttackStartColumn * 32,
            offsetY: OrcFacingRightRow * 32
        );

        var scaledSize = new Point((int)(32 * SpriteScale), (int)(32 * SpriteScale));
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
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (player == null && decoy == null)
        {
            return;
        }

        if (_attackCooldownTimer > 0f)
        {
            _attackCooldownTimer -= dt;
            if (_attackCooldownTimer < 0f)
                _attackCooldownTimer = 0f;
        }

        Vector2 targetPos;
        if (decoy != null)
        {
            targetPos = decoy.GetPosition();
        }
        else
        {
            var playerCollider = player.GetCollider();
            targetPos = playerCollider != null
                ? playerCollider.GetBoundingBox().Center.ToVector2()
                : player.GetPosition();
        }

        Vector2 toTarget = targetPos - GetPosition();

        if (toTarget.X != 0)
        {
            _isFacingLeft = toTarget.X < 0;
        }

        Vector2 direction;
        if (gameManager.NavigationGrid != null && gameManager.NavigationGrid.TryGetMoveDirection(GetPosition(), targetPos, out Vector2 pathDir))
        {
            direction = pathDir;
        }
        else
        {
            direction = toTarget;
            if (direction != Vector2.Zero)
                direction.Normalize();
        }

        // Move Orc
        Vector2 movement = direction * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        _precisePosition += movement;
        _collider.shape.Location = _precisePosition.ToPoint();
        
        // Update walking animation
        if (!_isAttacking && player != null)
        {
            _walkingAnimation.Update();
        }
        else if (_isAttacking)
        {
            _attackAnimation.Update();
            if (_attackAnimation.isFinished)
            {
                _isAttacking = false;
                _walkingAnimation = new AnimationManager(
                    numFrames: WalkingFrameCount,
                    numColumns: SheetColumns,
                    size: new Vector2(32, 32),
                    interval: 10,
                    loop: true,
                    offsetX: WalkingStartColumn * 32,
                    offsetY: OrcFacingRightRow * 32
                );
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
        int currentRowOffset = _isFacingLeft ? OrcFacingRightRow + 1 : OrcFacingRightRow;

        if (_isAttacking)
        {
            sourceRect = _attackAnimation.GetSourceRect();
            sourceRect.Y = currentRowOffset * 32;
        }
        else
        {
            sourceRect = _walkingAnimation.GetSourceRect();
            sourceRect.Y = currentRowOffset * 32;
        }
        
        spriteBatch.Draw(_texture, center, sourceRect, DrawTint, 0f, new Vector2(16, 16), SpriteScale, SpriteEffects.None, 0f);
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
        if ((other is not BasePlayer && other is not Decoy) || _isAttacking || _attackCooldownTimer > 0f)
            return;

        if (other is Decoy decoy)
        {
            decoy.Damage(10f); // Orc damages the decoy
        }

        _isAttacking = true;
        _attackCooldownTimer = AttackCooldownSeconds;
        _attackAnimation = new AnimationManager(
            numFrames: AttackFrameCount,
            numColumns: SheetColumns,
            size: new Vector2(32, 32),
            interval: 8,
            loop: false,
            offsetX: AttackStartColumn * 32,
            offsetY: OrcFacingRightRow * 32
        );
    }

    public override Vector2 GetPosition()
    {
        return _collider.shape.Center.ToVector2();
    }
}