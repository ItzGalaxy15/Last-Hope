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
    private const bool DebugDrawHitbox = true;

    private Vector2 _precisePosition;
    private BaseWeapon _weapon;
    private float _attackCooldown = 2f;
    private float _attackTimer = 0f;
    private float _attackRange = 300f;

    public Goblin(Point position, BaseWeapon weapon) : base(maxHealth: 10, currentHealth: 10, speed: 100, experienceValue: 12)
    {
        _collider = new RectangleCollider(new Rectangle(position, Point.Zero));
        SetCollider(_collider);
        _weapon = weapon;
        _weapon.SetOwner(this);
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

        if (distanceToPlayer > _attackRange)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveAmount = Speed * dt;
            moveAmount = Math.Min(moveAmount, distanceToPlayer - _attackRange);

            _precisePosition += direction * moveAmount;
            _collider.shape.Location = _precisePosition.ToPoint();
        }

        _attackTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (distanceToPlayer <= _attackRange && _attackTimer <= 0f)
        {
            _weapon.Attack(direction, GetPosition());
            _attackTimer = _attackCooldown;
        }

        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Vector2 center = _collider.shape.Center.ToVector2();
        spriteBatch.Draw(_texture, center, null, Color.White, 0f, new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f), SpriteScale, SpriteEffects.None, 0f);

        if (DebugDrawHitbox && _collider is not null)
            DrawHitbox(spriteBatch, _collider.shape, Color.Yellow);

        spriteBatch.Draw(_texture, center, null, DrawTint, 0f, new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f), SpriteScale, SpriteEffects.None, 0f);
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
    }

    public override Vector2 GetPosition()
    {
        return _collider.shape.Center.ToVector2();
    }
}