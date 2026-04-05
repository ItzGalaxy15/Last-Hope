using Last_Hope.Engine;
using Last_Hope.Collision;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Items;

public class Decoy : GameObject
{
    private const int DecoySize = 30;
    private const float ThrowDragPerFrame = 0.92f;
    private const float SettleSpeed = 8f;
    private static readonly Rectangle FoodBoxSourceRect = new Rectangle(0, 32, 32, 32);

    private Vector2 _position;
    private Vector2 _velocity;
    public float Health = 100f;
    private float _lifetime;
    private readonly RectangleCollider _collider;
    private Texture2D? _itemSpriteSheet;

    public Decoy(Vector2 position, Vector2 initialVelocity, float lifetimeSeconds = 5f)
    {
        _position = position;
        _velocity = initialVelocity;
        _lifetime = lifetimeSeconds;

        _collider = new RectangleCollider(new Rectangle((int)position.X - (DecoySize / 2), (int)position.Y - (DecoySize / 2), DecoySize, DecoySize));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        try
        {
            _itemSpriteSheet = content.Load<Texture2D>("itemSpriteSheet");
        }
        catch (ContentLoadException)
        {
            _itemSpriteSheet = null;
        }

        base.Load(content);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _position += _velocity * dt;
        _velocity *= ThrowDragPerFrame;
        if (_velocity.LengthSquared() < SettleSpeed * SettleSpeed)
            _velocity = Vector2.Zero;

        _collider.shape.Location = new Point((int)_position.X - (DecoySize / 2), (int)_position.Y - (DecoySize / 2));

        _lifetime -= dt;
        if (_lifetime <= 0f || Health <= 0f)
        {
            GameManager.GetGameManager().RemoveGameObject(this);
            if (GameManager.GetGameManager().ActiveDecoy == this)
            {
                GameManager.GetGameManager().ActiveDecoy = null;
            }
        }

        base.Update(gameTime);
    }

    public void Damage(float amount)
    {
        Health -= amount;
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Rectangle destinationRect = new Rectangle((int)_position.X - (DecoySize / 2), (int)_position.Y - (DecoySize / 2), DecoySize, DecoySize);

        if (_itemSpriteSheet is not null)
        {
            spriteBatch.Draw(_itemSpriteSheet, destinationRect, FoodBoxSourceRect, Color.White);
        }
        else
        {
            Texture2D pixel = GameManager.GetGameManager().Pixel;
            spriteBatch.Draw(pixel, destinationRect, Color.Brown);
        }

        base.Draw(gameTime, spriteBatch);
    }

    public Vector2 GetPosition()
    {
        return _position;
    }

    public override void Destroy()
    {
        if (GameManager.GetGameManager().ActiveDecoy == this)
        {
            GameManager.GetGameManager().ActiveDecoy = null;
        }
        base.Destroy();
    }
}