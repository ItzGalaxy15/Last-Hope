using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;

namespace Last_Hope.UI.Menus;

public abstract class MenuBase
{
    private const int FrameSize = 32;
    private const int FrameCount = 3;
    private const double FrameDuration = 0.15;
    private static Texture2D _lmbTexture;
    private static Texture2D _keysTexture;

    private const int KeyRowW = 0;
    private const int KeyRowA = 1;
    private const int KeyRowS = 2;
    private const int KeyRowD = 3;
    private const int KeyRowT = 4;
    private const int KeyRow1 = 5;
    private const int KeyRow2 = 6;
    private const int KeyRowShift = 7;

    private enum SegKind { Text, Key, Lmb, ItemSprite, BoundKey }
    private readonly struct Segment
    {
        public readonly SegKind Kind;
        public readonly string Text;
        public readonly int KeyRow;
        public readonly ItemType Item;
        public readonly KeybindId? BindId;
        private Segment(SegKind k, string t, int r, ItemType i, KeybindId? bindId)
        {
            Kind = k;
            Text = t;
            KeyRow = r;
            Item = i;
            BindId = bindId;
        }
        public static Segment T(string t) => new Segment(SegKind.Text, t, 0, ItemType.None, null);
        public static Segment K(int row) => new Segment(SegKind.Key, null, row, ItemType.None, null);
        public static Segment L() => new Segment(SegKind.Lmb, null, 0, ItemType.None, null);
        public static Segment I(ItemType item) => new Segment(SegKind.ItemSprite, null, 0, item, null);
        public static Segment B(KeybindId id) => new Segment(SegKind.BoundKey, null, 0, ItemType.None, id);
    }

