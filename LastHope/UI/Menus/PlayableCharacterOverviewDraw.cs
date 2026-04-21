using System.Collections.Generic;
using Last_Hope;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Shared layout for character select and the Characters roster (portraits + bottom stats panel).
/// </summary>
internal static class PlayableCharacterOverviewDraw
{
    private const int CellW = 220;
    private const int CellH = 240;
    private const int CellGap = 40;
    private const int PortraitRowY = 100;
    /// <summary>How many portrait cells fit on one row (roster / select grid).</summary>
    internal const int PortraitsPerRow = 7;
    private const int PortraitRowGap = 20;
    private const int BottomPanelHeight = 200;
    /// <summary>Space from viewport bottom to the stats panel (larger keeps footer + bottom stats readable).</summary>
    private const int BottomPanelMargin = 76;
    private const int FooterHintBottomMargin = 52;
    private const int ConfirmButtonWidth = 220;
    private const int ConfirmButtonHeight = 52;
    private const int MarginBelowPortraitGrid = 16;

    internal static int GetPortraitGridRowCount(int totalCount)
    {
        if (totalCount <= 0)
            return 0;
        return (totalCount + PortraitsPerRow - 1) / PortraitsPerRow;
    }

    /// <summary>Bottom Y of the portrait grid (exclusive of content below).</summary>
    internal static int GetPortraitGridBottomY(int totalCount)
    {
        int rows = GetPortraitGridRowCount(totalCount);
        if (rows == 0)
            return PortraitRowY;
        return PortraitRowY + rows * CellH + System.Math.Max(0, rows - 1) * PortraitRowGap;
    }

    internal static Rectangle GetPortraitRect(Viewport vp, int index)
    {
        int n = PlayableCharacterRegistry.Count;
        if (n <= 0 || index < 0 || index >= n)
            return Rectangle.Empty;

        int row = index / PortraitsPerRow;
        int col = index % PortraitsPerRow;
        int countThisRow = System.Math.Min(PortraitsPerRow, n - row * PortraitsPerRow);
        int totalW = countThisRow * CellW + System.Math.Max(0, countThisRow - 1) * CellGap;
        int startX = vp.Width / 2 - totalW / 2;
        int x = startX + col * (CellW + CellGap);
        int y = PortraitRowY + row * (CellH + PortraitRowGap);
        return new Rectangle(x, y, CellW, CellH);
    }

    internal static Rectangle GetConfirmRect(Viewport vp)
    {
        int n = PlayableCharacterRegistry.Count;
        int panelTop = vp.Height - BottomPanelMargin - BottomPanelHeight;
        int idealTop = GetPortraitGridBottomY(n) + MarginBelowPortraitGrid;
        int confirmTop = System.Math.Min(idealTop, panelTop - ConfirmButtonHeight - 8);
        confirmTop = System.Math.Max(confirmTop, PortraitRowY + 40);
        int confirmLeft = vp.Width / 2 - ConfirmButtonWidth / 2;
        return new Rectangle(confirmLeft, confirmTop, ConfirmButtonWidth, ConfirmButtonHeight);
    }

