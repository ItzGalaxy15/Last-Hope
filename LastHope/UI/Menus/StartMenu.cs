using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.UI.Menus;

public class StartMenu : MenuBase
{
    /// <summary>Vertical distance between each menu line center; large enough that padded hit boxes do not overlap.</summary>
    private const float RowSpacing = 118f;

    public void Update(GameTime gameTime)
    {
        if (_font == null)
            return;

        if (!InputManager.LeftMousePress())
            return;

        Point mouse = InputManager.CurrentMouseState.Position;
        var layout = GetLayout();

        // One action per click — overlapping padded rects must not run Start then Quit in the same frame.
        if (layout.StartRect.Contains(mouse))
            _state = GameState.CharacterSelect;
        else if (layout.RosterRect.Contains(mouse))
            _state = GameState.Characters;
        else if (layout.QuitRect.Contains(mouse))
            Game.Exit();
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        if (_font == null)
            return;

        var layout = GetLayout();

        spriteBatch.Begin();
        spriteBatch.Draw(Pixel, layout.StartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, layout.RosterRect, new Color(35, 50, 65));
        spriteBatch.Draw(Pixel, layout.QuitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, layout.StartText, layout.StartPos, Color.White);
        spriteBatch.DrawString(_font, layout.RosterText, layout.RosterPos, new Color(200, 220, 255));
        spriteBatch.DrawString(_font, layout.QuitText, layout.QuitPos, Color.Red);
        spriteBatch.End();
    }

    private Layout GetLayout()
    {
        Viewport vp = Game.GraphicsDevice.Viewport;
        Vector2 center = new Vector2(vp.Width / 2f, vp.Height / 2f);

        const string startText = "Start Game";
        const string rosterText = "Characters";
        const string quitText = "Quit Game";

        Vector2 startPos = new Vector2(center.X - _font.MeasureString(startText).X / 2f, center.Y - RowSpacing);
        Vector2 rosterPos = new Vector2(center.X - _font.MeasureString(rosterText).X / 2f, center.Y);
        Vector2 quitPos = new Vector2(center.X - _font.MeasureString(quitText).X / 2f, center.Y + RowSpacing);

        return new Layout(
            startText,
            rosterText,
            quitText,
            startPos,
            rosterPos,
            quitPos,
            GetTextRectangle(startText, startPos),
            GetTextRectangle(rosterText, rosterPos),
            GetTextRectangle(quitText, quitPos));
    }

    private readonly struct Layout(
        string StartText,
        string RosterText,
        string QuitText,
        Vector2 StartPos,
        Vector2 RosterPos,
        Vector2 QuitPos,
        Rectangle StartRect,
        Rectangle RosterRect,
        Rectangle QuitRect)
    {
        internal string StartText { get; } = StartText;
        internal string RosterText { get; } = RosterText;
        internal string QuitText { get; } = QuitText;
        internal Vector2 StartPos { get; } = StartPos;
        internal Vector2 RosterPos { get; } = RosterPos;
        internal Vector2 QuitPos { get; } = QuitPos;
        internal Rectangle StartRect { get; } = StartRect;
        internal Rectangle RosterRect { get; } = RosterRect;
        internal Rectangle QuitRect { get; } = QuitRect;
    }
}