    private static readonly Segment[][] ControlsLines =
    {
        new[] { Segment.T("Controls") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Movement") },
        new[] {
            Segment.K(KeyRowW), Segment.T(" "),
            Segment.K(KeyRowA), Segment.T(" "),
            Segment.K(KeyRowS), Segment.T(" "),
            Segment.K(KeyRowD), Segment.T(" -> Move"),
        },
        new[] { Segment.K(KeyRowShift), Segment.T(" -> Dash") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Combat") },
        new[] { Segment.L(), Segment.T(" -> Attack") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Hotbar") },
        new[] {
            Segment.K(KeyRow1), Segment.T(" / "),
            Segment.K(KeyRow2), Segment.T(" -> Select slot"),
        },
        new[] { Segment.T("G -> Place item") },
        new[] { Segment.K(KeyRowT), Segment.T(" -> Throw item") },
    };

    private static Segment[][] BuildSavedControlsLines() => new Segment[][]
    {
        new[] { Segment.T("Controls") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Movement") },
        new[] {
            Segment.B(KeybindId.MoveUp), Segment.T(" "),
            Segment.B(KeybindId.MoveDown), Segment.T(" "),
            Segment.B(KeybindId.MoveLeft), Segment.T(" "),
            Segment.B(KeybindId.MoveRight), Segment.T(" -> Move"),
        },
        new[] { Segment.B(KeybindId.Dash), Segment.T(" -> Dash") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Combat") },
        new[] { Segment.B(KeybindId.Attack), Segment.T(" -> Attack") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Hotbar") },
        new[] {
            Segment.B(KeybindId.ItemSlot1), Segment.T(" / "),
            Segment.B(KeybindId.ItemSlot2), Segment.T(" -> Select slot"),
        },
        new[] { Segment.B(KeybindId.PlaceItem), Segment.T(" -> Place item") },
        new[] { Segment.B(KeybindId.ThrowItem), Segment.T(" -> Throw item") },
    };

    private static readonly Segment[][] ItemsSegments =
    {
        new[] { Segment.T("Reference") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.Bomb), Segment.T(": A bomb that damages nearby enemies") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.Decoy), Segment.T(": A decoy to distract enemies") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.HealingPotion), Segment.T($": Heals {HealingPotion.DefaultHealAmount} HP") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.OneUp), Segment.T(": Grants an extra revive upon death") },
    };

    private static SpriteFont _menuPixelFont;
    private static bool _menuPixelFontResolved;

    protected GameManager gm => GameManager.GetGameManager();

    protected SpriteFont _font => gm._font;
    protected InputManager InputManager => gm.InputManager;
    protected GameState _state
    {
        get => gm._state;
        set => gm._state = value;
    }
    protected Game Game => gm.Game;
    protected Texture2D Pixel => gm.Pixel;
    protected List<GameObject> _gameObjects => gm._gameObjects;
    protected List<GameObject> _toBeAdded => gm._toBeAdded;
    protected List<GameObject> _toBeRemoved => gm._toBeRemoved;
    protected ContentManager _content => gm._content;

    /// <summary>Scales menu text and hit targets on high-res windows (reference ~900px tall).</summary>
    protected static float MenuUiScale(in Viewport viewport)
    {
        if (viewport.Height <= 0)
            return 1.35f;
        float s = viewport.Height / 900f;
        return MathHelper.Clamp(s, 1.35f, 2.8f);
    }

    private static readonly Color HubBackdropTop = new(14, 18, 34, 255);
    private static readonly Color HubBackdropBottom = new(28, 38, 72, 255);
    private static readonly Color HubBackdropRail = new(255, 255, 255, 18);

    /// <summary>Full-screen gradient wash for title/settings style menus (replaces the gameplay map).</summary>
    protected static void DrawHubMenuBackdrop(SpriteBatch spriteBatch, Texture2D pixel, in Viewport vp)
    {
        const int bands = 16;
        int w = vp.Width;
        int h = vp.Height;
        for (int i = 0; i < bands; i++)
        {
            float t0 = i / (float)bands;
            float t1 = (i + 1f) / bands;
            int y0 = (int)(t0 * h);
            int y1 = (int)(t1 * h);
            int rh = System.Math.Max(1, y1 - y0);
            Color c = Color.Lerp(HubBackdropTop, HubBackdropBottom, (t0 + t1) * 0.5f);
            spriteBatch.Draw(pixel, new Rectangle(0, y0, w, rh), c);
        }
    }

    protected static void DrawHubMenuLeftRail(SpriteBatch spriteBatch, Texture2D pixel, in Viewport vp, float uiScale)
    {
        int railW = (int)(52f * uiScale);
        spriteBatch.Draw(pixel, new Rectangle(0, 0, railW, vp.Height), HubBackdropRail);
    }

    /// <summary>Thin border frame (settings panel, modals, hub boxes).</summary>
    protected static void DrawPanelOutline(SpriteBatch spriteBatch, Texture2D pixel, Rectangle r, Color c, int thickness = 2)
    {
        int t = thickness;
        spriteBatch.Draw(pixel, new Rectangle(r.Left, r.Top, r.Width, t), c);
        spriteBatch.Draw(pixel, new Rectangle(r.Left, r.Bottom - t, r.Width, t), c);
        spriteBatch.Draw(pixel, new Rectangle(r.Left, r.Top, t, r.Height), c);
        spriteBatch.Draw(pixel, new Rectangle(r.Right - t, r.Top, t, r.Height), c);
    }

    protected void DrawPanelOutline(SpriteBatch spriteBatch, Rectangle r, Color c, int thickness = 2) =>
        DrawPanelOutline(spriteBatch, Pixel, r, c, thickness);

    /// <summary>Optional compact font (<c>Fonts/MenuPixel</c>); falls back to the main UI font if missing.</summary>
    protected SpriteFont MenuUiFont
    {
        get
        {
            if (_menuPixelFontResolved)
                return _menuPixelFont ?? _font;
            _menuPixelFontResolved = true;
            try
            {
                _menuPixelFont = _content.Load<SpriteFont>("Fonts/MenuPixel");
            }
            catch (ContentLoadException)
            {
                _menuPixelFont = null;
            }
            return _menuPixelFont ?? _font;
        }
    }

    /// <summary>Fonts/MenuPixel often has zero-width space glyphs; use the main UI font for sentences.</summary>
    protected (SpriteFont body, float scaleMul) BodyFontForMenuText(SpriteFont menuFont)
    {
        SpriteFont body = _font ?? menuFont;
        float mul = ReferenceEquals(body, menuFont) ? 1f : menuFont.LineSpacing / Math.Max(1f, body.LineSpacing);
        return (body, mul);
    }

    protected Rectangle GetTextRectangle(string text, Vector2 position, float scale = 1)
        => GetTextRectangleForFont(_font, text, position, scale);

    protected Rectangle GetTextRectangleForFont(SpriteFont font, string text, Vector2 position, float scale = 1f)
    {
        if (font == null && gm.FontBitmap == null)
            return Rectangle.Empty;
        Vector2 size = gm.MeasureUiString(font, text, scale);
        return new Rectangle(
            (int)position.X - 10,
            (int)position.Y - 5,
            (int)size.X + 20,
            (int)size.Y + 10);
    }

    public Vector2 GetFontPosition(string text)
    {
        Viewport viewport = Game.GraphicsDevice.Viewport;
        Vector2 center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        if (_font == null && gm.FontBitmap == null)
            return center;

        Vector2 textSize = gm.MeasureUiString(_font, text, 1f);
        return new Vector2(center.X - textSize.X / 2f, center.Y - textSize.Y / 2f);
    }

    protected float MeasureControlsContentWidth(SpriteFont textFont, float textScale) =>
        MeasureControlsLinesWidth(textFont, textScale, ControlsLines);

    private float MeasureControlsLinesWidth(SpriteFont textFont, float textScale, Segment[][] lines)
    {
        SpriteFont bodyFont = _font ?? textFont;
        float bodyMul = ReferenceEquals(bodyFont, textFont) ? 1f : textFont.LineSpacing / Math.Max(1f, bodyFont.LineSpacing);
        float bodyTs = textScale * bodyMul;
        float spriteSize = (textFont.LineSpacing * textScale * 1.4f / FrameSize) * FrameSize;
        float maxW = 0f;
        foreach (var line in lines)
        {
            float w = 0f;
            foreach (var seg in line)
            {
                switch (seg.Kind)
                {
                    case SegKind.Text:
                        w += gm.MeasureUiString(bodyFont, seg.Text, bodyTs).X;
                        break;
                    default:
                        w += spriteSize;
                        break;
                }
            }
            if (w > maxW)
                maxW = w;
        }
        return maxW;
    }

    protected float MeasureItemsContentWidth(SpriteFont textFont, float textScale)
    {
        SpriteFont bodyFont = _font ?? textFont;
        float bodyTextScale = ReferenceEquals(bodyFont, textFont)
            ? textScale
            : textScale * (textFont.LineSpacing / Math.Max(1f, bodyFont.LineSpacing));
        float spriteSize = (textFont.LineSpacing * textScale * 1.4f / FrameSize) * FrameSize;
        float maxW = 0f;
        foreach (var line in ItemsSegments)
        {
            float w = 0f;
            foreach (var seg in line)
            {
                if (seg.Kind == SegKind.Text)
                    w += gm.MeasureUiString(bodyFont, seg.Text, bodyTextScale).X;
                else
                    w += spriteSize;
            }
            if (w > maxW)
                maxW = w;
        }
        return maxW;
    }

    /// <param name="basePos">Top-left of the text block inside the panel.</param>
    protected void DrawControlsText(SpriteBatch spriteBatch, GameTime gameTime, Vector2 basePos, float textScale) =>
        DrawControlsLinesCore(spriteBatch, gameTime, basePos, textScale, ControlsLines);

    /// <summary>Draws control hints using <see cref="KeybindStore"/> (saved bindings).</summary>
    protected void DrawSavedControlsText(SpriteBatch spriteBatch, GameTime gameTime, Vector2 basePos, float textScale) =>
        DrawControlsLinesCore(spriteBatch, gameTime, basePos, textScale, BuildSavedControlsLines());

    private void DrawControlsLinesCore(SpriteBatch spriteBatch, GameTime gameTime, Vector2 basePos, float textScale, Segment[][] lines)
    {
        if (_font == null && gm.FontBitmap == null)
            return;

        if (_lmbTexture == null) _lmbTexture = _content.Load<Texture2D>("menu/LeftMouseClick");
        if (_keysTexture == null) _keysTexture = _content.Load<Texture2D>("menu/keys");

        SpriteFont textFont = MenuUiFont;
        var (bodyFont, bodyMul) = BodyFontForMenuText(textFont);
        float bodyTs = textScale * bodyMul;
        float lineHeight = textFont.LineSpacing * textScale;
        float spriteScale = (lineHeight * 1.4f) / FrameSize;
        float spriteSize = FrameSize * spriteScale;
        float spriteYOffset = -(spriteSize - lineHeight) / 2f;
        int frame = (int)(gameTime.TotalGameTime.TotalSeconds / FrameDuration) % FrameCount;

        float maxWidth = MeasureControlsLinesWidth(textFont, textScale, lines);
        float totalHeight = lines.Length * lineHeight;

        Rectangle backgroundRect = new Rectangle(
            (int)basePos.X - 10,
            (int)basePos.Y - 5,
            (int)maxWidth + 20,
            (int)totalHeight + 10);
        spriteBatch.Draw(Pixel, backgroundRect, new Color(18, 24, 38, 255));

        for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
        {
            float x = basePos.X;
            float y = basePos.Y + lineIdx * lineHeight;
            foreach (var seg in lines[lineIdx])
            {
                switch (seg.Kind)
                {
                    case SegKind.Text:
                        gm.DrawUiString(spriteBatch, bodyFont, seg.Text, new Vector2(x, y), Color.White, bodyTs);
                        x += gm.MeasureUiString(bodyFont, seg.Text, bodyTs).X;
                        break;
                    case SegKind.Key:
                    {
                        Rectangle src = new Rectangle(frame * FrameSize, seg.KeyRow * FrameSize, FrameSize, FrameSize);
                        spriteBatch.Draw(_keysTexture, new Vector2(x, y + spriteYOffset), src, Color.White, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0f);
                        x += spriteSize;
                        break;
                    }
                    case SegKind.Lmb:
                    {
                        Rectangle src = new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize);
                        spriteBatch.Draw(_lmbTexture, new Vector2(x, y + spriteYOffset), src, Color.White, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0f);
                        x += spriteSize;
                        break;
                    }
                    case SegKind.BoundKey:
                    {
                        GameInputBinding b = KeybindStore.GetBinding(seg.BindId!.Value);
                        DrawControlsBoundKey(spriteBatch, bodyFont, bodyMul, textScale, frame, x, y, lineHeight, spriteYOffset, spriteScale, spriteSize, b);
                        x += spriteSize;
                        break;
                    }
                }
            }
        }
    }

    private void DrawControlsBoundKey(SpriteBatch spriteBatch, SpriteFont bodyFont, float bodyMul, float textScale, int frame,
        float x, float y, float lineHeight, float spriteYOffset, float spriteScale, float spriteSize, GameInputBinding b)
    {
        switch (b.Kind)
        {
            case BindingKind.Keyboard:
            {
                int? row = KeySpriteRowForBindings(b.Key);
                if (row.HasValue)
                {
                    Rectangle src = new Rectangle(frame * FrameSize, row.Value * FrameSize, FrameSize, FrameSize);
                    spriteBatch.Draw(_keysTexture, new Vector2(x, y + spriteYOffset), src, Color.White, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0f);
                }
                else
                {
                    string s = GameInputBinding.Format(b);
                    float ts = 0.38f * textScale * bodyMul;
                    Vector2 sz = gm.MeasureUiString(bodyFont, s, ts);
                    gm.DrawUiString(spriteBatch, bodyFont, s, new Vector2(x + (spriteSize - sz.X) / 2f, y + (lineHeight - sz.Y) / 2f), Color.White, ts);
                }
                break;
            }
            case BindingKind.Mouse:
            {
                if (b.Mouse == MouseBindButton.Left)
                {
                    Rectangle src = new Rectangle(frame * FrameSize, 0, FrameSize, FrameSize);
                    spriteBatch.Draw(_lmbTexture, new Vector2(x, y + spriteYOffset), src, Color.White, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0f);
                }
                else
                {
                    string s = GameInputBinding.Format(b);
                    float ts = 0.38f * textScale * bodyMul;
                    Vector2 sz = gm.MeasureUiString(bodyFont, s, ts);
                    gm.DrawUiString(spriteBatch, bodyFont, s, new Vector2(x + (spriteSize - sz.X) / 2f, y + (lineHeight - sz.Y) / 2f), Color.White, ts);
                }
                break;
            }
            default:
            {
                string s = "(none)";
                float ts = 0.38f * textScale * bodyMul;
                Vector2 sz = gm.MeasureUiString(bodyFont, s, ts);
                gm.DrawUiString(spriteBatch, bodyFont, s, new Vector2(x + (spriteSize - sz.X) / 2f, y + (lineHeight - sz.Y) / 2f), Color.Gray, ts);
                break;
            }
        }
    }

    private static int? KeySpriteRowForBindings(Keys k) => k switch
    {
        Keys.W => 0,
        Keys.A => 1,
        Keys.S => 2,
        Keys.D => 3,
        Keys.T => 4,
        Keys.D1 or Keys.NumPad1 => 5,
        Keys.D2 or Keys.NumPad2 => 6,
        Keys.LeftShift or Keys.RightShift => 7,
        _ => null,
    };

    /// <param name="wrapInnerWidth">Max width for wrapping item descriptions (excluding panel padding).</param>
    protected void DrawItemsText(SpriteBatch spriteBatch, GameTime gameTime, Vector2 basePos, float wrapInnerWidth, float textScale)
    {
        if (_font == null && gm.FontBitmap == null)
            return;

        SpriteFont textFont = MenuUiFont;
        SpriteFont bodyFont = _font ?? textFont;
        float bodyTextScale = ReferenceEquals(bodyFont, textFont)
            ? textScale
            : textScale * (textFont.LineSpacing / Math.Max(1f, bodyFont.LineSpacing));
        wrapInnerWidth = MathHelper.Max(wrapInnerWidth, 220f);
        float lineHeight = textFont.LineSpacing * textScale;
        float spriteScale = (lineHeight * 1.4f) / FrameSize;
        float spriteSize = FrameSize * spriteScale;

        float contentWidth = Math.Max(MeasureItemsContentWidth(textFont, textScale), wrapInnerWidth);
        float panelInnerWidth = Math.Max(wrapInnerWidth, contentWidth);
        float totalHeight = ItemsSegments.Length * lineHeight * 2f;

        Rectangle backgroundRect = new Rectangle(
            (int)basePos.X - 10,
            (int)basePos.Y - 5,
            (int)panelInnerWidth + 20,
            (int)totalHeight + 10);
        spriteBatch.Draw(Pixel, backgroundRect, new Color(18, 24, 38, 255));

        Texture2D itemSheet = null;
        Texture2D heartSheet = null;
        try { itemSheet = _content.Load<Texture2D>("itemSpriteSheet"); }
        catch (ContentLoadException) { }
        try { heartSheet = _content.Load<Texture2D>("Heart"); }
        catch (ContentLoadException) { }

        float y = basePos.Y;
        for (int lineIdx = 0; lineIdx < ItemsSegments.Length; lineIdx++)
        {
            if (ItemsSegments[lineIdx].Length == 0)
            {
                y += lineHeight;
                continue;
            }

            float x = basePos.X;
            float currentIndent = 0f;
            float extraLineHeight = 0f;
            float lineWrapIndent = 0f;

            foreach (var seg in ItemsSegments[lineIdx])
            {
                if (seg.Kind == SegKind.ItemSprite)
                {
                    lineWrapIndent = spriteSize;
                    float yOffset = -(spriteSize - lineHeight) / 2f;
                    if (seg.Item == ItemType.OneUp)
                    {
                        if (heartSheet != null && heartSheet.Height > 0)
                        {
                            Rectangle src = new Rectangle(0, 0, heartSheet.Width, heartSheet.Height);
                            float scale = spriteSize / heartSheet.Height;
                            spriteBatch.Draw(heartSheet, new Vector2(x, y + yOffset + 4f), src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (itemSheet != null)
                    {
                        Rectangle src = seg.Item == ItemType.Bomb ? new Rectangle(0, 0, 32, 32) :
                            seg.Item == ItemType.Decoy ? new Rectangle(0, 32, 32, 32) :
                            new Rectangle(0, 64, 32, 32);
                        spriteBatch.Draw(itemSheet, new Vector2(x, y + yOffset), src, Color.White, 0f, Vector2.Zero, spriteSize / 32f, SpriteEffects.None, 0f);
                    }
                    x += spriteSize;
                    currentIndent += spriteSize;
                }
                else if (seg.Kind == SegKind.Text)
                {
                    string text = seg.Text;
                    string[] words = text.Split(' ');
                    string currentLine = "";

                    foreach (string word in words)
                    {
                        string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                        float testWidth = gm.MeasureUiString(bodyFont, testLine, bodyTextScale).X;

                        if (currentIndent + testWidth > wrapInnerWidth && !string.IsNullOrEmpty(currentLine))
                        {
                            gm.DrawUiString(spriteBatch, bodyFont, currentLine, new Vector2(x, y + extraLineHeight), Color.White, bodyTextScale);
                            extraLineHeight += lineHeight;
                            x = basePos.X + lineWrapIndent;
                            currentIndent = lineWrapIndent;
                            currentLine = word;
                        }
                        else
                        {
                            currentLine = testLine;
                        }
                    }

                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        gm.DrawUiString(spriteBatch, bodyFont, currentLine, new Vector2(x, y + extraLineHeight), Color.White, bodyTextScale);
                        x += gm.MeasureUiString(bodyFont, currentLine, bodyTextScale).X;
                        currentIndent += gm.MeasureUiString(bodyFont, currentLine, bodyTextScale).X;
                    }
                }
            }
            y += lineHeight + extraLineHeight;
        }
    }

    /// <summary>Top-left Back control + shortcut hint (shared by hub-style menus).</summary>
    protected readonly struct MenuHubBackChrome
    {
        public readonly Rectangle BackHitRect;
        public readonly Vector2 BackTextPos;
        public readonly Vector2 HintTextPos;
        public readonly float BackTextScale;
        public readonly float HintTextScale;
        public readonly SpriteFont LabelFont;

        public MenuHubBackChrome(Rectangle backHitRect, Vector2 backTextPos, Vector2 hintTextPos, float backTextScale, float hintTextScale, SpriteFont labelFont)
        {
            BackHitRect = backHitRect;
            BackTextPos = backTextPos;
            HintTextPos = hintTextPos;
            BackTextScale = backTextScale;
            HintTextScale = hintTextScale;
            LabelFont = labelFont;
        }
    }

    /// <param name="anchorPanel">Top-left anchor (typically the framed menu panel, or full viewport).</param>
    protected MenuHubBackChrome LayoutMenuHubBackChrome(Rectangle anchorPanel, float uiScale, SpriteFont layoutFont)
    {
        var (bf, mul) = BodyFontForMenuText(layoutFont);
        float backS = 0.58f * uiScale * mul;
        const string backLabel = "Back";
        Vector2 pos = new Vector2(anchorPanel.X + 18f * uiScale, anchorPanel.Y + 14f * uiScale);
        Vector2 sz = gm.MeasureUiString(bf, backLabel, backS);
        int padX = (int)System.Math.Ceiling(10f * uiScale);
        int padY = (int)System.Math.Ceiling(6f * uiScale);
        var hit = new Rectangle((int)pos.X - padX, (int)pos.Y - padY, (int)sz.X + padX * 2, (int)sz.Y + padY * 2);
        float hintS = 0.4f * uiScale * mul;
        var hintPos = new Vector2(pos.X, pos.Y + sz.Y + 5f * uiScale);

        return new MenuHubBackChrome(hit, pos, hintPos, backS, hintS, bf);
    }

    protected void DrawMenuHubBackChrome(SpriteBatch spriteBatch, in MenuHubBackChrome chrome, bool backHovered, float uiScale, string? extraHintLine = null)
    {
        var (bf, mul) = BodyFontForMenuText(chrome.LabelFont);
        Color fill = backHovered ? new Color(50, 62, 86, 255) : new Color(32, 40, 56, 255);
        spriteBatch.Draw(Pixel, chrome.BackHitRect, fill);
        DrawPanelOutline(spriteBatch, chrome.BackHitRect, new Color(140, 185, 255, backHovered ? 200 : 130));
        gm.DrawUiString(spriteBatch, bf, "Back", chrome.BackTextPos, Color.White, 0f, Vector2.Zero, chrome.BackTextScale, SpriteEffects.None, 0f);
        gm.DrawUiString(spriteBatch, bf, "Esc / Q", chrome.HintTextPos, new Color(175, 182, 198), 0f, Vector2.Zero, chrome.HintTextScale, SpriteEffects.None, 0f);
        if (!string.IsNullOrEmpty(extraHintLine))
        {
            float extraS = 0.36f * uiScale * mul;
            float y = chrome.HintTextPos.Y + gm.MeasureUiString(bf, "Esc / Q", chrome.HintTextScale).Y + 3f * uiScale;
            gm.DrawUiString(spriteBatch, bf, extraHintLine, new Vector2(chrome.HintTextPos.X, y), new Color(130, 138, 155), 0f, Vector2.Zero, extraS, SpriteEffects.None, 0f);
        }
    }

    protected void DrawWorld(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(gameTime, spriteBatch);
        }
        spriteBatch.End();
    }
}
