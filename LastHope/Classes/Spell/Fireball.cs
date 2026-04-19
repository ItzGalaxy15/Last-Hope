using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Spell;

public class Fireball : GameObject
{
    private RectangleCollider _collider;
    private Vector2 _position;
    private Vector2 _velocity;
    private readonly GameObject _owner;
    private readonly float _damage;
    private float _lifetime;

    private const int Size = 18;
    private const float Speed = 320f;
    private const float MaxLifetime = 5f;

    public Fireball(Vector2 origin, Vector2 direction, GameObject owner, float damage = 25f)
    {
        _owner = owner;
        _damage = damage;
        _lifetime = MaxLifetime;

        // Spawn slightly ahead of the owner so it doesn't self-collide on first frame
        _position = origin + direction * 90f;
        _velocity = direction * Speed;

        _collider = new RectangleCollider(new Rectangle(
            (int)(_position.X - Size / 2f),
            (int)(_position.Y - Size / 2f),
            Size, Size));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _lifetime -= dt;
        if (_lifetime <= 0f)
        {
            GameManager.GetGameManager().RemoveGameObject(this);
            return;
        }

        Vector2 nextPosition = _position + _velocity * dt;

        Rectangle nextRect = new Rectangle(
            (int)(nextPosition.X - Size / 2f),
            (int)(nextPosition.Y - Size / 2f),
            Size, Size);

        if (CollisionWorld.CollidesWithStatic(new RectangleCollider(nextRect)))
        {
            GameManager.GetGameManager().RemoveGameObject(this);
            return;
        }

        _position = nextPosition;
        _collider.shape.Location = nextRect.Location;

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Texture2D pixel = GameManager.GetGameManager().Pixel;

        // Outer glow (larger, darker orange)
        int glowSize = Size + 6;
        spriteBatch.Draw(pixel,
            new Rectangle((int)_position.X - glowSize / 2, (int)_position.Y - glowSize / 2, glowSize, glowSize),
            new Color(255, 80, 0, 120));

        // Core (bright orange-red)
        spriteBatch.Draw(pixel,
            new Rectangle((int)_position.X - Size / 2, (int)_position.Y - Size / 2, Size, Size),
            new Color(255, 140, 0));

        // Bright center
        int coreSize = Size / 2;
        spriteBatch.Draw(pixel,
            new Rectangle((int)_position.X - coreSize / 2, (int)_position.Y - coreSize / 2, coreSize, coreSize),
            new Color(255, 220, 80));

        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
        if (other == _owner) return;

        var gm = GameManager.GetGameManager();

        if (other is BasePlayer player)
        {
            player.Damage(_damage);
            gm.RemoveGameObject(this);
        }
        else if (other is Decoy decoy)
        {
            decoy.Damage(_damage);
            gm.RemoveGameObject(this);
        }
    }
}
