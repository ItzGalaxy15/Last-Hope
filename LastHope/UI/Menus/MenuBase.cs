using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.Classes.Items;

namespace Last_Hope.UI.Menus;

/// <summary>
/// Shared helpers for full-screen and hub menus: fonts, keybind/control reference drawing, item index text,
/// hub backdrop, and end-game overlays. Concrete screens inherit this and implement their own
/// <c>Update</c>/<c>Draw</c>; routing is via <see cref="Last_Hope.UI.Menu"/> and <see cref="GameManager"/>.
/// </summary>
public abstract class MenuBase
{
    private const int FrameSize = 32;
    private const int FrameCount = 3;
    private const double FrameDuration = 0.15;
    protected static Texture2D _lmbTexture;
    protected static Texture2D _lettersTexture;
    protected static Texture2D _numbersTexture;
    protected static Texture2D _specialTexture;

    protected enum KeySheet { Letters, Numbers, Special }

    /// <summary>Labels for the shared restart/quit pair on <see cref="GameOverMenu"/> and <see cref="WinnerMenu"/>.</summary>
    protected static class EndGameMenuLabels
    {
        public const string Restart = "Restart Game";
        public const string Quit = "Quit Game";
    }

    /// <summary>Hit boxes and text positions for <see cref="LayoutEndGameTwoButtonMenu"/>.</summary>
    protected readonly record struct EndGameMenuLayout(
        Vector2 TitlePosition,
        Vector2 RestartTextPosition,
        Vector2 QuitTextPosition,
        Rectangle RestartHitBox,
        Rectangle QuitHitBox);

    /// <summary>
    /// Computes centered title position plus stacked restart/quit rows (used by <see cref="GameOverMenu"/> and <see cref="WinnerMenu"/>).
    /// </summary>
    protected EndGameMenuLayout LayoutEndGameTwoButtonMenu(string titleText, float restartOffsetY = 100f, float quitOffsetY = 200f)
    {
        Vector2 titlePos = GetFontPosition(titleText);
        Vector2 restartPos = GetFontPosition(EndGameMenuLabels.Restart) + new Vector2(0, restartOffsetY);
        Vector2 quitPos = GetFontPosition(EndGameMenuLabels.Quit) + new Vector2(0, quitOffsetY);
        return new EndGameMenuLayout(
            titlePos,
            restartPos,
            quitPos,
            GetTextRectangle(EndGameMenuLabels.Restart, restartPos),
            GetTextRectangle(EndGameMenuLabels.Quit, quitPos));
    }

    /// <summary>
    /// Mouse handling for end-game buttons. Invoked from <see cref="GameOverMenu.Update"/> / <see cref="WinnerMenu.Update"/>.
    /// </summary>
    protected void HandleEndGameMenuClicks(in EndGameMenuLayout layout, Action onRestart, Action onQuit)
    {
        if (!InputManager.LeftMousePress())
            return;
        Point m = InputManager.CurrentMouseState.Position;
        if (layout.RestartHitBox.Contains(m))
            onRestart();
        else if (layout.QuitHitBox.Contains(m))
            onQuit();
    }

    /// <summary>
    /// Draws title + dim button rects + restart/quit labels. Used by <see cref="GameOverMenu.Draw"/> and <see cref="WinnerMenu.Draw"/>.
    /// </summary>
    protected void DrawEndGameTwoButtonOverlay(SpriteBatch spriteBatch, string titleText, Color titleColor, Color quitLabelColor, in EndGameMenuLayout layout)
    {
        gm.DrawUiString(spriteBatch, _font, titleText, layout.TitlePosition, titleColor);
        spriteBatch.Draw(Pixel, layout.RestartHitBox, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, layout.QuitHitBox, Color.DarkSlateGray);
        gm.DrawUiString(spriteBatch, _font, EndGameMenuLabels.Restart, layout.RestartTextPosition, Color.White);
        gm.DrawUiString(spriteBatch, _font, EndGameMenuLabels.Quit, layout.QuitTextPosition, quitLabelColor);
    }

    private enum SegKind { Text, ItemSprite, BoundKey }

    /// <summary>One token in a controls or items reference line (see <see cref="DrawSavedControlsText"/>, <see cref="DrawItemsText"/>).</summary>
    private readonly struct Segment
    {
        public readonly SegKind Kind;
        public readonly string Text;
        public readonly ItemType Item;
        public readonly KeybindId? BindId;
        private Segment(SegKind k, string t, ItemType i, KeybindId? bindId)
        {
            Kind = k;
            Text = t;
            Item = i;
            BindId = bindId;
        }
        public static Segment T(string t) => new Segment(SegKind.Text, t, ItemType.None, null);
        public static Segment I(ItemType item) => new Segment(SegKind.ItemSprite, null, item, null);
        public static Segment B(KeybindId id) => new Segment(SegKind.BoundKey, null, ItemType.None, id);
    }

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
        new[] { Segment.B(KeybindId.KeyboardAttack), Segment.T(" -> Attack (KB)") },
        Array.Empty<Segment>(),
        new[] { Segment.T("Aim") },
        new[] {
            Segment.B(KeybindId.AimUp), Segment.T(" "),
            Segment.B(KeybindId.AimDown), Segment.T(" "),
            Segment.B(KeybindId.AimLeft), Segment.T(" "),
            Segment.B(KeybindId.AimRight), Segment.T(" -> Aim"),
        },
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

    /// <summary>Full-screen gradient wash for title/settings style menus (replaces the gameplay map). Used by <see cref="MainMenuScreen"/>, <see cref="SettingsMenu"/>, <see cref="ItemsIndexMenu"/>.</summary>
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

