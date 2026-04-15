using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.BaseModel;

namespace Last_Hope.UI.Menus;

public abstract class MenuBase
{
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

    protected void DrawControlsText(SpriteBatch spriteBatch)
    {
        float scale = 0.5f;
        string text =
            "Controls\n\nMovement\n" +
            "[W] [A] [S] [D] -> Move\n" +
            "[Left Shift] -> Dash\n\n" +
            "Combat\n" +
            "[LMB] -> Attack\n\n" +
            "Items\n" +
            "[1] / [2] -> Select Item\n" +
            "[T] -> Use Item";
        Vector2 textPos = new Vector2(50, 250);
        Rectangle backgroundRect = GetTextRectangle(text, textPos, scale);
        spriteBatch.Draw(Pixel, backgroundRect, Color.Black * 0.60f);
        spriteBatch.DrawString(_font, text, textPos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
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
