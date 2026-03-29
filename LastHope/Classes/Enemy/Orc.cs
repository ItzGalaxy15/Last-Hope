using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Orc : BaseEnemy
{
    private const float SpriteScale = 1f;
    private Vector2 _precisePosition;
    private AnimationManager _walkingAnimation;
    private bool _isAttacking = false;
    private bool _isFacingLeft = false;
    private const float AttackDistance = 50f;
    private const int OrcRowOffset = 7;

    public Orc(Point position) : base(maxHealth: 100, currentHealth: 100, speed: 50)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("spritesheet");

        // Initialize walking animation: 3 frames, 8 columns, 32x32 sprites
        _walkingAnimation = new AnimationManager(
            numFrames: 3,
            numColumns: 8,
            size: new Vector2(32, 32),
            interval: 10,
            loop: true,
            offsetX: 4 * 32, 
            offsetY: OrcRowOffset * 32
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

        if (player == null)
        {
            return;
        }

        Vector2 playerPos = player.GetPosition();
        Vector2 direction = playerPos - GetPosition();
        float distanceToPlayer = direction.Length();

        if (direction.X != 0)
        {
            _isFacingLeft = direction.X < 0;
        }
        
        // Check if should switch to attack mode
        _isAttacking = distanceToPlayer < AttackDistance;
        
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }
            
        // Move Orc
        Vector2 movement = direction * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        _precisePosition += movement;
        _collider.shape.Location = _precisePosition.ToPoint();
        
        // Update walking animation
        if (!_isAttacking)
        {
            _walkingAnimation.Update();
        }
        
        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _collider.shape.Center.ToVector2();
        
        Rectangle sourceRect;
        int currentRowOffset = _isFacingLeft ? OrcRowOffset + 1 : OrcRowOffset;

        if (_isAttacking)
        {
            // Draw attack frame (column 3, row 4)
            sourceRect = new Rectangle(7 * 32, currentRowOffset * 32, 32, 32);
        }
        else
        {
            // Draw current walking animation frame
            sourceRect = _walkingAnimation.GetSourceRect();
            sourceRect.Y = currentRowOffset * 32;
        }
        
        spriteBatch.Draw(_texture, center, sourceRect, Color.White, 0f, new Vector2(16, 16), SpriteScale, SpriteEffects.None, 0f);
        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
    }

    public override Vector2 GetPosition()
    {
        return _collider.shape.Center.ToVector2();
    }
}