    /// <summary>
    /// Gets a Rectangle representing the bounding box for the given text.
    /// </summary>
    /// <param name="text">The text to measure.</param>
    /// <param name="position">The top-left position of the text.</param>
    /// <param name="scale">The scale applied to the text.</param>
    /// <returns>A Rectangle encompassing the text area.</returns>
    protected Rectangle GetTextRectangle(string text, Vector2 position, float scale = 1)
        => GetTextRectangleForFont(_font, text, position, scale);

    /// <summary>
    /// Gets a Rectangle representing the bounding box for the given text, using a specific font.
    /// </summary>
    /// <param name="font">The SpriteFont to use for measurement.</param>
    /// <param name="text">The text to measure.</param>
    /// <param name="position">The top-left position of the text.</param>
    /// <param name="scale">The scale applied to the text.</param>
    /// <returns>A Rectangle encompassing the text area.</returns>
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

    /// <summary>
    /// Calculates the centered position for a given text string on the screen.
    /// </summary>
    /// <param name="text">The text to measure and center.</param>
    /// <returns>A Vector2 representing the centered position.</returns>
    protected Vector2 GetFontPosition(string text)
    {
        Viewport viewport = Game.GraphicsDevice.Viewport;
        Vector2 center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        if (_font == null && gm.FontBitmap == null)
            return center;

        Vector2 textSize = gm.MeasureUiString(_font, text, 1f);
        return new Vector2(center.X - textSize.X / 2f, center.Y - textSize.Y / 2f);
    }

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

    /// <summary>
    /// Draws control hints using <see cref="KeybindStore"/> (saved bindings). Used by <see cref="PausedMenu.Draw"/>.
    /// </summary>
    /// <param name="basePos">Top-left of the text block inside the panel.</param>
    protected void DrawSavedControlsText(SpriteBatch spriteBatch, GameTime gameTime, Vector2 basePos, float textScale) =>
        DrawControlsLinesCore(spriteBatch, gameTime, basePos, textScale, BuildSavedControlsLines());

    private void DrawControlsLinesCore(SpriteBatch spriteBatch, GameTime gameTime, Vector2 basePos, float textScale, Segment[][] lines)
    {
        if (_font == null && gm.FontBitmap == null)
            return;

        if (_lmbTexture == null) _lmbTexture = _content.Load<Texture2D>("menu/LeftMouseClick");
        if (_lettersTexture == null) _lettersTexture = _content.Load<Texture2D>("menu/letters");
        if (_numbersTexture == null) _numbersTexture = _content.Load<Texture2D>("menu/numbers");
        if (_specialTexture == null) _specialTexture = _content.Load<Texture2D>("menu/special");

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
                var info = KeySpriteInfoForBindings(b.Key);
                if (info.HasValue)
                {
                    Texture2D tex = info.Value.sheet switch
                    {
                        KeySheet.Letters => _lettersTexture,
                        KeySheet.Numbers => _numbersTexture,
                        _ => _specialTexture,
                    };
                    Rectangle src = new Rectangle(frame * FrameSize, info.Value.row * FrameSize, FrameSize, FrameSize);
                    spriteBatch.Draw(tex, new Vector2(x, y + spriteYOffset), src, Color.White, 0f, Vector2.Zero, spriteScale, SpriteEffects.None, 0f);
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

    protected static (KeySheet sheet, int row)? KeySpriteInfoForBindings(Keys k)
    {
        if (k >= Keys.A && k <= Keys.Z)
            return (KeySheet.Letters, k - Keys.A);

        if (k >= Keys.D1 && k <= Keys.D9)
            return (KeySheet.Numbers, k - Keys.D1);
        if (k == Keys.D0)
            return (KeySheet.Numbers, 9);
        if (k >= Keys.NumPad1 && k <= Keys.NumPad9)
            return (KeySheet.Numbers, k - Keys.NumPad1);
        if (k == Keys.NumPad0)
            return (KeySheet.Numbers, 9);

        return k switch
        {
            Keys.Up                             => (KeySheet.Special, 0),
            Keys.Down                           => (KeySheet.Special, 1),
            Keys.Left                           => (KeySheet.Special, 2),
            Keys.Right                          => (KeySheet.Special, 3),
            Keys.OemPeriod                      => (KeySheet.Special, 4),
            Keys.OemComma                       => (KeySheet.Special, 5),
            Keys.LeftShift or Keys.RightShift   => (KeySheet.Special, 6),
            Keys.Space                          => (KeySheet.Special, 7),
            Keys.LeftAlt or Keys.RightAlt       => (KeySheet.Special, 8),
            Keys.Tab                            => (KeySheet.Special, 9),
            Keys.Enter                          => (KeySheet.Special, 10),
            Keys.Back                           => (KeySheet.Special, 11),
            _                                   => null,
        };
    }

    /// <summary>
    /// Item reference list (icons + descriptions). Used by <see cref="PausedMenu.Draw"/> and <see cref="ItemsIndexMenu.Draw"/>.
    /// </summary>
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

    /// <summary>
    /// Draws all <see cref="GameObject"/> instances in play. Used by gameplay and menus that show the frozen world
    /// (<see cref="PausedMenu"/>, <see cref="GameOverMenu"/>, <see cref="WinnerMenu"/>, <see cref="RunningMenu"/>, <see cref="ItemsIndexMenu"/>).
    /// </summary>
    /// <param name="effect">Optional post-process (e.g. <see cref="GameManager.DeathFade"/> on game over).</param>
    protected void DrawWorld(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix, Effect effect = null)
    {
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: effect, transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            if (gameObject.IsYSorted) continue;
            gameObject.Draw(gameTime, spriteBatch);
        }
        spriteBatch.End();
    }
}
