using System.Collections.Generic;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class Hud
{
	private readonly List<UIElement> _elements;

	public Hud(BasePlayer player, Texture2D pixel)
	{
		_elements = new List<UIElement>
		{
			new ExperienceBar(player, pixel),
			new HealthBar(player, pixel),
			new ItemSlotsBar(pixel)
		};
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
