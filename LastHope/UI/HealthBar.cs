using System;
using Last_Hope;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class HealthBar : UIElement
{
	private readonly BasePlayer? _player;
	private readonly Texture2D? _pixel;
	private Texture2D? _fallbackPixel;
	private Texture2D? _heartSprite;
	private bool _triedLoadingHeart;
    private Rectangle _healthFrameRect;
    private Rectangle _healthFillRect;
	private Rectangle _frameRect;
	private Rectangle _fillRect;
	private Rectangle _hudPanelRect;
	private Point _badgeCenter;
	private int _badgeRadius;

	public HealthBar(BasePlayer? player, Texture2D? pixel)
	{
		_player = player;
		_pixel = pixel;
	}

	private BasePlayer? GetActivePlayer()
	{
		return GameManager.GetGameManager()._player ?? _player;
	}

	public override void Update(GameTime gameTime, Viewport viewport)
    {
        const int topMargin = 16;
        const int sideMargin = 48;
        const int barHeight = 20;
        const int spacingY = 10;
        const int healthHeight = 16;

        // --- Match XP bar width calculation ---
        int usableWidth = viewport.Width - (sideMargin * 2);
        int badgeSpace = (20 * 2) + 18; // same as XP bar
        int xpBarWidth = Math.Max(260, usableWidth - badgeSpace);

        // --- Health bar settings ---
        int healthWidth = xpBarWidth / 2;

        // ✅ LEFT ALIGNED
        int healthX = sideMargin;

        // ✅ BELOW XP BAR
        int healthY = topMargin + barHeight + spacingY;

        _healthFrameRect = new Rectangle(healthX, healthY, healthWidth, healthHeight);

		BasePlayer? player = GetActivePlayer();
		float progress = player?.HealthProgress ?? 0f;

        int fillWidth = (int)MathF.Round((_healthFrameRect.Width - 4) * progress);
        fillWidth = Math.Clamp(fillWidth, 0, _healthFrameRect.Width - 4);

        _healthFillRect = new Rectangle(
            _healthFrameRect.X + 2,
            _healthFrameRect.Y + 2,
            fillWidth,
            _healthFrameRect.Height - 4);
    }

	public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        Texture2D pixel = GetPixel(spriteBatch);
		BasePlayer? player = GetActivePlayer();
		float healthProgress = player?.HealthProgress ?? 0f;

        Color frame = new Color(130, 140, 150, 255); // Matched premium metal border
        Color background = new Color(20, 20, 20, 245);
        Color fill = Color.Lerp(Color.Red, Color.LimeGreen, healthProgress);

        // Frame
        spriteBatch.Draw(pixel, _healthFrameRect, frame);

        // Background
        Rectangle inner = new Rectangle(
            _healthFrameRect.X + 1,
            _healthFrameRect.Y + 1,
            Math.Max(1, _healthFrameRect.Width - 2),
            Math.Max(1, _healthFrameRect.Height - 2));

        spriteBatch.Draw(pixel, inner, background);

        // Fill
        if (_healthFillRect.Width > 0)
            spriteBatch.Draw(pixel, _healthFillRect, fill);

        if (player is not null && PlayerInventoryHelper.GetHudExtraLives(player) > 0)
        {
            if (!_triedLoadingHeart && _heartSprite == null)
            {
                _triedLoadingHeart = true;
                try { _heartSprite = GameManager.GetGameManager()._content.Load<Texture2D>("Heart"); } catch { }
            }
            
            int badgeX = _healthFrameRect.Right + 12;
            int badgeY = _healthFrameRect.Y + (_healthFrameRect.Height / 2) - 8;
            
            if (_heartSprite != null)
            {
                spriteBatch.Draw(_heartSprite, new Rectangle(badgeX, badgeY, 16, 16), Color.White);
            }
            
            SpriteFont font = GameManager.GetGameManager()._font;
            if (font != null)
            {
                spriteBatch.DrawString(font, "+1", new Vector2(badgeX + 20, badgeY - 2), Color.LimeGreen, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }
        }
    }

	private void DrawLevelNumber(SpriteBatch spriteBatch, Point center, int level, Color color)
	{
		Texture2D pixel = GetPixel(spriteBatch);
		string text = Math.Max(0, level).ToString();

		const int digitW = 8;
		const int digitH = 12;
		const int thickness = 2;
		const int spacing = 3;

		int totalWidth = (text.Length * digitW) + ((text.Length - 1) * spacing);
		int startX = center.X - (totalWidth / 2);
		int startY = center.Y - (digitH / 2);

		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsDigit(text[i]))
				continue;

			int digit = text[i] - '0';
			DrawSevenSegmentDigit(
				spriteBatch,
				pixel,
				new Point(startX + i * (digitW + spacing), startY),
				digit,
				digitW,
				digitH,
				thickness,
				color);
		}
	}

	private void DrawSevenSegmentDigit(
		SpriteBatch spriteBatch,
		Texture2D pixel,
		Point topLeft,
		int digit,
		int width,
		int height,
		int thickness,
		Color color)
	{
		bool a = digit is 0 or 2 or 3 or 5 or 6 or 7 or 8 or 9;
		bool b = digit is 0 or 1 or 2 or 3 or 4 or 7 or 8 or 9;
		bool c = digit is 0 or 1 or 3 or 4 or 5 or 6 or 7 or 8 or 9;
		bool d = digit is 0 or 2 or 3 or 5 or 6 or 8 or 9;
		bool e = digit is 0 or 2 or 6 or 8;
		bool f = digit is 0 or 4 or 5 or 6 or 8 or 9;
		bool g = digit is 2 or 3 or 4 or 5 or 6 or 8 or 9;

		int midY = topLeft.Y + (height / 2) - (thickness / 2);
		int bottomY = topLeft.Y + height - thickness;
		int rightX = topLeft.X + width - thickness;
		int upperVertHeight = (height / 2) - thickness;
		int lowerVertY = midY + thickness;
		int lowerVertHeight = height - (height / 2) - thickness;

		if (a)
			spriteBatch.Draw(pixel, new Rectangle(topLeft.X, topLeft.Y, width, thickness), color);
		if (g)
			spriteBatch.Draw(pixel, new Rectangle(topLeft.X, midY, width, thickness), color);
		if (d)
			spriteBatch.Draw(pixel, new Rectangle(topLeft.X, bottomY, width, thickness), color);

		if (f)
			spriteBatch.Draw(pixel, new Rectangle(topLeft.X, topLeft.Y + thickness, thickness, upperVertHeight), color);
		if (b)
			spriteBatch.Draw(pixel, new Rectangle(rightX, topLeft.Y + thickness, thickness, upperVertHeight), color);
		if (e)
			spriteBatch.Draw(pixel, new Rectangle(topLeft.X, lowerVertY, thickness, lowerVertHeight), color);
		if (c)
			spriteBatch.Draw(pixel, new Rectangle(rightX, lowerVertY, thickness, lowerVertHeight), color);
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

	private void DrawFilledCircle(SpriteBatch spriteBatch, Point center, int radius, Color color)
	{
		Texture2D pixel = GetPixel(spriteBatch);
		for (int y = -radius; y <= radius; y++)
		{
			int x = (int)MathF.Sqrt((radius * radius) - (y * y));
			Rectangle row = new Rectangle(center.X - x, center.Y + y, (x * 2) + 1, 1);
			spriteBatch.Draw(pixel, row, color);
		}
	}

	private void DrawCircleOutline(SpriteBatch spriteBatch, Point center, int radius, int thickness, Color color)
	{
		Texture2D pixel = GetPixel(spriteBatch);
		int innerRadius = Math.Max(0, radius - thickness);
		for (int y = -radius; y <= radius; y++)
		{
			int outerX = (int)MathF.Sqrt((radius * radius) - (y * y));
			int innerX = (int)MathF.Sqrt(Math.Max(0, (innerRadius * innerRadius) - (y * y)));

			int leftWidth = Math.Max(0, outerX - innerX);
			if (leftWidth > 0)
			{
				spriteBatch.Draw(pixel, new Rectangle(center.X - outerX, center.Y + y, leftWidth, 1), color);
				spriteBatch.Draw(pixel, new Rectangle(center.X + innerX + 1, center.Y + y, leftWidth, 1), color);
			}
		}
	}
}
