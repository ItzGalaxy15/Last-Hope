using System.Runtime.CompilerServices;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Goblin : BaseEnemy
{
    private const float SpriteScale = 5f;

    public Goblin(Point position) : base (100,100,2)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Goblin");

        var scaledSize = new Point((int)(_texture.Width * SpriteScale), (int)(_texture.Height * SpriteScale));
        _collider.shape.Size = scaledSize;
        _collider.shape.Location -= new Point(scaledSize.X / 2, scaledSize.Y / 2);
        SetCollider(_collider);
    }

    public override void Update(GameTime gameTime)
    {
        // Implement goblin behavior and movement logic here
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            _texture,
            _collider.shape.Center.ToVector2(),
            null,
            Color.White,
            0f,
            new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f),
            SpriteScale,
            SpriteEffects.None,
            0f);
        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
    }
}