    internal static Texture2D? LoadPortraitTexture(
        ContentManager content,
        Dictionary<string, Texture2D> cache,
        string? contentKey)
    {
        if (string.IsNullOrEmpty(contentKey))
            return null;
        if (cache.TryGetValue(contentKey, out Texture2D? cached))
            return cached;
        try
        {
            Texture2D tex = content.Load<Texture2D>(contentKey);
            cache[contentKey] = tex;
            return tex;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Draws overview inside an already-open <see cref="SpriteBatch"/> (caller owns Begin/End).</summary>
    internal static void Draw(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Texture2D pixel,
        ContentManager content,
        Dictionary<string, Texture2D> portraitCache,
        Viewport vp,
        int selectedIndex,
        string title,
        float titleScale,
        bool showStartButton,
        string footerHint)
    {
        spriteBatch.Draw(pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(12, 18, 32, 255));

        GameManager gm = GameManager.GetGameManager();

        if (PlayableCharacterRegistry.Count == 0)
        {
            const string msg = "No playable characters. Add entries to PlayableCharacterRegistry.";
            Vector2 ms = gm.MeasureUiString(font, msg, 0.45f);
            gm.DrawUiString(
                spriteBatch,
                font,
                msg,
                new Vector2(vp.Width / 2f - ms.X / 2f, vp.Height / 2f),
                Color.OrangeRed,
                0.45f);
            return;
        }

        selectedIndex = System.Math.Clamp(selectedIndex, 0, PlayableCharacterRegistry.Count - 1);

        Vector2 titleSize = gm.MeasureUiString(font, title, titleScale);
        Vector2 titlePos = new Vector2(vp.Width / 2f - titleSize.X / 2f, 48f);
        gm.DrawUiString(spriteBatch, font, title, titlePos, Color.White, titleScale);

        int n = PlayableCharacterRegistry.Count;
        for (int i = 0; i < n; i++)
            DrawPortraitCell(spriteBatch, gm, font, pixel, content, portraitCache, vp, i, selectedIndex);

        DrawBottomPanel(spriteBatch, gm, font, pixel, vp, selectedIndex);

        if (showStartButton)
        {
            Rectangle confirmRect = GetConfirmRect(vp);
            spriteBatch.Draw(pixel, confirmRect, new Color(40, 55, 75, 255));
            const string confirm = "START";
            Vector2 cs = gm.MeasureUiString(font, confirm, 0.55f);
            Vector2 cp = new Vector2(confirmRect.Center.X - cs.X / 2f, confirmRect.Center.Y - cs.Y / 2f);
            gm.DrawUiString(spriteBatch, font, confirm, cp, Color.White, 0.55f);
        }

        const float hintScale = 0.38f;
        Vector2 hintSize = gm.MeasureUiString(font, footerHint, hintScale);
        gm.DrawUiString(
            spriteBatch,
            font,
            footerHint,
            new Vector2(vp.Width - hintSize.X - 24, vp.Height - hintSize.Y - FooterHintBottomMargin),
            new Color(200, 210, 230, 255),
            hintScale);
    }

    private static void DrawPortraitCell(
        SpriteBatch spriteBatch,
        GameManager gm,
        SpriteFont font,
        Texture2D pixel,
        ContentManager content,
        Dictionary<string, Texture2D> portraitCache,
        Viewport vp,
        int index,
        int selectedIndex)
    {
        PlayableCharacterRegistry.Definition def = PlayableCharacterRegistry.OrderedAt(index);

        Rectangle outer = GetPortraitRect(vp, index);
        bool sel = index == selectedIndex;
        Color border = sel ? new Color(120, 200, 255) : new Color(70, 80, 100);
        DrawThickRect(spriteBatch, pixel, outer, border, sel ? 4 : 2);

        Rectangle inner = new Rectangle(outer.X + 8, outer.Y + 8, outer.Width - 16, outer.Height - 36);
        spriteBatch.Draw(pixel, inner, new Color(20, 26, 40, 255));

        Texture2D? sheet = LoadPortraitTexture(content, portraitCache, def.PortraitTextureKey);
        if (sheet != null)
        {
            int fs = System.Math.Max(1, def.PortraitFrameSize);
            Rectangle src = new Rectangle(
                def.PortraitFrameColumn * fs,
                def.PortraitFrameRow * fs,
                fs,
                fs);

            int drawW = System.Math.Min(inner.Width - 20, 160);
            int drawH = drawW;
            Rectangle dest = new Rectangle(
                inner.Center.X - drawW / 2,
                inner.Y + 10,
                drawW,
                drawH);

            spriteBatch.Draw(sheet, dest, src, def.PortraitTint);
        }

        string name = def.DisplayName;
        const float nameScale = 0.5f;
        Vector2 ns = gm.MeasureUiString(font, name, nameScale);
        gm.DrawUiString(
            spriteBatch,
            font,
            name,
            new Vector2(outer.Center.X - ns.X / 2f, outer.Bottom - ns.Y - 10),
            Color.White,
            nameScale);
    }

    private static void DrawBottomPanel(
        SpriteBatch spriteBatch,
        GameManager gm,
        SpriteFont font,
        Texture2D pixel,
        Viewport vp,
        int selectedIndex)
    {
        int panelH = BottomPanelHeight;
        Rectangle panel = new Rectangle(40, vp.Height - panelH - BottomPanelMargin, vp.Width - 80, panelH);
        spriteBatch.Draw(pixel, panel, new Color(0, 0, 0, 200));
        DrawThickRect(spriteBatch, pixel, panel, new Color(230, 235, 245), 2);

        PlayableCharacterRegistry.Definition def = PlayableCharacterRegistry.OrderedAt(selectedIndex);

        const float headScale = 0.55f;
        Vector2 textOrigin = new Vector2(panel.X + 20, panel.Y + 16);
        gm.DrawUiString(spriteBatch, font, def.DisplayName.ToUpperInvariant(), textOrigin, Color.White, headScale);

        const float lineScale = 0.42f;
        float lineSpacing = gm.FontBitmap != null ? gm.FontBitmap.LineHeight : font.LineSpacing;
        float lineY = textOrigin.Y + lineSpacing * headScale + 8f;
        const float barMaxW = 220f;
        const float rowGap = 30f;

        DrawStatRow(spriteBatch, gm, font, pixel, "HP", def.MaxHp, 150f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);
        lineY += rowGap;
        DrawStatRow(spriteBatch, gm, font, pixel, "ATK", def.Attack, 40f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);
        lineY += rowGap;
        DrawStatRow(spriteBatch, gm, font, pixel, "SPD", def.Speed, 300f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);
        lineY += rowGap;
        DrawStatRow(spriteBatch, gm, font, pixel, "CRT", def.CritPercent, 100f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);

        const float descScale = 0.4f;
        string desc = $"{def.WeaponName} - {def.Tagline}";
        Vector2 descPos = new Vector2(panel.Right - 520, panel.Y + 22);
        gm.DrawUiString(spriteBatch, font, desc, descPos, new Color(210, 220, 235), descScale);

        string critDmg = $"Crit damage: {def.CritDamageLabel}";
        float descLineSpacing = gm.FontBitmap != null ? gm.FontBitmap.LineHeight : font.LineSpacing;
        gm.DrawUiString(
            spriteBatch,
            font,
            critDmg,
            new Vector2(descPos.X, descPos.Y + descLineSpacing * descScale + 6),
            new Color(180, 200, 255),
            descScale);
    }

    private static void DrawStatRow(
        SpriteBatch spriteBatch,
        GameManager gm,
        SpriteFont font,
        Texture2D pixel,
        string label,
        float value,
        float barMax,
        Vector2 origin,
        float barPixelWidth,
        float scale)
    {
        string valueText = label switch
        {
            "CRT" => $"{value:0}%",
            "SPD" => $"{value:0}",
            _ => value % 1f == 0f ? $"{value:0}" : $"{value:0.##}"
        };

        string line = $"{label}: {valueText}";
        gm.DrawUiString(spriteBatch, font, line, origin, Color.White, scale);

        float t = MathHelper.Clamp(value / barMax, 0f, 1f);
        const float gapAfterValue = 26f;
        float textWidth = gm.MeasureUiString(font, line, scale).X;
        int barLeft = (int)(origin.X + textWidth + gapAfterValue);
        Rectangle barBack = new Rectangle(barLeft, (int)origin.Y + 4, (int)barPixelWidth, 12);
        spriteBatch.Draw(pixel, barBack, new Color(30, 35, 50, 255));
        Rectangle barFill = new Rectangle(barBack.X + 1, barBack.Y + 1, (int)((barBack.Width - 2) * t), barBack.Height - 2);
        if (barFill.Width > 0)
            spriteBatch.Draw(pixel, barFill, new Color(80, 170, 255));
    }

    private static void DrawThickRect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }
}
