using System;
using System.Collections.Generic;
using Last_Hope;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Choose a registered playable character before a run; layout scales with <see cref="PlayableCharacterRegistry.Count"/>.
/// </summary>
public class CharacterSelectMenu : MenuBase
{
    private int _selectedIndex;
    private readonly Dictionary<string, Texture2D> _portraitTextures = new();

    public void Update(GameTime gameTime)
    {
        if (_font == null)
            return;

        if (InputManager.IsKeyPress(Keys.Escape) || InputManager.IsKeyPress(Keys.Q))
        {
            _state = GameState.StartMenu;
            return;
        }

        int n = PlayableCharacterRegistry.Count;
        if (n == 0)
            return;

        _selectedIndex = Math.Clamp(_selectedIndex, 0, n - 1);

        if (InputManager.IsKeyPress(Keys.A) || InputManager.IsKeyPress(Keys.Left))
            _selectedIndex = (_selectedIndex - 1 + n) % n;
        if (InputManager.IsKeyPress(Keys.D) || InputManager.IsKeyPress(Keys.Right))
            _selectedIndex = (_selectedIndex + 1) % n;

        Viewport vp = Game.GraphicsDevice.Viewport;
        for (int i = 0; i < n; i++)
        {
            if (!GetPortraitRect(vp, i).Contains(InputManager.CurrentMouseState.Position))
                continue;
            if (InputManager.LeftMousePress())
                _selectedIndex = i;
        }

        Rectangle confirmRect = GetConfirmRect(vp);
        bool confirmClick = confirmRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress();
        bool confirmKey = InputManager.IsKeyPress(Keys.Enter) || InputManager.IsKeyPress(Keys.Space);

        if (confirmClick || confirmKey)
        {
            gm.SelectedCharacter = PlayableCharacterRegistry.OrderedAt(_selectedIndex).Kind;
            gm.ResetGame();
            _state = GameState.Running;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_font == null)
            return;

        Viewport vp = Game.GraphicsDevice.Viewport;

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        spriteBatch.Draw(Pixel, new Rectangle(0, 0, vp.Width, vp.Height), new Color(12, 18, 32, 255));

        if (PlayableCharacterRegistry.Count == 0)
        {
            const string msg = "No playable characters. Add entries to PlayableCharacterRegistry.";
            Vector2 ms = _font.MeasureString(msg) * 0.45f;
            spriteBatch.DrawString(_font, msg, new Vector2(vp.Width / 2f - ms.X / 2f, vp.Height / 2f), Color.OrangeRed, 0f, Vector2.Zero, 0.45f, SpriteEffects.None, 0f);
            spriteBatch.End();
            return;
        }

        const float titleScale = 0.85f;
        string title = "CHOOSE YOUR HERO";
        Vector2 titleSize = _font.MeasureString(title) * titleScale;
        Vector2 titlePos = new Vector2(vp.Width / 2f - titleSize.X / 2f, 48f);
        spriteBatch.DrawString(_font, title, titlePos, Color.White, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        int n = PlayableCharacterRegistry.Count;
        for (int i = 0; i < n; i++)
            DrawPortraitCell(spriteBatch, vp, i);

        DrawBottomPanel(spriteBatch, vp);

        Rectangle confirmRect = GetConfirmRect(vp);
        spriteBatch.Draw(Pixel, confirmRect, new Color(40, 55, 75, 255));
        string confirm = "START";
        Vector2 cs = _font.MeasureString(confirm) * 0.55f;
        Vector2 cp = new Vector2(confirmRect.Center.X - cs.X / 2f, confirmRect.Center.Y - cs.Y / 2f);
        spriteBatch.DrawString(_font, confirm, cp, Color.White, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);

        const float hintScale = 0.38f;
        string hint = "Confirm: Space / Enter  |  Back: Esc / Q";
        Vector2 hintSize = _font.MeasureString(hint) * hintScale;
        spriteBatch.DrawString(
            _font,
            hint,
            new Vector2(vp.Width - hintSize.X - 24, vp.Height - hintSize.Y - 18),
            new Color(200, 210, 230, 255),
            0f,
            Vector2.Zero,
            hintScale,
            SpriteEffects.None,
            0f);

        spriteBatch.End();
    }

    private Texture2D? LoadPortraitTexture(string? contentKey)
    {
        if (string.IsNullOrEmpty(contentKey))
            return null;
        if (_portraitTextures.TryGetValue(contentKey, out Texture2D? cached))
            return cached;
        try
        {
            Texture2D tex = _content.Load<Texture2D>(contentKey);
            _portraitTextures[contentKey] = tex;
            return tex;
        }
        catch
        {
            return null;
        }
    }

    private Rectangle GetPortraitRect(Viewport vp, int index)
    {
        int cellW = 220;
        int cellH = 240;
        int gap = 40;
        int count = PlayableCharacterRegistry.Count;
        int totalW = count * cellW + Math.Max(0, count - 1) * gap;
        int startX = vp.Width / 2 - totalW / 2;
        int y = 130;
        return new Rectangle(startX + index * (cellW + gap), y, cellW, cellH);
    }

    private Rectangle GetConfirmRect(Viewport vp)
    {
        return new Rectangle(vp.Width / 2 - 110, 400, 220, 52);
    }

    private void DrawPortraitCell(SpriteBatch spriteBatch, Viewport vp, int index)
    {
        PlayableCharacterRegistry.Definition def = PlayableCharacterRegistry.OrderedAt(index);

        Rectangle outer = GetPortraitRect(vp, index);
        bool sel = index == _selectedIndex;
        Color border = sel ? new Color(120, 200, 255) : new Color(70, 80, 100);
        DrawThickRect(spriteBatch, Pixel, outer, border, sel ? 4 : 2);

        Rectangle inner = new Rectangle(outer.X + 8, outer.Y + 8, outer.Width - 16, outer.Height - 36);
        spriteBatch.Draw(Pixel, inner, new Color(20, 26, 40, 255));

        Texture2D? sheet = LoadPortraitTexture(def.PortraitTextureKey);
        if (sheet != null)
        {
            int fs = Math.Max(1, def.PortraitFrameSize);
            Rectangle src = new Rectangle(
                def.PortraitFrameColumn * fs,
                def.PortraitFrameRow * fs,
                fs,
                fs);

            int drawW = Math.Min(inner.Width - 20, 160);
            int drawH = drawW;
            Rectangle dest = new Rectangle(
                inner.Center.X - drawW / 2,
                inner.Y + 10,
                drawW,
                drawH);

            spriteBatch.Draw(sheet, dest, src, def.PortraitTint);
        }

        string name = def.DisplayName;
        float nameScale = 0.5f;
        Vector2 ns = _font.MeasureString(name) * nameScale;
        spriteBatch.DrawString(
            _font,
            name,
            new Vector2(outer.Center.X - ns.X / 2f, outer.Bottom - ns.Y - 10),
            Color.White,
            0f,
            Vector2.Zero,
            nameScale,
            SpriteEffects.None,
            0f);
    }

    private void DrawBottomPanel(SpriteBatch spriteBatch, Viewport vp)
    {
        int panelH = 200;
        Rectangle panel = new Rectangle(40, vp.Height - panelH - 40, vp.Width - 80, panelH);
        spriteBatch.Draw(Pixel, panel, new Color(0, 0, 0, 200));
        DrawThickRect(spriteBatch, Pixel, panel, new Color(230, 235, 245), 2);

        PlayableCharacterRegistry.Definition def = PlayableCharacterRegistry.OrderedAt(_selectedIndex);

        float headScale = 0.55f;
        Vector2 textOrigin = new Vector2(panel.X + 20, panel.Y + 16);
        spriteBatch.DrawString(_font, def.DisplayName.ToUpperInvariant(), textOrigin, Color.White, 0f, Vector2.Zero, headScale, SpriteEffects.None, 0f);

        float lineScale = 0.42f;
        float lineY = textOrigin.Y + _font.LineSpacing * headScale + 8f;
        float barMaxW = 220f;
        float rowGap = 30f;

        DrawStatRow(spriteBatch, "HP", def.MaxHp, 150f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);
        lineY += rowGap;
        DrawStatRow(spriteBatch, "ATK", def.Attack, 40f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);
        lineY += rowGap;
        DrawStatRow(spriteBatch, "SPD", def.Speed, 300f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);
        lineY += rowGap;
        DrawStatRow(spriteBatch, "CRT", def.CritPercent, 100f, new Vector2(panel.X + 20, lineY), barMaxW, lineScale);

        float descScale = 0.4f;
        string desc = $"{def.WeaponName} - {def.Tagline}";
        Vector2 descPos = new Vector2(panel.Right - 520, panel.Y + 22);
        spriteBatch.DrawString(_font, desc, descPos, new Color(210, 220, 235), 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);

        string critDmg = $"Crit damage: {def.CritDamageLabel}";
        spriteBatch.DrawString(_font, critDmg, new Vector2(descPos.X, descPos.Y + _font.LineSpacing * descScale + 6), new Color(180, 200, 255), 0f, Vector2.Zero, descScale, SpriteEffects.None, 0f);
    }

    private void DrawStatRow(
        SpriteBatch spriteBatch,
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
        spriteBatch.DrawString(_font, line, origin, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        float t = MathHelper.Clamp(value / barMax, 0f, 1f);
        const float gapAfterValue = 26f;
        float textWidth = _font.MeasureString(line).X * scale;
        int barLeft = (int)(origin.X + textWidth + gapAfterValue);
        Rectangle barBack = new Rectangle(barLeft, (int)origin.Y + 4, (int)barPixelWidth, 12);
        spriteBatch.Draw(Pixel, barBack, new Color(30, 35, 50, 255));
        Rectangle barFill = new Rectangle(barBack.X + 1, barBack.Y + 1, (int)((barBack.Width - 2) * t), barBack.Height - 2);
        if (barFill.Width > 0)
            spriteBatch.Draw(Pixel, barFill, new Color(80, 170, 255));
    }

    private static void DrawThickRect(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }
}
