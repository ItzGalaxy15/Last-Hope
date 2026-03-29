using System;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope;

public class Warrior : BasePlayer
{
    public Vector2 Position { get; private set; }
    public Texture2D AxeSprite;
    public Texture2D WarriorSprite;
    public InputManager _inputManager {get; private set;}

    private const float AttackCooldown = 1f;  // 0.5 seconds between attacks
    private const float EnemyContactDamage = 10f;
    private const float EnemyContactHurtInterval = 0.5f;
    private double timeSinceLastAttack = 0;
    private Vector2 _moveInput;
    private bool _facingLeft;
    private RectangleCollider _collider;
    private float _hurtCooldown;


    public Warrior(Vector2 startPosition)
        : base(hp: 10f, weapon: new Weapon("Sword", damage: 20, critChance: 1.0f), speed: 220f, level: 0, experience: 0)
    {
        Position = startPosition;
        var origin = new Point((int)startPosition.X, (int)startPosition.Y);
        _collider = new RectangleCollider(new Rectangle(origin, Point.Zero));
        SetCollider(_collider);
    }

    public override Vector2 GetPosition()
    {
        return Position;
    }

    public void Move(Vector2 direction, GameTime gameTime)
    {
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();
        Position += direction * _Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        AxeSprite = content.Load<Texture2D>("Axe");
        WarriorSprite = content.Load<Texture2D>("Warrior");
        _inputManager = GameManager.GetGameManager().InputManager;

        SyncColliderToPosition();
        SetCollider(_collider);
    }

    public override void HandleInput(InputManager inputManager)
    {
        _moveInput = Vector2.Zero;
        if (inputManager.IsKeyDown(Keys.W) || inputManager.IsKeyDown(Keys.Up))
            _moveInput.Y -= 1f;
        if (inputManager.IsKeyDown(Keys.S) || inputManager.IsKeyDown(Keys.Down))
            _moveInput.Y += 1f;
        if (inputManager.IsKeyDown(Keys.A) || inputManager.IsKeyDown(Keys.Left))
            _moveInput.X -= 1f;
        if (inputManager.IsKeyDown(Keys.D) || inputManager.IsKeyDown(Keys.Right))
            _moveInput.X += 1f;
    }

    public override void Update(GameTime gameTime)
    {
        if (!GameManager.GetGameManager().playerAlive || _Hp <= 0f)
            return;

        Move(_moveInput, gameTime);
        SyncColliderToPosition();

        if (_hurtCooldown > 0f)
            _hurtCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_moveInput.X < 0f)
            _facingLeft = true;
        else if (_moveInput.X > 0f)
            _facingLeft = false;

        timeSinceLastAttack += gameTime.ElapsedGameTime.TotalSeconds;
        if (_inputManager.IsKeyPress(Keys.B) && timeSinceLastAttack >= AttackCooldown)
        {
            UseWeapon();
            timeSinceLastAttack = 0;
        }
        base.Update(gameTime);
    }

    public void UseWeapon()
    {
        System.Console.WriteLine("UseWeapon called");
        Vector2 mousePosition = _inputManager.CurrentMouseState.Position.ToVector2();
        Vector2 direction = mousePosition - Position;
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();

        // Calculate center of the scaled sprite (scale is 2f)
        Vector2 spriteCenter = Position + new Vector2(WarriorSprite.Width, WarriorSprite.Height);

        // Offset slash from center
        const float slashDistance = 80f;
        Vector2 slashOrigin = spriteCenter + direction * slashDistance;

        System.Console.WriteLine($"Creating slash at {slashOrigin} in direction {direction}");
        _Weapon.Attack(direction, slashOrigin);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var flip = _facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float scaledWarriorW = WarriorSprite.Width * 2f;
        float scaledAxeW = AxeSprite.Width * 2f;
        Vector2 axeOffset = _facingLeft
            ? new Vector2(scaledWarriorW - scaledAxeW - 40f, 5f)
            : new Vector2(40f, 5f);

        spriteBatch.Draw(WarriorSprite, Position, null, Color.White, 0f, Vector2.Zero, 1f, flip, 0f);
        spriteBatch.Draw(AxeSprite, Position + axeOffset, null, Color.White, 0f, Vector2.Zero, 1f, flip, 0f);

        base.Draw(gameTime, spriteBatch);
    }

    public override void OnCollision(GameObject other)
    {
        if (other is not BaseEnemy || _hurtCooldown > 0f)
            return;

        _Hp -= EnemyContactDamage;
        _hurtCooldown = EnemyContactHurtInterval;
        if (_Hp <= 0f)
        {
            _Hp = 0f;
            GameManager.GetGameManager().playerAlive = false;
        }
    }

    private void SyncColliderToPosition()
    {
        const int pad = 4;
        // Union of warrior + axe (draw scale 1f); offsets match Draw().
        float axeOx = _facingLeft ? WarriorSprite.Width - AxeSprite.Width - 40f : 40f;
        float minX = Math.Min(0f, axeOx);
        float maxX = Math.Max(WarriorSprite.Width, axeOx + AxeSprite.Width);
        float minY = Math.Min(0f, 5f);
        float maxY = Math.Max(WarriorSprite.Height, 5f + AxeSprite.Height);

        _collider.shape.X = (int)Math.Floor(Position.X + minX) - pad;
        _collider.shape.Y = (int)Math.Floor(Position.Y + minY) - pad;
        _collider.shape.Width = (int)Math.Ceiling(maxX - minX) + pad * 2;
        _collider.shape.Height = (int)Math.Ceiling(maxY - minY) + pad * 2;
    }

    public override void Damage(float amount)
    {
        _Hp -= amount;
    }
}
