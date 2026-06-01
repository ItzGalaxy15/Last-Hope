using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class OneUpIcon : UIElement
{
    private readonly Texture2D? _pixel;
    private Texture2D? _heartSprite;
    private bool _triedLoadingHeart;

    private const int SlotSize = 64;
    private const int SlotGap = 3;
    private const int SideMargin = 48;
    private const int BottomMargin = 28;
    private const int ItemInset = 6;

    private readonly bool _hasDash;
    private readonly bool _hasTeleport;
    private Rectangle _frameRect;
    private Rectangle _iconRect;

    public OneUpIcon(Texture2D? pixel, bool hasDash = false, bool hasTeleport = false)
    {
        _pixel = pixel;
        _hasDash = hasDash;
        _hasTeleport = hasTeleport;
    }

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        var gm = GameManager.GetGameManager();

        int slotIndex = 0;
        if (_hasDash) slotIndex++;
        if (_hasTeleport) slotIndex++;
        if (gm._player?.ActiveAbility?.Icon != null) slotIndex++;

        int slotX = SideMargin + slotIndex * (SlotSize + SlotGap);
        int slotY = viewport.Height - BottomMargin - SlotSize;

        _frameRect = new Rectangle(slotX, slotY, SlotSize, SlotSize);
        _iconRect = new Rectangle(
            slotX + ItemInset,
            slotY + ItemInset,
            SlotSize - ItemInset * 2,
            SlotSize - ItemInset * 2);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var gm = GameManager.GetGameManager();
        if (gm._player == null) return;

        if (PlayerInventoryHelper.GetHudExtraLives(gm._player) <= 0)
            return;

        if (!_triedLoadingHeart && _heartSprite == null)
        {
            _triedLoadingHeart = true;
            try { _heartSprite = gm._content.Load<Texture2D>("Heart"); } catch { }
        }

        Texture2D pixel = GetPixel(spriteBatch);
        Color frame = new Color(210, 210, 210, 245);
        Color background = new Color(28, 28, 28, 245);

        spriteBatch.Draw(pixel, _frameRect, frame);
        Rectangle innerRect = new Rectangle(_frameRect.X + 1, _frameRect.Y + 1, _frameRect.Width - 2, _frameRect.Height - 2);
        spriteBatch.Draw(pixel, innerRect, background);

        if (_heartSprite != null)
        {
            spriteBatch.Draw(_heartSprite, _iconRect, Color.White);
        }
        else
        {
            // fallback - draw a magenta square
            spriteBatch.Draw(pixel, _iconRect, Color.Pink);
        }
    }

    private Texture2D GetPixel(SpriteBatch spriteBatch)
    {
        if (_pixel is not null)
            return _pixel;

        var fallback = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
        fallback.SetData(new[] { Color.White });
        return fallback;
    }
}
