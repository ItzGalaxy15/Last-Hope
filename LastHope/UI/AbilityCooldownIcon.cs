using System;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class AbilityCooldownIcon : UIElement
{
    private readonly Texture2D _icon;
    private readonly Effect _cooldownShader;
    private readonly Texture2D? _pixel;
    private readonly Func<float> _getCooldownProgress;
    private readonly int _slotIndex;
    private Texture2D? _fallbackPixel;

    private Rectangle _iconRect;
    private Rectangle _frameRect;

    private const int SlotSize = 64;
    private const int SlotGap = 3;
    private const int SideMargin = 48;
    private const int BottomMargin = 28;
    private const int ItemInset = 6;

    public AbilityCooldownIcon(Texture2D icon, Effect cooldownShader, Texture2D? pixel, Func<float> getCooldownProgress, int slotIndex = 0)
    {
        _icon = icon;
        _cooldownShader = cooldownShader;
        _pixel = pixel;
        _getCooldownProgress = getCooldownProgress;
        _slotIndex = slotIndex;
    }

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        int slotX = SideMargin + _slotIndex * (SlotSize + SlotGap);
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
        GameManager gm = GameManager.GetGameManager();

        float cooldown = _getCooldownProgress();

        Color frame = new Color(210, 210, 210, 245);
        Color background = new Color(28, 28, 28, 245);

        Texture2D pixel = GetPixel(spriteBatch);
        spriteBatch.Draw(pixel, _frameRect, frame);

        Rectangle innerRect = new Rectangle(
            _frameRect.X + 1, _frameRect.Y + 1,
            _frameRect.Width - 2, _frameRect.Height - 2);
        spriteBatch.Draw(pixel, innerRect, background);

        spriteBatch.End();
        _cooldownShader.Parameters["CooldownPercent"].SetValue(cooldown);
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _cooldownShader);
        spriteBatch.Draw(_icon, _iconRect, Color.White);
        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
    }

    private Texture2D GetPixel(SpriteBatch spriteBatch)
    {
        if (_pixel is not null)
            return _pixel;
        if (_fallbackPixel is null)
        {
            _fallbackPixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _fallbackPixel.SetData(new[] { Color.White });
        }
        return _fallbackPixel;
    }
}
