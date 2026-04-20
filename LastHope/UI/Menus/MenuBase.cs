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

    // Rows in keys.png: 0=W, 1=A, 2=S, 3=D, 4=T, 5=1, 6=2
    private const int KeyRowW = 0;
    private const int KeyRowA = 1;
    private const int KeyRowS = 2;
    private const int KeyRowD = 3;
    private const int KeyRowT = 4;
    private const int KeyRow1 = 5;
    private const int KeyRow2 = 6;

    private const int KeyRowShift = 7;

    private enum SegKind { Text, Key, Lmb, ItemSprite }
    private readonly struct Segment
    {
        public readonly SegKind Kind;
        public readonly string Text;
        public readonly int KeyRow;
        public readonly ItemType Item;
        private Segment(SegKind k, string t, int r, ItemType i = ItemType.None) { Kind = k; Text = t; KeyRow = r; Item = i; }
        public static Segment T(string t) => new Segment(SegKind.Text, t, 0);
        public static Segment K(int row) => new Segment(SegKind.Key, null, row);
        public static Segment L() => new Segment(SegKind.Lmb, null, 0);
        public static Segment I(ItemType item) => new Segment(SegKind.ItemSprite, null, 0, item);
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
        new[] { Segment.T("Items") },
        new[] {
            Segment.K(KeyRow1), Segment.T(" / "),
            Segment.K(KeyRow2), Segment.T(" -> Select Item"),
        },
        new[] { Segment.K(KeyRowT), Segment.T(" -> Use Item") },
    };

    private static readonly Segment[][] ItemsSegments =
    {
        new[] { Segment.T("Items") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.Bomb), Segment.T(": A bomb that damages nearby enemies") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.Decoy), Segment.T(": A decoy to distract enemies") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.HealingPotion), Segment.T($": Heals {HealingPotion.DefaultHealAmount} HP") },
        Array.Empty<Segment>(),
        new[] { Segment.I(ItemType.OneUp), Segment.T(": Grants an extra revive upon death") },
    };

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

    protected Rectangle GetTextRectangle(string text, Vector2 position, float scale = 1)
    {
        Vector2 size = _font.MeasureString(text) * scale;
        // -10, -5, +20, +10 to give some padding around the text for easier clicking
        return new Rectangle(
            (int)position.X - 10,
            (int)position.Y - 5,
            (int)size.X + 20,
            (int)size.Y + 10
        );
    }

    public Vector2 GetFontPosition(string text)
    {
        Viewport viewport = Game.GraphicsDevice.Viewport;
        Vector2 center = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        if (_font == null)
        {
            // Safe fallback until content is loaded.
            return center;
        }

        Vector2 textSize = _font.MeasureString(text);
        Vector2 position = new Vector2(center.X - textSize.X / 2f, center.Y - textSize.Y / 2f);
        return position;
    }

    protected void DrawControlsText(SpriteBatch spriteBatch, GameTime gameTime)
    {
        if (_lmbTexture == null) _lmbTexture = _content.Load<Texture2D>("menu/LeftMouseClick");
        if (_keysTexture == null) _keysTexture = _content.Load<Texture2D>("menu/keys");

        float textScale = 0.5f;
        float lineHeight = _font.LineSpacing * textScale;
        float spriteScale = (lineHeight * 1.4f) / FrameSize;
        float spriteSize = FrameSize * spriteScale;
        float spriteYOffset = -(spriteSize - lineHeight) / 2f;
        int frame = (int)(gameTime.TotalGameTime.TotalSeconds / FrameDuration) % FrameCount;

        Vector2 basePos = new Vector2(50, 250);

        float maxWidth = 0f;
        foreach (var line in ControlsLines)
        {
            float w = 0f;
            foreach (var seg in line)
            {
                if (seg.Kind == SegKind.Text) w += _font.MeasureString(seg.Text).X * textScale;
                else w += spriteSize;
            }
            if (w > maxWidth) maxWidth = w;
        }
        
        float totalHeight = Math.Max(ControlsLines.Length, 15) * lineHeight;

        Rectangle backgroundRect = new Rectangle(
            (int)basePos.X - 10,
            (int)basePos.Y - 5,
            (int)maxWidth + 20,
            (int)totalHeight + 10);
        spriteBatch.Draw(Pixel, backgroundRect, Color.Black * 0.60f);

        for (int lineIdx = 0; lineIdx < ControlsLines.Length; lineIdx++)
        {
            float x = basePos.X;
            float y = basePos.Y + lineIdx * lineHeight;
            foreach (var seg in ControlsLines[lineIdx])
            {
                switch (seg.Kind)
                {
                    case SegKind.Text:
                        spriteBatch.DrawString(_font, seg.Text, new Vector2(x, y), Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                        x += _font.MeasureString(seg.Text).X * textScale;
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
                }
            }
        }
    }

    protected void DrawItemsText(SpriteBatch spriteBatch, GameTime gameTime)
    {
        float textScale = 0.5f;
        float lineHeight = _font.LineSpacing * textScale;
        float spriteScale = (lineHeight * 1.4f) / FrameSize;
        float spriteSize = FrameSize * spriteScale;

        float maxWidth = 0f;
        foreach (var line in ControlsLines)
        {
            float w = 0f;
            foreach (var seg in line)
            {
                if (seg.Kind == SegKind.Text) w += _font.MeasureString(seg.Text).X * textScale;
                else w += spriteSize;
            }
            if (w > maxWidth) maxWidth = w;
        }

        float totalHeight = Math.Max(ControlsLines.Length, 15) * lineHeight;

        Viewport viewport = Game.GraphicsDevice.Viewport;
        Vector2 basePos = new Vector2(viewport.Width - maxWidth - 50, 250);

        Rectangle backgroundRect = new Rectangle(
            (int)basePos.X - 10,
            (int)basePos.Y - 5,
            (int)maxWidth + 20,
            (int)totalHeight + 10);
        spriteBatch.Draw(Pixel, backgroundRect, Color.Black * 0.60f);

        Texture2D itemSheet = _content.Load<Texture2D>("itemSpriteSheet");
        Texture2D heartSheet = _content.Load<Texture2D>("Heart");

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
            float extraLineHeight = 0f; // to accumulate multiline text height to add at the end of the segment loop
            float lineWrapIndent = 0f;

            foreach (var seg in ItemsSegments[lineIdx])
            {
                if (seg.Kind == SegKind.ItemSprite)
                {
                    lineWrapIndent = spriteSize;
                    float yOffset = -(spriteSize - lineHeight) / 2f;
                    if (seg.Item == ItemType.OneUp)
                    {
                        Rectangle src = new Rectangle(0, 0, heartSheet.Width, heartSheet.Height);
                        float scale = spriteSize / heartSheet.Height;
                        spriteBatch.Draw(heartSheet, new Vector2(x, y + yOffset + 4f), src, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    }
                    else
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
                    // Pre-calculate spaces or split lines based on available width
                    string[] words = text.Split(' ');
                    string currentLine = "";

                    foreach (string word in words)
                    {
                        string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                        float testWidth = _font.MeasureString(testLine).X * textScale;

                        if (currentIndent + testWidth > maxWidth && !string.IsNullOrEmpty(currentLine))
                        {
                            // Draw what we have so far
                            spriteBatch.DrawString(_font, currentLine, new Vector2(x, y + extraLineHeight), Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                            extraLineHeight += lineHeight;
                            // reset x to keep text indented neatly aligned with the colon!
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
                        spriteBatch.DrawString(_font, currentLine, new Vector2(x, y + extraLineHeight), Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);
                        x += _font.MeasureString(currentLine).X * textScale;
                        currentIndent += _font.MeasureString(currentLine).X * textScale;
                    }
                }
            }
            y += lineHeight + extraLineHeight;
        }
    }

    protected void DrawWorld(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix)
    {
        spriteBatch.Begin(transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(gameTime, spriteBatch);
        }
        spriteBatch.End();
    }
}
