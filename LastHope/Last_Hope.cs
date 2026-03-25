using Last_Hope.Classes.Camera;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope;

public class Last_Hope : Game
{
    private GraphicsDeviceManager _graphics;
    private InputManager _inputManager;
    private GameManager _gameManager;
    private SpriteBatch _spriteBatch;
    private Texture2D _background;
    private Camera _camera;
    private Warrior _player;

    public Last_Hope()
    {
        _graphics = new GraphicsDeviceManager(this);

        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.IsFullScreen = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Initialize managers
        _inputManager = new InputManager();
        _gameManager = GameManager.GetGameManager();
        base.Initialize();

        var player = new Warrior(new Vector2(100, 100));
 
        _gameManager.AddGameObject(player);
        _gameManager.AddGameObject(new Goblin(new Point(600, 660)));
        _gameManager.AddGameObject(new Orc(new Point(600, 660)));
        _gameManager.Initialize(Content, this, player);
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _background = Content.Load<Texture2D>("Newbackground1");

        _camera = new Camera(
            new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            new Point(_background.Width, _background.Height),
            1.2f);

        _player = new Warrior(new Vector2(100, 100));
        gm.AddGameObject(_player);
        gm.AddGameObject(new Goblin(new Point(200, 160)));
        _background = Content.Load<Texture2D>("background");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        gm.Update(gameTime);
        _camera.Update(_player.Position);
        _gameManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(transformMatrix: _camera.ViewMatrix, samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_background, Vector2.Zero, Color.White);
        _spriteBatch.End();

        gm.Draw(gameTime, _spriteBatch, _camera.ViewMatrix);
        _gameManager.Draw(gameTime, _spriteBatch);

        base.Draw(gameTime);
    }
}
