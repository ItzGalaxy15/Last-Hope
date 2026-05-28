using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class ActiveAbilityIcon : AbilityCooldownIcon
{
    public ActiveAbilityIcon(Effect cooldownShader, Texture2D? pixel, int slotIndex)
        : base(icon: null, cooldownShader: cooldownShader, pixel: pixel, 
        getCooldownProgress: () => GameManager.GetGameManager()._player?.ActiveAbilityCooldownProgress ?? 0f, slotIndex: slotIndex)
    {
    }

    protected override Texture2D? GetIcon() => GameManager.GetGameManager()._player?.ActiveAbility?.Icon;
}