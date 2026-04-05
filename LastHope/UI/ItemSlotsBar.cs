using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.UI;

public class ItemSlotsBar : UIElement
{
	private readonly Texture2D? _pixel;
	private Texture2D? _fallbackPixel;

	private Rectangle _panelRect;
	private readonly Rectangle[] _slotFrameRects = new Rectangle[2];
	private readonly Rectangle[] _slotInnerRects = new Rectangle[2];
	private readonly Rectangle[] _slotItemRects = new Rectangle[2];
	private int _selectedSlot;

	public ItemSlotsBar(Texture2D? pixel)
	{
		_pixel = pixel;
		_selectedSlot = 0;
	}

	public override void Update(GameTime gameTime, Viewport viewport)
	{
		GameManager gm = GameManager.GetGameManager();
		InputManager input = gm.InputManager;

		if (input.IsKeyPress(Keys.D1) || input.IsKeyPress(Keys.NumPad1))
			gm.SetSelectedItemSlot(0);
		else if (input.IsKeyPress(Keys.D2) || input.IsKeyPress(Keys.NumPad2))
			gm.SetSelectedItemSlot(1);

		_selectedSlot = gm.SelectedItemSlot;

		const int sideMargin = 48;
		const int bottomMargin = 28;
		const int slotSize = 48;
		const int gap = 12;
		const int panelPadding = 10;
		const int itemInset = 9;

		int totalSlotsWidth = (slotSize * 2) + gap;
		int slotsX = viewport.Width - sideMargin - totalSlotsWidth;
		int slotsY = viewport.Height - bottomMargin - slotSize;

		_panelRect = new Rectangle(
			slotsX - panelPadding,
			slotsY - panelPadding,
			totalSlotsWidth + (panelPadding * 2),
			slotSize + (panelPadding * 2));

		_slotFrameRects[0] = new Rectangle(slotsX, slotsY, slotSize, slotSize);
		_slotFrameRects[1] = new Rectangle(slotsX + slotSize + gap, slotsY, slotSize, slotSize);

		for (int i = 0; i < 2; i++)
		{
			_slotInnerRects[i] = new Rectangle(
				_slotFrameRects[i].X + 1,
				_slotFrameRects[i].Y + 1,
				_slotFrameRects[i].Width - 2,
				_slotFrameRects[i].Height - 2);

			_slotItemRects[i] = new Rectangle(
				_slotFrameRects[i].X + itemInset,
				_slotFrameRects[i].Y + itemInset,
				_slotFrameRects[i].Width - (itemInset * 2),
				_slotFrameRects[i].Height - (itemInset * 2));
		}
	}

	public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
	{
		Texture2D pixel = GetPixel(spriteBatch);

		Color panel = new Color(0, 0, 0, 110);
		Color frame = new Color(210, 210, 210, 245);
		Color background = new Color(28, 28, 28, 245);
		Color itemColor = new Color(232, 189, 52, 255);
		Color selectedRing = new Color(255, 240, 170, 255);

		spriteBatch.Draw(pixel, _panelRect, panel);

		for (int i = 0; i < 2; i++)
		{
			spriteBatch.Draw(pixel, _slotFrameRects[i], frame);
			spriteBatch.Draw(pixel, _slotInnerRects[i], background);
			spriteBatch.Draw(pixel, _slotItemRects[i], itemColor);

			if (i == _selectedSlot)
				DrawOutline(spriteBatch, pixel, _slotFrameRects[i], 3, selectedRing);
		}
	}

	private static void DrawOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int thickness, Color color)
	{
		spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
		spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
		spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
		spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
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