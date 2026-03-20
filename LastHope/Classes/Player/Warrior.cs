using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope;

public class Warrior : BasePlayer
{
    public Vector2 Position { get; private set; }
    public Texture2D AxeSprite;
    public Texture2D WarriorSprite;

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
        WarriorSprite = content.Load<Texture2D>("WarriorSprite");

    }

    public void UseWeapon(Vector2 targetWorldPosition)
    {
        Vector2 direction = targetWorldPosition - Position;
        if (direction == Vector2.Zero)
            return;

        direction.Normalize();
        _Weapon.Slash(direction, Position);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(WarriorSprite, Position, Color.White);
        spriteBatch.Draw(AxeSprite, Position + new Vector2(12, 6), Color.White);


        base.Draw(gameTime, spriteBatch);
    }

}