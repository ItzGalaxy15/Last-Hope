using System.Collections.Generic;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class Hud
{
	private readonly List<UIElement> _elements;

	public Hud(BasePlayer? player, Texture2D pixel, Texture2D? itemSpriteSheet = null, Texture2D? dashIcon = null, Texture2D? teleportIcon = null, Effect? cooldownShader = null)
	{
		_elements = new List<UIElement>
		{
			new ExperienceBar(player, pixel),
			new HealthBar(player, pixel),
			new ItemSlotsBar(pixel, itemSpriteSheet),
			new WaveIndicator(),
			new BossWarningIndicator()
		};

		if (cooldownShader != null)
		{
			if (dashIcon != null)
				_elements.Add(new AbilityCooldownIcon(dashIcon, cooldownShader, pixel,
					() => GameManager.GetGameManager()._player?.DashCooldownProgress ?? 0f,
					slotIndex: 0));

			if (teleportIcon != null)
				_elements.Add(new AbilityCooldownIcon(teleportIcon, cooldownShader, pixel,
					() => GameManager.GetGameManager()._player?.TeleportCooldownProgress ?? 0f,
					slotIndex: 1));
		}
	}

	public void Update(GameTime gameTime, Viewport viewport)
	{
		foreach (UIElement element in _elements)
			element.Update(gameTime, viewport);
	}

	public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
	{
		foreach (UIElement element in _elements)
			element.Draw(gameTime, spriteBatch);
	}
}
