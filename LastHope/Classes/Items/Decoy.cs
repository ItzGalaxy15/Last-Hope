using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Items;

public class Decoy : GameObject
{
    private Vector2 _position;
    public float Health = 100f;
    private float _lifetime = 10f;

    public Decoy(Vector2 position)
    {
        _position = position;
        // Adding a collider so it can receive melee hits like from the Orc
        SetCollider(new Last_Hope.Collision.RectangleCollider(new Rectangle((int)position.X - 15, (int)position.Y - 15, 30, 30)));
    }

    public override void Update(GameTime gameTime)
    {
        _lifetime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
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
        Texture2D pixel = GameManager.GetGameManager().Pixel;
        // Draw a simple brown square as a placeholder for the decoy
        spriteBatch.Draw(pixel, new Rectangle((int)_position.X - 15, (int)_position.Y - 15, 30, 30), Color.Brown);
        
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