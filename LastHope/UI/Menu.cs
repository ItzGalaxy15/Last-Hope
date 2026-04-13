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

    private SkillTreeUI _skillTreeUI;
    private bool _showSkillTree = false;

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

    //public void UpdateStartMenu(GameTime gameTime)
    //{
    //    if (InputManager.IsKeyPress(Keys.Enter)) _state = GameState.Running;
    //    if (InputManager.IsKeyPress(Keys.Escape)) Game.Exit();

    //}

    //public void DrawStartMenu(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    //{
    //    string enterText = "Press Enter to start the Game";
    //    string escapeText = "Press Escape to quit";
    //    Vector2 positionEnter = GetFontPosition(enterText);
    //    Vector2 positionEscape = GetFontPosition(escapeText);

    //    spriteBatch.Begin();
    //    spriteBatch.DrawString(_font, enterText, positionEnter, Color.White);
    //    spriteBatch.DrawString(_font, escapeText, positionEscape + new Vector2(0, 100), Color.Red);
    //    spriteBatch.End();
    //}


    public void UpdateRunningMenu(GameTime gameTime)
    {
        //Vector2 topLeft = new Vector2(20, 20);

        //string pauseText = "Pause Game";
        //Vector2 pausePos = topLeft;
        //Rectangle pauseRect = GetTextRectangle(pauseText, pausePos);

        //string quitText = "Quit Game";
        //Vector2 quitPos = topLeft + new Vector2(600, 0);
        //Rectangle quitRect = GetTextRectangle(quitText, quitPos);

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

        if (InputManager.IsKeyPress(Keys.N))
        {
            _showSkillTree = !_showSkillTree;
            if (_showSkillTree && _skillTreeUI == null)
            {
                _skillTreeUI = new SkillTreeUI(Pixel, Game.GraphicsDevice.Viewport.Width);
            }
        }

        if (_showSkillTree)
        {
            _skillTreeUI?.Update(gameTime, Game.GraphicsDevice.Viewport);
            return; // Pause the game underneath while the UI is open
        }

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
            _showSkillTree = false;
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

        //Vector2 topLeft = new Vector2(20, 20);

        //string pauseText = "Pause Game";
        //Vector2 pausePos = topLeft;
        //Rectangle pauseRect = GetTextRectangle(pauseText, pausePos);

        //string quitText = "Quit Game";
        //Vector2 quitPos = topLeft + new Vector2(600, 0);
        //Rectangle quitRect = GetTextRectangle(quitText, quitPos);

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

        Color metalBg = new Color(30, 35, 40, 220);
        Color metalBorder = new Color(130, 140, 150, 255);

        spriteBatch.Begin();
        
        // Premium styled metallic buttons
        DrawPremiumButton(spriteBatch, Pixel, pauseRect, metalBg, metalBorder);
        DrawPremiumButton(spriteBatch, Pixel, quitRect, metalBg, metalBorder);
        DrawPremiumButton(spriteBatch, Pixel, restartRect, metalBg, metalBorder);
        
        spriteBatch.DrawString(_font, pauseText, pausePos, Color.White, 0f, Vector2.Zero, scale * 0.8f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, quitText, quitPos, new Color(255, 100, 100), 0f, Vector2.Zero, scale * 0.8f, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_font, restartText, restartPos, Color.LimeGreen, 0f, Vector2.Zero, scale * 0.8f, SpriteEffects.None, 0f);
        spriteBatch.End();

        if (_showSkillTree && _skillTreeUI != null)
        {
            spriteBatch.Begin();
            // Deep rich textured ambient background (parchment/dark metal tone)
            spriteBatch.Draw(Pixel, new Rectangle(0, 0, Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height), new Color(22, 24, 28, 240));
            _skillTreeUI.Draw(gameTime, spriteBatch);
            spriteBatch.End();
        }
    }

    private void DrawPremiumButton(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color bg, Color border)
    {
        spriteBatch.Draw(pixel, rect, bg);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, 2), border);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - 2, rect.Width, 2), border);
        spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, 2, rect.Height), border);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - 2, rect.Top, 2, rect.Height), border);
    }

    //public void UpdateRunning(GameTime gameTime)
    //{

    //    if (InputManager.IsKeyPress(Keys.Space))
    //    {
    //        _state = GameState.Paused;
    //        return;
    //    }
    //    if (InputManager.IsKeyPress(Keys.Escape))
    //    {
    //        Game.Exit();
    //        return;
    //    }

    //    // Handle input
    //    HandleInput(InputManager);


    //    // Update
    //    foreach (GameObject gameObject in _gameObjects)
    //    {
    //        gameObject.Update(gameTime);
    //    }


    //    // Check Collission
    //    CheckCollision();

    //    foreach (GameObject gameObject in _toBeAdded)
    //    {
    //        gameObject.Load(_content);
    //        _gameObjects.Add(gameObject);
    //    }
    //    _toBeAdded.Clear();

    //    foreach (GameObject gameObject in _toBeRemoved)
    //    {
    //        gameObject.Destroy();
    //        _gameObjects.Remove(gameObject);
    //    }
    //    _toBeRemoved.Clear();
    //}


    //public void DrawRunning(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    //{

    //    string spaceText = "Press Space to pause the Game";
    //    string escapeText = "Press Escape to quit";
    //    Vector2 topLeft = new Vector2(20, 20);

    //    spriteBatch.Begin(transformMatrix: transformMatrix);
    //    foreach (GameObject gameObject in _gameObjects)
    //    {
    //        gameObject.Draw(gameTime, spriteBatch);
    //    }
    //    spriteBatch.End();

    //    spriteBatch.Begin();
    //    spriteBatch.DrawString(_font, spaceText, topLeft, Color.White);
    //    spriteBatch.DrawString(_font, escapeText, topLeft + new Vector2(0, 100), Color.Red);
    //    spriteBatch.End();
    //}



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
        spriteBatch.Draw(Pixel, continueRect, Color.DarkSlateGray);
        spriteBatch.Draw(Pixel, quitRect, Color.DarkSlateGray);
        spriteBatch.DrawString(_font, continueText, continuePos, Color.White);
        spriteBatch.DrawString(_font, quitText, quitPos, Color.Red);
        spriteBatch.End();

    }


    //public void UpdatePaused(GameTime gameTime)
    //{
    //    if (InputManager.IsKeyPress(Keys.Space)) _state = GameState.Running;
    //    if (InputManager.IsKeyPress(Keys.Escape)) Game.Exit();

    //}


    //public void DrawPaused(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    //{
    //    string spaceText = "Press Space to continue the Game";
    //    string escapeText = "Press Escape to quit";
    //    Vector2 positionEnter = GetFontPosition(spaceText);
    //    Vector2 positionEscape = GetFontPosition(escapeText);


    //    spriteBatch.Begin(transformMatrix: transformMatrix);
    //    foreach (GameObject gameObject in _gameObjects)
    //    {
    //        gameObject.Draw(gameTime, spriteBatch);
    //    }
    //    spriteBatch.End();

    //    spriteBatch.Begin();
    //    spriteBatch.DrawString(_font, spaceText, positionEnter, Color.White);
    //    spriteBatch.DrawString(_font, escapeText, positionEscape + new Vector2(0, 100), Color.Red);
    //    spriteBatch.End();
    //}


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
            _showSkillTree = false;
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
            _showSkillTree = false;
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

    //public void UpdateGameOver(GameTime gameTime)
    //{
    //    if (InputManager.IsKeyPress(Keys.Enter))
    //    {
    //        ResetGame();
    //        _state = GameState.Running;
    //    }


    //    if (InputManager.IsKeyPress(Keys.Escape)) Game.Exit();
    //}


    //public void DrawGameOver(GameTime gameTime, SpriteBatch spriteBatch, Matrix? transformMatrix = null)
    //{
    //    string gameOverText = "Game Over";
    //    string enterText = "Press Enter to restart the Game";
    //    string escapeText = "Press Escape to quit";
    //    Vector2 positrionGameOver = GetFontPosition(gameOverText);
    //    Vector2 positionEnter = GetFontPosition(enterText);
    //    Vector2 positionEscape = GetFontPosition(escapeText);

    //    spriteBatch.Begin(transformMatrix: transformMatrix);
    //    foreach (GameObject gameObject in _gameObjects)
    //    {
    //        gameObject.Draw(gameTime, spriteBatch);
    //    }

    //    spriteBatch.End();

    //    spriteBatch.Begin();
    //    spriteBatch.DrawString(_font, gameOverText, positrionGameOver, Color.Red);
    //    spriteBatch.DrawString(_font, enterText, positionEnter + new Vector2(0, 100), Color.White);
    //    spriteBatch.DrawString(_font, escapeText, positionEscape + new Vector2(0, 200), Color.Red);
    //    spriteBatch.End();
    //}


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
