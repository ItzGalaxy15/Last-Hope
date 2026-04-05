using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.Engine;

namespace Last_Hope.Animations;

public class Explosion : GameObject
{
    private Texture2D _explosionTexture;
    private AnimationManager _animationManager;

    private Rectangle _destinationRectangle;

    private int _explosionFrameCount;
    private int _explosionColumns;
    private int _explosionRows;
    private int _explosionInterval;
    private string _textureName;
    private float _scale;

    public Explosion(Point position,
        int explosionFrameCount,
        int explosionColumns,
        int explosionRows,
        int explosionInterval,
        float scale,
        string textureName)
    {
        this._explosionFrameCount = explosionFrameCount;
        this._explosionColumns = explosionColumns;
        this._explosionRows = explosionRows;
        this._explosionInterval = explosionInterval;
        this._scale = scale;
        this._textureName = textureName;

        _destinationRectangle = new Rectangle(position.X, position.Y, 0, 0);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);

        _explosionTexture = content.Load<Texture2D>("TempExplosion");
        int explosionFrameWidth = _explosionTexture.Width / _explosionColumns;
        int explosionFrameHeight = _explosionTexture.Height / _explosionRows;

        _animationManager = new AnimationManager(
            _explosionFrameCount,
            _explosionColumns,
            new Vector2(explosionFrameWidth, explosionFrameHeight),
            _explosionInterval,
            false
        );

        _destinationRectangle.Width = (int)(explosionFrameWidth * _scale);
        _destinationRectangle.Height = (int)(explosionFrameHeight * _scale);

        _destinationRectangle.X -= _destinationRectangle.Width / 2;
        _destinationRectangle.Y -= _destinationRectangle.Height / 2;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _animationManager.Update();

        if (_animationManager.isFinished)
        {
            GameManager.GetGameManager().RemoveGameObject(this);
        }
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(
            _explosionTexture,
            _destinationRectangle,
            _animationManager.GetSourceRect(),
            Color.White
        );

        base.Draw(gameTime, spriteBatch);
    }
}