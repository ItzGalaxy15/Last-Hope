using System;
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
    private Vector2 _center;

    private int _explosionFrameCount;
    private int _explosionColumns;
    private int _explosionRows;
    private int _explosionInterval;
    private string _textureName;
    private float _scale;
    private float _hitboxRadius;

    private const bool DebugDrawHitbox = true;

    public Explosion(Point position,
        int explosionFrameCount,
        int explosionColumns,
        int explosionRows,
        int explosionInterval,
        float scale,
        string textureName,
        float hitboxRadius = 0f)
    {
        this._explosionFrameCount = explosionFrameCount;
        this._explosionColumns = explosionColumns;
        this._explosionRows = explosionRows;
        this._explosionInterval = explosionInterval;
        this._scale = scale;
        this._textureName = textureName;
        this._hitboxRadius = hitboxRadius;

        _center = position.ToVector2();
        _destinationRectangle = new Rectangle(position.X, position.Y, 0, 0);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);

        _explosionTexture = content.Load<Texture2D>(_textureName);
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

        if (DebugDrawHitbox && _hitboxRadius > 0f)
            DrawHitbox(spriteBatch, _center, _hitboxRadius, Color.Red);

        base.Draw(gameTime, spriteBatch);
    }

    private static void DrawHitbox(SpriteBatch spriteBatch, Vector2 center, float radius, Color color)
    {
        Texture2D pixel = GameManager.GetGameManager().Pixel;
        const int segments = 32;
        float angleStep = MathHelper.TwoPi / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep;
            float nextAngle = angle + angleStep;
            Vector2 p1 = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
            Vector2 p2 = center + new Vector2((float)Math.Cos(nextAngle), (float)Math.Sin(nextAngle)) * radius;

            Vector2 diff = p2 - p1;
            float length = diff.Length();
            float lineAngle = (float)Math.Atan2(diff.Y, diff.X);
            spriteBatch.Draw(pixel, p1, null, color, lineAngle, Vector2.Zero, new Vector2(length, 2f), SpriteEffects.None, 0f);
        }
    }
}