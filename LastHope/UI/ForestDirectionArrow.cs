using System;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

/// <summary>
/// Shows a pulsing arrow and text prompt that guides the player left after the village is cleared.
/// </summary>
public class ForestDirectionArrow
{
    private float _timer;

    /// <summary>
    /// Advances the prompt animation timer while the prompt should be visible.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        if (ShouldShow())
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        else
            _timer = 0f;
    }

    /// <summary>
    /// Draws the animated left arrow, shadow, and "Go left" text prompt.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        var gm = GameManager.GetGameManager();
        if (!ShouldShow() || gm._font == null || gm.Pixel == null)
            return;

        Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
        float pulse = 0.76f + (float)Math.Sin(_timer * MathHelper.TwoPi * 0.85f) * 0.16f;
        float bob = (float)Math.Sin(_timer * MathHelper.TwoPi * 1.1f) * 5f;
        int alpha = (int)MathHelper.Clamp(255f * pulse, 120f, 255f);

        Color arrowColor = new Color(255, 236, 145, alpha);
        Color arrowShadow = new Color(0, 0, 0, Math.Min(alpha, 140));
        Color textColor = new Color(255, 245, 210, alpha);
        Color textShadow = new Color(0, 0, 0, Math.Min(alpha, 180));

        // https://docs.monogame.net/api/Microsoft.Xna.Framework.MathHelper.html 
        float arrowLength = MathHelper.Clamp(viewport.Width * 0.105f, 86f, 150f);
        float headLength = MathHelper.Clamp(viewport.Width * 0.034f, 30f, 50f);
        float headHeight = headLength * 0.68f;
        float thickness = MathHelper.Clamp(viewport.Width * 0.0055f, 5f, 9f);

        Vector2 center = new Vector2(
            viewport.Width * 0.5f + bob,
            viewport.Height * 0.34f);

        Vector2 leftTip = center - new Vector2(arrowLength * 0.5f, 0f);
        Vector2 rightEnd = center + new Vector2(arrowLength * 0.5f, 0f);
        Vector2 shaftStart = leftTip + new Vector2(headLength * 0.6f, 0f);

        DrawArrow(spriteBatch, gm.Pixel, shaftStart + new Vector2(2f, 2f), rightEnd + new Vector2(2f, 2f),
            leftTip + new Vector2(2f, 2f), headLength, headHeight, thickness + 2f, arrowShadow);
        DrawArrow(spriteBatch, gm.Pixel, shaftStart, rightEnd, leftTip, headLength, headHeight, thickness, arrowColor);

        const string promptText = "Go left";
        float textScale = MathHelper.Clamp(viewport.Width / 1920f * 0.46f, 0.34f, 0.5f);
        Vector2 textSize = gm._font.MeasureString(promptText) * textScale;
        Vector2 textPos = new Vector2(
            center.X - textSize.X * 0.5f,
            center.Y + headHeight + 10f);

        spriteBatch.DrawString(gm._font, promptText, textPos + new Vector2(2f, 2f), textShadow, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(gm._font, promptText, textPos, textColor, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
    }

    /// <summary>
    /// Checks whether the player is in the cleared village and should be guided toward the forest.
    /// </summary>
    private static bool ShouldShow()
    {
        var gm = GameManager.GetGameManager();
        return gm._state == GameState.Running &&
               gm.CurrentZone == Zone.Village &&
               gm.VillageCleared &&
               gm.ForestBoundaryX > 0f;
    }

    /// <summary>
    /// Draws the arrow shaft and two angled head lines with the given pixel texture.
    /// </summary>
    private static void DrawArrow(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        Vector2 shaftStart,
        Vector2 shaftEnd,
        Vector2 leftTip,
        float headLength,
        float headHeight,
        float thickness,
        Color color)
    {
        DrawLine(spriteBatch, pixel, shaftStart, shaftEnd, thickness, color);
        DrawLine(spriteBatch, pixel, leftTip, leftTip + new Vector2(headLength, -headHeight), thickness, color);
        DrawLine(spriteBatch, pixel, leftTip, leftTip + new Vector2(headLength, headHeight), thickness, color);
    }

    /// <summary>
    /// Draws a thick line by stretching and rotating a 1x1 pixel texture.
    /// </summary>
    private static void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, float thickness, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length <= 0f)
            return;

        float rotation = (float)Math.Atan2(delta.Y, delta.X);
        spriteBatch.Draw(
            pixel,
            start,
            null,
            color,
            rotation,
            new Vector2(0f, 0.5f),
            new Vector2(length, thickness),
            SpriteEffects.None,
            0f);
    }
}
