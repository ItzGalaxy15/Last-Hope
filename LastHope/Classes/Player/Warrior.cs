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
    private double timeSinceLastAttack = 0;


    public Warrior(Vector2 startPosition)
        : base(hp: 100f, weapon: new Weapon("Sword", attack: 20, critChance: 0.2f), speed: 220f)
    {
        Position = startPosition;
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
    }

    public override void Update(GameTime gameTime)
    {
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
        _Weapon.Slash(direction, slashOrigin);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(WarriorSprite, Position, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
        spriteBatch.Draw(AxeSprite, Position + new Vector2(40, 5), null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);


        base.Draw(gameTime, spriteBatch);
    }

}