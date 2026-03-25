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

    public Orc(Point position) : base(maxHealth: 100, currentHealth: 100, speed: 50)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Orc");

        var scaledSize = new Point((int)(_texture.Width * SpriteScale), (int)(_texture.Height * SpriteScale));
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
        
        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }
            
        // Move Orc
        Vector2 movement = direction * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;

        _precisePosition += movement;
        _collider.shape.Location = _precisePosition.ToPoint();
        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_texture, _collider.shape.Location.ToVector2(), null, Color.White, 0f, new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f), SpriteScale, SpriteEffects.None, 0f);
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