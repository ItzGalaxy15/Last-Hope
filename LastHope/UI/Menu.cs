using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Last_Hope.Engine;
using Last_Hope.BaseModel;

namespace Last_Hope.UI;

public class Menu
{
    private GameManager gm => GameManager.GetGameManager();

    private SpriteFont _font => gm._font;
    private InputManager InputManager => gm.InputManager;
    public GameState PreviousState => _previousState;
    private GameState _previousState = GameState.StartMenu;
    private GameState _state
    {
        get => gm._state;
        set => gm._state = value;
    }
    private Game Game => gm.Game;
    private Texture2D Pixel => gm.Pixel;
    public List<GameObject> _gameObjects => gm._gameObjects;
    private List<GameObject> _toBeAdded => gm._toBeAdded;
    private List<GameObject> _toBeRemoved => gm._toBeRemoved;
    private ContentManager _content => gm._content;

    private void HandleInput(InputManager inputManager)
    {
        gm.HandleInput(inputManager);
    }
    private void CheckCollision()
    {
        gm.CheckCollision();
    }
    private void ResetGame()
    {
        gm.ResetGame();
    }


    private Rectangle GetTextRectangle(string text, Vector2 position, float scale = 1)
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
    public void UpdateStartMenu(GameTime gameTime)
    {
        string startText = "Start Game";
        Vector2 startPos = GetFontPosition(startText);
        Rectangle startRect = GetTextRectangle(startText, startPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 100);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);


