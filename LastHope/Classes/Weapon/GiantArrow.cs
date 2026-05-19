using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Last_Hope.Classes.Abilities;

public class GiantArrow : GameObject
{
    private RectangleCollider _collider;
    private Texture2D _sprite;
    private Vector2 _position;
    private Vector2 _velocity;
    private GameObject _owner;
    private int _damage;
    private HashSet<GameObject> _alreadyHit = new HashSet<GameObject>();

    // Const specific to the Giant Arrow
    private const float Scale = 3f;
    private Color ArrowColor = Color.Yellow;

    public GiantArrow(Vector2 origin, Vector2 direction, float speed, GameObject owner, int damage)
    {
        _position = origin;
        _velocity = direction * speed;
        _owner = owner;
        _damage = damage;
        _collider = new RectangleCollider(new Rectangle(origin.ToPoint(), new Point(30, 30)));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        _sprite = content.Load<Texture2D>("Arrow");
        _collider.shape.Size = new Point(
            (int)(_sprite.Width * Scale),
            (int)(_sprite.Height * Scale)
        );
        base.Load(content);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 nextPosition = _position + _velocity * dt;

        Rectangle nextRect = new Rectangle(
            (int)(nextPosition.X - _collider.shape.Width / 2f),
            (int)(nextPosition.Y - _collider.shape.Height / 2f),
            _collider.shape.Width,
            _collider.shape.Height
        );

        var testCollider = new RectangleCollider(nextRect);
        if (CollisionWorld.CollidesWithStatic(testCollider))
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
        float angle = (float)System.Math.Atan2(_velocity.Y, _velocity.X);
        Vector2 origin = new Vector2(_sprite.Width / 2f, _sprite.Height / 2f);
        spriteBatch.Draw(_sprite, _position, null, ArrowColor, angle, origin, Scale, SpriteEffects.None, 0f);
        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
        if (_alreadyHit.Contains(other))
            return;

        if (other is BaseEnemy enemy)
        {
            _alreadyHit.Add(other);
            enemy.Damage(_damage);
            if (enemy._currentHp <= 0)
            {
                GameManager.GetGameManager().RemoveGameObject(enemy);
                GameManager.GetGameManager()._player?.AddExperience(enemy.ExperienceValue);
            }
        }
    }
}