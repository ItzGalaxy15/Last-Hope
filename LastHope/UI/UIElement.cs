using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

/// <summary>
/// Base type for in-world HUD widgets composed by <see cref="Hud"/>. Each element updates and draws in list order.
/// </summary>
public abstract class UIElement
{
	/// <summary>Per-frame logic; default no-op.</summary>
	public virtual void Update(GameTime gameTime, Viewport viewport)
	{
	}

	public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
