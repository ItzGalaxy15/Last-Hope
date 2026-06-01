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

        // Match XP bar width calculation
        int usableWidth = viewport.Width - (sideMargin * 2);
        int badgeSpace = (20 * 2) + 18; // same as XP bar
        int xpBarWidth = Math.Max(260, usableWidth - badgeSpace);

        // Health bar settings
        int healthWidth = xpBarWidth / 2;

        int healthX = sideMargin;

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

        Color frame = new Color(130, 140, 150, 255);
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