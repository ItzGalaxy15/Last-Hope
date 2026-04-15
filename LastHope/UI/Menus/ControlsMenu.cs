using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.UI.Menus;

public class ControlsMenu : MenuBase
{
    private readonly Menu _owner;

    public ControlsMenu(Menu owner)
    {
        _owner = owner;
    }

    public void Update(GameTime gameTime)
    {
        string continueText = "Continue";
        Vector2 continuePos = GetFontPosition(continueText);
        Rectangle continueRect = GetTextRectangle(continueText, continuePos);

        if (continueRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            _state = _owner.PreviousState;
        }
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string continueText = "Continue";
        Vector2 continuePos = GetFontPosition(continueText);
        Rectangle continueRect = GetTextRectangle(continueText, continuePos);

        if (transformMatrix != null)
        {
            DrawWorld(gameTime, spriteBatch, transformMatrix);
        }

        spriteBatch.Begin();
        DrawControlsText(spriteBatch);

        spriteBatch.Draw(Pixel, continueRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, continueText, continuePos, Color.White);
        spriteBatch.End();
    }
}
