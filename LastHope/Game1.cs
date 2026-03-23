using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private InputManager _inputManager;
    private SpriteBatch _spriteBatch;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = 1080;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.IsFullScreen = false; 
    
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

    }

    protected override void Initialize()
    {
        var gm = GameManager.GetGameManager();
        _inputManager = new InputManager();

        gm.Initialize(Content, this);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        var gm = GameManager.GetGameManager();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        gm.AddGameObject(new Warrior(new Vector2(100, 100)));
    }

    protected override void Update(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        gm.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        GraphicsDevice.Clear(Color.CornflowerBlue);

        gm.Draw(gameTime, _spriteBatch);

        base.Draw(gameTime);
    }
}
