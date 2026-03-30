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
    private const float SpriteScale = 3f;
    private Vector2 _precisePosition;
    private Bow _bow;
    private float _attackCooldown = 2f;
    private float _attackTimer = 0f;
    private float _attackRange = 300f;

    public Goblin(Point position) : base(maxHealth: 10, currentHealth: 10, speed: 100, experienceValue: 12)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
        _bow = new Bow(name: "Goblin Bow", damage: 1, critChance: 0.05f, speed: 200f, owner: this);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        _texture = content.Load<Texture2D>("Goblin");

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

        if (player == null) return;

        Vector2 playerPos = player.GetPosition();
        Vector2 direction = playerPos - GetPosition();
        float distanceToPlayer = direction.Length();

        if (direction != Vector2.Zero)
        {
            direction.Normalize();
        }

        // Only move if outside attack range
        if (distanceToPlayer > _attackRange)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveAmount = Speed * dt;

            // Don't overshoot the attack range boundary
            moveAmount = Math.Min(moveAmount, distanceToPlayer - _attackRange);

            _precisePosition += direction * moveAmount;
            _collider.shape.Location = _precisePosition.ToPoint();
        }

        // Attack if in range and cooldown is ready
        _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (distanceToPlayer <= _attackRange && _attackTimer <= 0f)
        {
            _bow.Attack(direction, GetPosition());
            _attackTimer = _attackCooldown;
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _collider.shape.Center.ToVector2();
        spriteBatch.Draw(_texture, center, null, DrawTint, 0f, new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f), SpriteScale, SpriteEffects.None, 0f);
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