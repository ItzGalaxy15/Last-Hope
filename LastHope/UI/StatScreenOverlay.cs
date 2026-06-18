using System;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI;

// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html
// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.MathHelper.html
public class StatScreenOverlay
{
    private readonly Texture2D _pixel;
    private float _entranceProgress;
    private const float EntranceSpeed = 4f;

    private const float TitleScale = 0.65f;
    private const float RowScale   = 0.52f;
    private const float HintScale  = 0.40f;
    private const int   PadX       = 16;
    private const int   PadY       = 14;
    private const int   RowH       = 32;

    public StatScreenOverlay(Texture2D pixel) => _pixel = pixel;

    public void Update(GameTime gameTime)
    {
        _entranceProgress = Math.Min(1f,
            _entranceProgress + (float)gameTime.ElapsedGameTime.TotalSeconds * EntranceSpeed);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var gm     = GameManager.GetGameManager();
        var vp     = spriteBatch.GraphicsDevice.Viewport;
        var player = gm._player;
        if (gm._font == null || player == null) return;

        string title  = $"Level {player.Level}";
        string[] rows =
        {
            $"Max HP : {player.CurrentMaxHp:0.#}",
            $"Damage : {player.CurrentDamage}",
            $"Crit   : {player.CurrentCritChance * 100f:0.#}%",
            $"Speed  : {player.CurrentSpeed:0.#}",
            $"Haste  : {player.CurrentHaste:0.##}s",
        };

        // Size panel to widest line
        float maxW = gm.MeasureUiString(gm._font, title, TitleScale).X;
        foreach (var r in rows)
            maxW = Math.Max(maxW, gm.MeasureUiString(gm._font, r, RowScale).X);

        int titleH  = (int)gm.MeasureUiString(gm._font, title, TitleScale).Y;
        string closeHint = $"[{KeybindStore.FormatBinding(KeybindId.StatScreen)}] Close";
        int hintH   = (int)gm.MeasureUiString(gm._font, closeHint, HintScale).Y;
        int panelW  = (int)maxW + PadX * 2 + 24;
        int panelH  = PadY * 2 + titleH + 10 + rows.Length * RowH + 6 + hintH;

        const int ScreenMargin = 12;
        float ease   = 1f - MathF.Pow(1f - _entranceProgress, 3f);
        int   panelX = vp.Width - (int)((panelW + ScreenMargin) * ease);
        int   panelY = (vp.Height - panelH) / 2;

        spriteBatch.Draw(_pixel, new Rectangle(panelX, panelY, panelW, panelH),
            new Color(10, 12, 16, 230));
        DrawBorder(spriteBatch, panelX, panelY, panelW, panelH, new Color(80, 90, 110));

        int x = panelX + PadX;
        int y = panelY + PadY;

        gm.DrawUiString(spriteBatch, gm._font, title,
            new Vector2(x, y), new Color(255, 215, 0), TitleScale);
        y += titleH + 6;

        spriteBatch.Draw(_pixel, new Rectangle(panelX + 8, y, panelW - 16, 1),
            new Color(60, 70, 85));
        y += 10;

        foreach (var r in rows)
        {
            gm.DrawUiString(spriteBatch, gm._font, r,
                new Vector2(x, y), Color.White, RowScale);
            y += RowH;
        }

        y += 4;
        gm.DrawUiString(spriteBatch, gm._font, closeHint,
            new Vector2(x, y), new Color(80, 90, 110), HintScale);
    }
    // stackoverflow.com/questions/23305577/draw-rectangle-in-monogame
    private void DrawBorder(SpriteBatch sb, int x, int y, int w, int h, Color c)
    {
        sb.Draw(_pixel, new Rectangle(x,         y,         w, 1), c);
        sb.Draw(_pixel, new Rectangle(x,         y + h - 1, w, 1), c);
        sb.Draw(_pixel, new Rectangle(x,         y,         1, h), c);
        sb.Draw(_pixel, new Rectangle(x + w - 1, y,         1, h), c);
    }
}
