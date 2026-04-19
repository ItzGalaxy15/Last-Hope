using System;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class ExperienceBar : UIElement
{
	private readonly BasePlayer? _player;
	private readonly Texture2D? _pixel;
	private Texture2D? _fallbackPixel;

	private Rectangle _frameRect;
	private Rectangle _fillRect;
	private Rectangle _hudPanelRect;
	private Point _badgeCenter;
	private int _badgeRadius;

	public ExperienceBar(BasePlayer? player, Texture2D? pixel)
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
		const int badgeGap = 18;
		const int badgeRadius = 20;

		int usableWidth = viewport.Width - (sideMargin * 2);
		int reservedForBadge = (badgeRadius * 2) + badgeGap;
		int barWidth = Math.Max(260, usableWidth - reservedForBadge);

		int totalHudWidth = barWidth + reservedForBadge;
		int startX = Math.Max(sideMargin, (viewport.Width - totalHudWidth) / 2);
		_hudPanelRect = new Rectangle(
			Math.Max(0, startX - 18),
			Math.Max(0, topMargin - 12),
			Math.Min(viewport.Width, totalHudWidth + 36),
			barHeight + 24);

		_frameRect = new Rectangle(startX, topMargin, barWidth, barHeight);

		BasePlayer? player = GetActivePlayer();
		float progress = player?.ExperienceProgress ?? 0f;
		int fillWidth = (int)MathF.Round((_frameRect.Width - 4) * progress);
		fillWidth = Math.Clamp(fillWidth, 0, _frameRect.Width - 4);
		_fillRect = new Rectangle(_frameRect.X + 2, _frameRect.Y + 2, fillWidth, _frameRect.Height - 4);

		_badgeRadius = badgeRadius;
		_badgeCenter = new Point(_frameRect.Right + badgeGap + _badgeRadius, _frameRect.Center.Y);
	}

	public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
	{
		Texture2D pixel = GetPixel(spriteBatch);
		BasePlayer? player = GetActivePlayer();
		float flash = player?.LevelUpFlashProgress ?? 0f;

		Color frame = new Color(130, 140, 150, 255); // Matched premium metal border
		Color background = new Color(28, 28, 28, 245);
		Color fill = new Color(120, 200, 255, 255);
		Color panel = new Color(0, 0, 0, 110);
		Color badgeBase = Color.Lerp(new Color(60, 60, 60, 225), new Color(255, 205, 80, 245), flash);
		Color badgeRing = Color.Lerp(frame, new Color(255, 240, 170, 255), flash);
		Color levelColor = Color.Lerp(new Color(235, 235, 235, 255), new Color(255, 255, 185, 255), flash);

		spriteBatch.Draw(pixel, _hudPanelRect, panel);

		spriteBatch.Draw(pixel, _frameRect, frame);

		Rectangle inner = new Rectangle(
			_frameRect.X + 1,
			_frameRect.Y + 1,
			Math.Max(1, _frameRect.Width - 2),
			Math.Max(1, _frameRect.Height - 2));
		spriteBatch.Draw(pixel, inner, background);

		if (_fillRect.Width > 0)
			spriteBatch.Draw(pixel, _fillRect, fill);

		DrawFilledCircle(spriteBatch, _badgeCenter, _badgeRadius, badgeBase);
		DrawCircleOutline(spriteBatch, _badgeCenter, _badgeRadius, 2, badgeRing);
		DrawLevelNumber(spriteBatch, _badgeCenter, player?.Level ?? 0, levelColor);
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
