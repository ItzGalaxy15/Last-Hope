using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class HitSkillCooldownIcon : AbilityCooldownIcon
{
    public HitSkillCooldownIcon(Texture2D icon, Effect cooldownShader, Texture2D? pixel, Func<float> getProgress)
        : base(icon, cooldownShader, pixel, getProgress)
    {
    }

    public bool IsActive => _getCooldownProgress() > 0f;

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        int slotX = SideMargin + _slotIndex * (SlotSize + SlotGap);
        int slotY = viewport.Height - BottomMargin - SlotSize - (SlotSize + SlotGap + 6);

        _frameRect = new Rectangle(slotX, slotY, SlotSize, SlotSize);
        _iconRect  = new Rectangle(
            slotX + ItemInset,
            slotY + ItemInset,
            SlotSize - ItemInset * 2,
            SlotSize - ItemInset * 2);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        float progress = _getCooldownProgress();
        if (progress <= 0f)
            return;

        // Invert for the shader: hit skills drain from full to empty
        // so we pass 0 (fully lit) when active and rising toward 1 (darkened) as it expires
        _cooldownShader.Parameters["CooldownPercent"].SetValue(1f - progress);

        base.Draw(gameTime, spriteBatch);
    }
}