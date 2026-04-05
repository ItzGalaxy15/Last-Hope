using System.Collections.Generic;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Items;

public class Bomb : GameObject
{
    private Vector2 _position;
    private float _timer = 2f;
    private float _explosionRadius = 150f;
    private float _damage = 50f;

    public Bomb(Vector2 position)
    {
        _position = position;
    }

    public override void Update(GameTime gameTime)
    {
        _timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_timer <= 0f)
        {
            Explode();
        }
        
        base.Update(gameTime);
    }

    private void Explode()
    {
        var gm = GameManager.GetGameManager();
        
        List<GameObject> inRadius = gm.GetObjectsInRadius(_position, _explosionRadius);
        foreach (var obj in inRadius)
        {
            if (obj is BaseEnemy enemy)
            {
                enemy.Damage(_damage);
            }
        }
        
        gm.RemoveGameObject(this);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Texture2D pixel = GameManager.GetGameManager().Pixel;
        // Draw a simple black square as a placeholder for the bomb
        spriteBatch.Draw(pixel, new Rectangle((int)_position.X - 10, (int)_position.Y - 10, 20, 20), Color.Black);
        
        base.Draw(gameTime, spriteBatch);
    }
}