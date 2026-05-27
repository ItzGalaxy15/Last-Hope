using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Last_Hope.BaseModel;
using Last_Hope.Classes.Weapon;
using Last_Hope.Engine;
using System;

namespace Last_Hope.Classes.Abilities;

public class ArrowRainAbility : BaseAbility
{
    private const float RainDuration = 3f;
    private const float Interval = 0.15f;
    private const float Radius = 180f;
    private const int ArrowsPerWave = 3;
    private const float ArrowSpawnHeight = 400f;
    private const float ArrowSpeed = 600f;
    private const float AimDistance = 250f;

    private bool _active;
    private float _rainTimer;
    private float _intervalTimer;
    private Vector2 _targetCenter;
    private Archer _caster;

    private Texture2D _circleTexture;

    public ArrowRainAbility() : base(cooldown: 12f) { }

    protected override void OnExecute(BasePlayer player)
    {
        _caster = player as Archer;

        Vector2 playerCenter = _caster._position + new Vector2(_caster._bodyWidth * 0.5f);
        _targetCenter = playerCenter + _caster.CurrentAimDirection * AimDistance;

        _active = true;
        _rainTimer = RainDuration;
        _intervalTimer = 0f;
    }

    public override void Update(BasePlayer player, GameTime gameTime)
    {
        base.Update(player, gameTime);

        if (!_active) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _rainTimer -= dt;
        _intervalTimer -= dt;

        if (_intervalTimer <= 0f)
        {
            _intervalTimer = Interval;
            SpawnWave();
        }

        if (_rainTimer <= 0f)
            _active = false;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!_active) return;

        _circleTexture ??= CreateCircleTexture(spriteBatch.GraphicsDevice, Radius);
        spriteBatch.Draw(_circleTexture, _targetCenter - new Vector2(Radius), Color.White * 0.5f);
    }

    private void SpawnWave()
    {
        if (_caster is null) return;

        var rng = GameManager.GetGameManager().RNG;
        var bow = (Bow)_caster._Weapon;

        for (int i = 0; i < ArrowsPerWave; i++)
        {
            float angle = rng.NextSingle() * MathF.Tau;
            float dist = rng.NextSingle() * Radius;
            Vector2 target = _targetCenter + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * dist;
            Vector2 spawnPos = target - new Vector2(0f, ArrowSpawnHeight);
            float impactY = target.Y - 40f;
            var arrow = new RainArrow(
                spawnPos, impactY, ArrowSpeed, _caster,
                _caster.CurrentDamage, _caster.CurrentCritChance,
                bow.piercingArrows, bow.poisonArrows, bow.spreadPoison, bow.increasedPoisonDamage,
                bow.explosiveArrows, bow.increasedExplosionRadius, bow.increasedExplosionDamage,
                bow.clusterBomb, bow.OnHitCallBack
            );
            GameManager.GetGameManager().AddGameObject(arrow);
        }
    }

    private Texture2D CreateCircleTexture(GraphicsDevice graphicsDevice, float radius)
    {
        int diameter = (int)(radius * 2);
        var texture = new Texture2D(graphicsDevice, diameter, diameter);
        Color[] data = new Color[diameter * diameter];

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float dx = x - radius;
                float dy = y - radius;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist <= radius - 2f)
                    data[y * diameter + x] = Color.Red;
                else if (dist <= radius)
                    data[y * diameter + x] = Color.DarkRed;
                else
                    data[y * diameter + x] = Color.Transparent;
            }
        }

        texture.SetData(data);
        return texture;
    }
}