using System.Collections.Generic;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Last_Hope.UI;

/// <summary>
/// Aggregates screen-space HUD widgets (health, XP, waves, boss warning, optional ability cooldowns).
/// Owned by gameplay; see <see cref="GameManager"/> draw/update paths.
/// </summary>
public class Hud
{
	private readonly List<UIElement> _elements;
	private readonly List<HitSkillCooldownIcon> _hitIcons = new();
	private BossWarningIndicator _bossWarningIndicator;

	public Hud(BasePlayer? player, Texture2D pixel, Texture2D? itemSpriteSheet = null, Texture2D? dashIcon = null, Texture2D? teleportIcon = null, Effect? cooldownShader = null, ContentManager content = null, Texture2D? rapidFireIcon = null, Texture2D? critGuaranteeIcon = null, Texture2D? warriorAtkSpdUp = null, Texture2D? regenHpIcon = null, Texture2D? warriorDamageUp = null)
	{
		_bossWarningIndicator = new BossWarningIndicator();
		if (content != null)
		{
			_bossWarningIndicator.LoadContent(content);
		}

		_elements = new List<UIElement>
		{
			new ExperienceBar(player, pixel),
			new HealthBar(player, pixel),
			new ItemSlotsBar(pixel, itemSpriteSheet),
			new OneUpIcon(pixel),
			new WaveIndicator(),
			_bossWarningIndicator,
			new ToastNotification(pixel)
		};

		if (cooldownShader != null)
		{
			int nextSlot = 0;

			if (dashIcon != null)
			{
				_elements.Add(new AbilityCooldownIcon(dashIcon, cooldownShader, pixel,
					() => GameManager.GetGameManager()._player?.DashCooldownProgress ?? 0f,
					slotIndex: nextSlot));
				nextSlot++;
			}

			if (teleportIcon != null)
			{
				_elements.Add(new AbilityCooldownIcon(teleportIcon, cooldownShader, pixel,
					() => GameManager.GetGameManager()._player?.TeleportCooldownProgress ?? 0f,
					slotIndex: nextSlot));
				nextSlot++;
			}

			// Active ability slot (always present when cooldown shader is available)
			_elements.Add(new ActiveAbilityIcon(cooldownShader, pixel, slotIndex: nextSlot));
			nextSlot++;

			// One-up indicator (shows only when player has an extra life)
			_elements.Add(new OneUpIcon(pixel, dashIcon != null, teleportIcon != null));
			nextSlot++;

			if (rapidFireIcon != null)
			{
				var icon = new HitSkillCooldownIcon(rapidFireIcon, cooldownShader, pixel,
				() => (GameManager.GetGameManager()._player as Archer)?.RapidFireProgress ?? 0f);

				_hitIcons.Add(icon);
				_elements.Add(icon);
			}

			if (critGuaranteeIcon != null)
			{
				var icon = new HitSkillCooldownIcon(critGuaranteeIcon, cooldownShader, pixel,
				() => (GameManager.GetGameManager()._player as Archer)?.CritGuaranteeProgress ?? 0f);

				_hitIcons.Add(icon);
				_elements.Add(icon);
			}

			if (warriorAtkSpdUp != null)
			{
				var icon = new HitSkillCooldownIcon(warriorAtkSpdUp, cooldownShader, pixel,
				() => (GameManager.GetGameManager()._player as Warrior)?.AttackSpeedUpProgress ?? 0f);

				_hitIcons.Add(icon);
				_elements.Add(icon);
			}

			if (regenHpIcon != null)
			{
				var icon = new HitSkillCooldownIcon(regenHpIcon, cooldownShader, pixel,
				() => (GameManager.GetGameManager()._player as Warrior)?.RegenHpProgress ?? 0f);
				
				_hitIcons.Add(icon);
				_elements.Add(icon);
			}

			if (warriorDamageUp != null)
			{
				var icon = new HitSkillCooldownIcon(warriorDamageUp, cooldownShader, pixel,
				() => (GameManager.GetGameManager()._player as Warrior)?.DamageUpProgress ?? 0f);
				
				_hitIcons.Add(icon);
				_elements.Add(icon);
			}
		}
	}

	public void Update(GameTime gameTime, Viewport viewport)
	{
		int activeSlot = 0;
		foreach (var icon in _hitIcons)
		{
			if (icon.IsActive)
				icon.SlotIndex = activeSlot++;
		}

		foreach (UIElement element in _elements)
			element.Update(gameTime, viewport);
	}

	public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
	{
		foreach (UIElement element in _elements)
			element.Draw(gameTime, spriteBatch);
	}
}
