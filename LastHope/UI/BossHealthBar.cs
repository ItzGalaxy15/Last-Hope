using System;
using System.Linq;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

public class BossHealthBar : UIElement
{
    private readonly Texture2D? _pixel;
    private Texture2D? _fallbackPixel;
    private BaseEnemy? _activeBoss;
    private Rectangle _frameRect;
    private Rectangle _fillRect;

    public BossHealthBar(Texture2D? pixel)
    {
        _pixel = pixel;
    }

    public override void Update(GameTime gameTime, Viewport viewport)
    {
        _activeBoss = FindActiveBoss();

        const int bottomMargin = 28;
        const int barHeight = 18;
        int barWidth = Math.Clamp((int)(viewport.Width * 0.58f), 280, 620);
        int x = (viewport.Width - barWidth) / 2;
        int y = viewport.Height - bottomMargin - barHeight;

        _frameRect = new Rectangle(x, y, barWidth, barHeight);

        float progress = 0f;
        if (_activeBoss is not null && _activeBoss.CurrentMaxHp > 0f)
            progress = MathHelper.Clamp(_activeBoss._currentHp / _activeBoss.CurrentMaxHp, 0f, 1f);

        int fillWidth = (int)MathF.Round((_frameRect.Width - 4) * progress);
        fillWidth = Math.Clamp(fillWidth, 0, _frameRect.Width - 4);

        _fillRect = new Rectangle(
            _frameRect.X + 2,
            _frameRect.Y + 2,
            fillWidth,
            _frameRect.Height - 4);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_activeBoss is null)
            return;

        Texture2D pixel = GetPixel(spriteBatch);
        Color frame = new Color(160, 35, 35, 255);
        Color background = new Color(12, 12, 14, 235);
        Color fill = new Color(215, 45, 45, 255);
        Color highlight = new Color(255, 110, 90, 180);

        spriteBatch.Draw(pixel, _frameRect, frame);

        Rectangle inner = new Rectangle(
            _frameRect.X + 1,
            _frameRect.Y + 1,
            Math.Max(1, _frameRect.Width - 2),
            Math.Max(1, _frameRect.Height - 2));
        spriteBatch.Draw(pixel, inner, background);

        if (_fillRect.Width <= 0)
            return;

        spriteBatch.Draw(pixel, _fillRect, fill);

        Rectangle shine = new Rectangle(
            _fillRect.X,
            _fillRect.Y,
            _fillRect.Width,
            Math.Max(1, _fillRect.Height / 3));
        spriteBatch.Draw(pixel, shine, highlight);
    }

    private static BaseEnemy? FindActiveBoss()
    {
        var gm = GameManager.GetGameManager();
        return gm._gameObjects
            .OfType<BaseEnemy>()
            .FirstOrDefault(enemy => enemy is Boss or SpiderBoss);
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
