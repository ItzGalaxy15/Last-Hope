using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Last_Hope.Classes.Items;

public enum ItemType
{
    None,
    Bomb,
    Decoy
}

public class ItemDrop : GameObject
{
    private Vector2 _position;
    private ItemType _type;
    private RectangleCollider _collider;
    private Texture2D? _itemSpriteSheet;
    private const float PickupRadius = 150f;
    private const float Speed = 350f;

    public ItemDrop(Vector2 position, ItemType type)
    {
        _position = position;
        _type = type;
        _collider = new RectangleCollider(new Rectangle((int)position.X - 16, (int)position.Y - 16, 32, 32));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);
        try
        {
            _itemSpriteSheet = content.Load<Texture2D>("itemSpriteSheet");
        }
        catch (ContentLoadException)
        {
            _itemSpriteSheet = null;
        }
    }

    public override void Update(GameTime gameTime)
    {
        GameManager gm = GameManager.GetGameManager();
        if (gm._player is not Warrior player) return;

        Vector2 playerPos = player.GetPosition();
        float distance = Vector2.Distance(_position, playerPos);

        if (distance < PickupRadius)
        {
            Vector2 direction = playerPos - _position;
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
                _position += direction * Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                _collider.shape.Location = new Point((int)_position.X - 16, (int)_position.Y - 16);
            }

            if (distance < 25f)
            {
                if (player.TryPickupItem(_type))
                {
                    gm.RemoveGameObject(this);
                }
                else
                {
                    player.AddExperience(10); // Small chunk of XP if inventory is full
                    gm.RemoveGameObject(this);
                }
            }
        }
        base.Update(gameTime);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_itemSpriteSheet != null)
        {
            Rectangle sourceRect = _type == ItemType.Bomb ? new Rectangle(0, 0, 32, 32) : new Rectangle(0, 32, 32, 32);
            spriteBatch.Draw(_itemSpriteSheet, _position, sourceRect, Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);
        }
        else
        {
            Texture2D pixel = GameManager.GetGameManager().Pixel;
            spriteBatch.Draw(pixel, new Rectangle((int)_position.X - 8, (int)_position.Y - 8, 16, 16), _type == ItemType.Bomb ? Color.Black : Color.Brown);
        }
    }
}