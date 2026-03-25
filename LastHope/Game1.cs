using Last_Hope.Classes.Camera;
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
    private Texture2D _background;
    private Camera _camera;
    private Warrior _player;

    public Game1()
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
        var gm = GameManager.GetGameManager();
        _inputManager = new InputManager();

        gm.Initialize(Content, this);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        var gm = GameManager.GetGameManager();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _background = Content.Load<Texture2D>("Newbackground1");

        _camera = new Camera(
            new Point(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height),
            new Point(_background.Width, _background.Height),
            1.2f);

        _player = new Warrior(new Vector2(100, 100));
        gm.AddGameObject(_player);
        gm.AddGameObject(new Goblin(new Point(200, 160)));
    }

    protected override void Update(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        gm.Update(gameTime);
        _camera.Update(_player.Position);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var gm = GameManager.GetGameManager();
        GraphicsDevice.Clear(Color.CornflowerBlue);

        _spriteBatch.Begin(transformMatrix: _camera.ViewMatrix, samplerState: SamplerState.LinearClamp);
        _spriteBatch.Draw(_background, Vector2.Zero, Color.White);
        _spriteBatch.End();

        gm.Draw(gameTime, _spriteBatch, _camera.ViewMatrix);

        base.Draw(gameTime);
    }
}
