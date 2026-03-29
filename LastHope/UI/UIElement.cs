using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public abstract class UIElement
{
	public virtual void Update(GameTime gameTime, Viewport viewport)
	{
	}

	public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
