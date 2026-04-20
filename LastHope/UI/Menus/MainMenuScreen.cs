using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

/// <summary>Title hub: world backdrop + top-left entry list.</summary>
public sealed class MainMenuScreen : MenuBase
{
    private static readonly string[] Entries =
    {
        "Start Game",
        "Characters",
        "Items Index",
        "Settings",
        "Quit Game",
    };

    private int _selectedIndex;

    public void Update(GameTime gameTime)
    {
        SpriteFont font = MenuUiFont;
        if (font == null)
            return;

        var layout = BuildLayout(font);

        if (InputManager.IsKeyPress(Keys.Down) || InputManager.IsKeyPress(Keys.S))
        {
            _selectedIndex = (_selectedIndex + 1) % Entries.Length;
        }
        if (InputManager.IsKeyPress(Keys.Up) || InputManager.IsKeyPress(Keys.W))
        {
            _selectedIndex = (_selectedIndex - 1 + Entries.Length) % Entries.Length;
        }

        Point mouse = InputManager.CurrentMouseState.Position;
        for (int i = 0; i < layout.RowRects.Length; i++)
        {
            if (layout.RowRects[i].Contains(mouse))
            {
                _selectedIndex = i;
                break;
            }
        }

        if (InputManager.IsKeyPress(Keys.Enter) || InputManager.IsKeyPress(Keys.Space))
            ApplySelection(_selectedIndex);

        if (InputManager.LeftMousePress())
        {
            for (int i = 0; i < layout.RowRects.Length; i++)
            {
                if (layout.RowRects[i].Contains(mouse))
                {
                    ApplySelection(i);
                    break;
                }
            }
        }
    }

    private void ApplySelection(int index)
    {
        switch (index)
        {
            case 0:
                _state = GameState.CharacterSelect;
                break;
            case 1:
                _state = GameState.Characters;
                break;
            case 2:
                _state = GameState.ItemsIndex;
                break;
            case 3:
                _state = GameState.SettingsMenu;
                break;
            case 4:
                Game.Exit();
                break;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        SpriteFont font = MenuUiFont;
        if (font == null)
            return;

        if (transformMatrix != null)
            DrawWorld(gameTime, spriteBatch, transformMatrix);

        var layout = BuildLayout(font);

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        for (int i = 0; i < Entries.Length; i++)
        {
            Color bg = i == _selectedIndex ? new Color(55, 75, 95) : new Color(32, 36, 42);
            Color fg = i == _selectedIndex ? Color.White : new Color(220, 225, 235);
            spriteBatch.Draw(Pixel, layout.RowRects[i], bg);
            spriteBatch.DrawString(font, Entries[i], layout.RowPositions[i], fg, 0f, Vector2.Zero, layout.TextScale, SpriteEffects.None, 0f);
        }

        const string hint = "Up/Down or W/S  |  Enter / Click";
        float hs = 0.52f * MenuUiScale(Game.GraphicsDevice.Viewport);
        spriteBatch.DrawString(font, hint, layout.HintPos, Color.Gray * 0.9f, 0f, Vector2.Zero, hs, SpriteEffects.None, 0f);
        spriteBatch.End();
    }

    private readonly struct Layout
    {
        public readonly Vector2[] RowPositions;
        public readonly Rectangle[] RowRects;
        public readonly Vector2 HintPos;
        public readonly float TextScale;

        public Layout(Vector2[] rowPositions, Rectangle[] rowRects, Vector2 hintPos, float textScale)
        {
            RowPositions = rowPositions;
            RowRects = rowRects;
            HintPos = hintPos;
            TextScale = textScale;
        }

    }

    private Layout BuildLayout(SpriteFont font)
    {
        Viewport vp = Game.GraphicsDevice.Viewport;
        float ui = MenuUiScale(vp);
        float marginX = 40f * ui;
        float startY = 56f * ui;
        float rowPadY = 14f * ui;
        float rowPadX = 18f * ui;
        float textScale = 1.05f * ui;

        var positions = new Vector2[Entries.Length];
        var rects = new Rectangle[Entries.Length];
        float y = startY;
        float maxInnerW = 0f;
        for (int i = 0; i < Entries.Length; i++)
        {
            Vector2 sz = font.MeasureString(Entries[i]) * textScale;
            if (sz.X > maxInnerW)
                maxInnerW = sz.X;
        }

        float rowWidth = maxInnerW + rowPadX * 2f;
        for (int i = 0; i < Entries.Length; i++)
        {
            Vector2 sz = font.MeasureString(Entries[i]) * textScale;
            float rowH = sz.Y + rowPadY * 2f;
            var rect = new Rectangle((int)marginX, (int)y, (int)rowWidth, (int)rowH);
            rects[i] = rect;
            positions[i] = new Vector2(marginX + rowPadX, y + rowPadY);
            y += rowH + 10f * ui;
        }

        Vector2 hintPos = new Vector2(marginX, y + 16f * ui);
        return new Layout(positions, rects, hintPos, textScale);
    }
}
