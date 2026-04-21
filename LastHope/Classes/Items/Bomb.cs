using System;
using System.Collections.Generic;
using Last_Hope.Animations;
using Last_Hope.BaseModel;
using Last_Hope.Collision;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Items;

public class Bomb : GameObject
{
    private readonly RectangleCollider _collider;
    private Vector2 _position;
    private Vector2 _velocity;
    private float _fuseSeconds;
    private readonly float _initialFuseSeconds;
    private bool _exploded;

    private Texture2D? _bombSpriteSheet;
    private int _currentFuseFrame;

    private const float ExplosionRadius = 100f;
    private const float ExplosionDamage = 100f;

    private const int BombFrameSize = 32;
    private const int BombFrameCount = 8;
    private const int BombSpriteRowY = 0; // top row with bomb fuse frames
    private const float BombDrawScale = 1f;

    public Bomb(Vector2 position, Vector2 initialVelocity, float fuseSeconds = 3f)
    {
        _position = position;
        _velocity = initialVelocity;
        _fuseSeconds = fuseSeconds;
        _initialFuseSeconds = (float)Math.Max(0.01f, fuseSeconds);

        _collider = new RectangleCollider(new Rectangle((int)position.X - 8, (int)position.Y - 8, 16, 16));
        SetCollider(_collider);
    }

    public override void Load(ContentManager content)
    {
        base.Load(content);

        try
        {
            _bombSpriteSheet = content.Load<Texture2D>("itemSpriteSheet");
        }
        catch (ContentLoadException)
        {
            _bombSpriteSheet = null;
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_exploded)
            return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _position += _velocity * dt;
        _velocity *= 0.92f; // slight drag so thrown bombs slow down

        _collider.shape.Location = new Point((int)_position.X - 8, (int)_position.Y - 8);

        _fuseSeconds -= dt;

        float fuseProgress = 1f - (float)Math.Max(_fuseSeconds, 0f) / _initialFuseSeconds;
        _currentFuseFrame = Math.Clamp((int)(fuseProgress * BombFrameCount), 0, BombFrameCount - 1);

        if (_fuseSeconds <= 0f)
            Explode();

        base.Update(gameTime);
    }

    private void Explode()
    {
        if (_exploded)
            return;

        _exploded = true;
        GameManager gm = GameManager.GetGameManager();

        foreach (GameObject obj in gm.GetObjectsInRadius(_position, ExplosionRadius))
        {
            if (obj is not BaseEnemy enemy)
                continue;

            enemy.Damage(ExplosionDamage);
            if (enemy.CurrentHealth <= 0f)
            {
                gm._player?.AddExperience(enemy.ExperienceValue);
                gm.RemoveGameObject(enemy);
            }
        }

        // Explosion animation object (set textureName to your actual content asset name).
        gm.AddGameObject(new Explosion(
            position: _position.ToPoint(),
            explosionFrameCount: 6,
            explosionColumns: 6,
            explosionRows: 1,
            explosionInterval: 4,
            scale: 2.0f,
            textureName: "explosion",
            hitboxRadius: ExplosionRadius));

        gm.RemoveGameObject(this);
    }

    public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
    if (_bombSpriteSheet is not null)
    {
        Rectangle source = new Rectangle(_currentFuseFrame * BombFrameSize, BombSpriteRowY, BombFrameSize, BombFrameSize);
        Vector2 origin = new Vector2(BombFrameSize * 0.5f, BombFrameSize * 0.5f);

        spriteBatch.Draw(
            _bombSpriteSheet,
            _position,
            source,
            Color.White,
            0f,
            origin,
            BombDrawScale,
            SpriteEffects.None,
            0f);
    }
    else
    {
        Texture2D pixel = GameManager.GetGameManager().Pixel;
        spriteBatch.Draw(pixel, new Rectangle((int)_position.X - 6, (int)_position.Y - 6, 12, 12), Color.Black);
    }

    base.Draw(gameTime, spriteBatch);
}
}