        if (startRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            _state = GameState.Running;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
        }
    }


    public void DrawStartMenu(GameTime gametime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string startText = "Start Game";
        Vector2 startPos = GetFontPosition(startText);
        Rectangle startRect = GetTextRectangle(startText, startPos);
        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 100);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);
        spriteBatch.Begin();
        spriteBatch.Draw(Pixel, startRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, startText, startPos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }

    private void DrawControlsText(SpriteBatch spriteBatch)
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

    public void UpdateControlsMenu(GameTime gameTime)
    {
        string continueText = "Continue";
        Vector2 continuePos = GetFontPosition(continueText);
        Rectangle continueRect = GetTextRectangle(continueText, continuePos);

        if (continueRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            _state = _previousState;
        }
    }

    public void DrawControlsMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string continueText = "Continue";
        Vector2 continuePos = GetFontPosition(continueText);
        Rectangle continueRect = GetTextRectangle(continueText, continuePos);

        if (transformMatrix != null)
        {
            spriteBatch.Begin(transformMatrix: transformMatrix);
            foreach (GameObject gameObject in _gameObjects)
            {
                gameObject.Draw(gameTime, spriteBatch);
            }
            spriteBatch.End();
        }

        spriteBatch.Begin();
        DrawControlsText(spriteBatch);

        spriteBatch.Draw(Pixel, continueRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, continueText, continuePos, Color.White);
        spriteBatch.End();
    }

    public void UpdateRunningMenu(GameTime gameTime)
    {

        float scale = 0.5f;
        Vector2 topLeft = new Vector2(20, 100);

        string pauseText = "Pause Game";
        Vector2 pausePos = topLeft + new Vector2(10, 5);
        Rectangle pauseRect = GetTextRectangle(pauseText, pausePos, scale);

        Vector2 pauseSize = _font.MeasureString(pauseText) * scale;
        string quitText = "Quit Game";
        Vector2 quitPos = pausePos + new Vector2(pauseSize.X + 40, 0);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos, scale);

        Vector2 quitSize = _font.MeasureString(quitText) * scale;
        string restartText = "Restart Game";
        Vector2 restartPos = quitPos + new Vector2(quitSize.X + 40, 0);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos, scale);

        if (pauseRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            _state = GameState.Paused;
            return;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
            return;
        }

        if (restartRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            ResetGame();
            _state = GameState.Running;
        }
        // Handle input
        HandleInput(InputManager);


        // Update
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Update(gameTime);
        }

        // Check Collission
        CheckCollision();

        foreach (GameObject gameObject in _toBeAdded)
        {
            gameObject.Load(_content);
            _gameObjects.Add(gameObject);
        }
        _toBeAdded.Clear();

        foreach (GameObject gameObject in _toBeRemoved)
        {
            gameObject.Destroy();
            _gameObjects.Remove(gameObject);
        }
        _toBeRemoved.Clear();
    }


    public void DrawRunningMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        float scale = 0.5f;
        Vector2 topLeft = new Vector2(20, 100);

        string pauseText = "Pause Game";
        Vector2 pausePos = topLeft + new Vector2(10, 5);
        Rectangle pauseRect = GetTextRectangle(pauseText, pausePos, scale);


        Vector2 pauseSize = _font.MeasureString(pauseText) * scale;
        string quitText = "Quit Game";
        Vector2 quitPos = pausePos + new Vector2(pauseSize.X + 40, 0);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos, scale);

        Vector2 quitSize = _font.MeasureString(quitText) * scale;
        string restartText = "Restart Game";
        Vector2 restartPos = quitPos + new Vector2(quitSize.X + 40, 0);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos, scale);

        spriteBatch.Begin(transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(gameTime, spriteBatch);
        }
        spriteBatch.End();

        spriteBatch.Begin();
        spriteBatch.Draw(Pixel, pauseRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, pauseText, pausePos, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.Green, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        spriteBatch.End();
    }


    public void UpdatePausedMenu(GameTime gameTime)
    {
        string continueText = "Continue Game";
        Vector2 continuePos = GetFontPosition(continueText);
        Rectangle continueRect = GetTextRectangle(continueText, continuePos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 100);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (continueRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            _state = GameState.Running;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
        }
    }


    public void DrawPausedMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {

        string continueText = "Continue Game";
        Vector2 continuePos = GetFontPosition(continueText);
        Rectangle continueRect = GetTextRectangle(continueText, continuePos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 100);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        spriteBatch.Begin(transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(gameTime, spriteBatch);
        }
        spriteBatch.End();

        spriteBatch.Begin();
        DrawControlsText(spriteBatch);
        spriteBatch.Draw(Pixel, continueRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, continueText, continuePos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();

    }

    public void UpdateWinnerMenu(GameTime gameTime)
    {
        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (restartRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            ResetGame();
            _state = GameState.Running;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
        }
    }

    public void DrawWinnerMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string winnerText = "Winner";
        Vector2 positionWinner = GetFontPosition(winnerText);

        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);


        spriteBatch.Begin(transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(gameTime, spriteBatch);
        }

        spriteBatch.End();

        spriteBatch.Begin();
        spriteBatch.DrawString(_font, winnerText, positionWinner, Color.LimeGreen);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();
    }

    public void UpdateGameOverMenu(GameTime gameTime)
    {
        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);

        if (restartRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            ResetGame();
            _state = GameState.Running;
        }

        if (quitRect.Contains(InputManager.CurrentMouseState.Position) && InputManager.LeftMousePress())
        {
            Game.Exit();
        }
    }



    public void DrawGameOverMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    {
        string gameOverText = "Game Over";
        Vector2 positrionGameOver = GetFontPosition(gameOverText);

        string restartText = "Restart Game";
        Vector2 restartPos = GetFontPosition(restartText) + new Vector2(0, 100);
        Rectangle restartRect = GetTextRectangle(restartText, restartPos);

        string quitText = "Quit Game";
        Vector2 quitPos = GetFontPosition(quitText) + new Vector2(0, 200);
        Rectangle quitRect = GetTextRectangle(quitText, quitPos);


        spriteBatch.Begin(transformMatrix: transformMatrix);
        foreach (GameObject gameObject in _gameObjects)
        {
            gameObject.Draw(gameTime, spriteBatch);
        }

        spriteBatch.End();

        spriteBatch.Begin();
        spriteBatch.DrawString(_font, gameOverText, positrionGameOver, Color.Red);
        spriteBatch.Draw(Pixel, restartRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();
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
}

