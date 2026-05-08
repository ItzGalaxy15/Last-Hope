using Last_Hope.BaseModel;
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
    OneUp,
}

public class ItemDrop : GameObject
{
    private Vector2 _position;
    public ItemType Type { get; }
    private RectangleCollider _collider;
    private Texture2D? _itemSpriteSheet;
    private Texture2D? _hearthSprite;
    private const float PickupRadius = 150f;
    private const float Speed = 350f;

    /// <summary>
    /// Creates an item drop of the given type at the given world position.
    /// </summary>
    public ItemDrop(Vector2 position, ItemType type)
    {
        _position = position;
        Type = type;
        _collider = new RectangleCollider(new Rectangle((int)position.X - 16, (int)position.Y - 16, 32, 32));
        SetCollider(_collider);
    }

    /// <summary>
    /// Loads the item sprite sheet and the heart sprite for the OneUp item. Falls back gracefully if either asset is missing.
    /// </summary>
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

    /// <summary>
    /// Pulls the item toward the player when within pickup radius, picks it up on contact, and awards fallback XP if the inventory is full.
    /// Rare items (OneUp, HealingPotion) are frozen in place when both inventory slots are occupied.
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        GameManager gm = GameManager.GetGameManager();
        BasePlayer? player = gm._player;
        ItemType[]? inv = PlayerInventoryHelper.GetInventorySlots(player);
        if (player is null || inv is null)
            return;

        // Prevent rare items from being sucked in and destroyed for XP if inventory is full
        bool isInventoryFull = inv[0] != ItemType.None && inv[1] != ItemType.None;
        if (isInventoryFull && (Type == ItemType.OneUp || Type == ItemType.HealingPotion))
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
                if (!PlayerInventoryHelper.TryPickup(player, Type))
                    player.AddExperience(10);
                gm.RemoveGameObject(this);
            }
        }
        base.Update(gameTime);
    }

    /// <summary>
    /// Draws the item with a sine-wave vertical bounce and a red glow behind it. Uses the heart sprite for OneUp, the sprite sheet for all other types, or colored squares as a fallback.
    /// </summary>
    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        float bounceOffset = (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 4f) * 6f;
        Vector2 drawPos = _position + new Vector2(0, bounceOffset);

        if (Type == ItemType.OneUp && _hearthSprite != null)
        {
            Rectangle sourceRect = new Rectangle(0, 0, _hearthSprite.Width, _hearthSprite.Height);
            Vector2 origin = new Vector2(_hearthSprite.Width / 2f, _hearthSprite.Height / 2f);
            
            // Draw red glow behind the item
            spriteBatch.Draw(_hearthSprite, drawPos, sourceRect, Color.Red * 0.5f, 0f, origin, 1.4f, SpriteEffects.None, 0f);
            spriteBatch.Draw(_hearthSprite, drawPos, sourceRect, Color.White, 0f, origin, 1f, SpriteEffects.None, 0f);
        }
        else if (_itemSpriteSheet != null)
        {
            Rectangle sourceRect = Type == ItemType.Bomb ? new Rectangle(0, 0, 32, 32) : 
                                   Type == ItemType.Decoy ? new Rectangle(0, 32, 32, 32) : 
                                   new Rectangle(0, 64, 32, 32);
            
            // Draw red glow behind the item
            spriteBatch.Draw(_itemSpriteSheet, drawPos, sourceRect, Color.Red * 0.5f, 0f, new Vector2(16, 16), 1.4f, SpriteEffects.None, 0f);
            spriteBatch.Draw(_itemSpriteSheet, drawPos, sourceRect, Color.White, 0f, new Vector2(16, 16), 1f, SpriteEffects.None, 0f);
        }
        else
        {
            Texture2D pixel = GameManager.GetGameManager().Pixel;
            
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X - 12, (int)drawPos.Y - 12, 24, 24), Color.Red * 0.5f);
            Color itemColor = Type == ItemType.Bomb ? Color.Black : 
                              Type == ItemType.Decoy ? Color.Brown : 
                              Type == ItemType.HealingPotion ? Color.Red : Color.Pink;
            spriteBatch.Draw(pixel, new Rectangle((int)drawPos.X - 8, (int)drawPos.Y - 8, 16, 16), itemColor);
        }
    }
}