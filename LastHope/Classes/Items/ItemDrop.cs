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
    Decoy,
    HealingPotion,
    OneUp
}

public class ItemDrop : GameObject
{
    private Vector2 _position;
    private ItemType _type;
    public ItemType Type => _type;
    private RectangleCollider _collider;
    private Texture2D? _itemSpriteSheet;
    private Texture2D? _hearthSprite;
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

        try
        {
            _hearthSprite = content.Load<Texture2D>("Heart");
        }
        catch (ContentLoadException)
        {
            _hearthSprite = null;
        }
    }

    public override void Update(GameTime gameTime)
    {
        GameManager gm = GameManager.GetGameManager();
        if (gm._player is not Warrior player) return;

        // Prevent rare items from being sucked in and destroyed for XP if inventory is full
        bool isInventoryFull = player.Inventory[0] != ItemType.None && player.Inventory[1] != ItemType.None;
        if (isInventoryFull && (_type == ItemType.OneUp || _type == ItemType.HealingPotion))
            return;

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
        float bounceOffset = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 4f) * 6f;
        Vector2 drawPos = _position + new Vector2(0, bounceOffset);

        if (_type == ItemType.OneUp && _hearthSprite != null)
        {
            Rectangle sourceRect = new Rectangle(0, 0, _hearthSprite.Width, _hearthSprite.Height);
            Vector2 origin = new Vector2(_hearthSprite.Width / 2f, _hearthSprite.Height / 2f);
            
            // Draw red glow behind the item
            spriteBatch.Draw(_hearthSprite, drawPos, sourceRect, Color.Red * 0.5f, 0f, origin, 1.4f, SpriteEffects.None, 0f);
            spriteBatch.Draw(_hearthSprite, drawPos, sourceRect, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
        else if (_itemSpriteSheet != null && _type != ItemType.OneUp)
        {
            Rectangle sourceRect = _type == ItemType.Bomb ? new Rectangle(0, 0, 32, 32) : 
                                   _type == ItemType.Decoy ? new Rectangle(0, 32, 32, 32) : 
                                   new Rectangle(0, 64, 32, 32);
            
            // Draw red glow behind the item
            spriteBatch.Draw(_itemSpriteSheet, drawPos, sourceRect, Color.Red * 0.5f, 0f, new Vector2(16, 16), 1.4f, SpriteEffects.None, 0f);
            spriteBatch.Draw(_itemSpriteSheet, drawPos, sourceRect, Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);
        }
        else
        {
            Texture2D pixel = GameManager.GetGameManager().Pixel;
            
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X - 12, (int)drawPos.Y - 12, 24, 24), Color.Red * 0.5f);
            Color itemColor = _type == ItemType.Bomb ? Color.Black : 
                              _type == ItemType.Decoy ? Color.Brown : 
                              _type == ItemType.HealingPotion ? Color.Red : Color.Pink;
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X - 8, (int)drawPos.Y - 8, 16, 16), itemColor);
        }
    }
}