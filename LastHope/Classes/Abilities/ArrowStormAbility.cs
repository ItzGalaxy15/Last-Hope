using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Last_Hope.Classes.Weapon;
using Last_Hope.Helpers;
using System;

namespace Last_Hope.Classes.Abilities;

public class ArrowStormAbility : BaseAbility
{
    private const float ArrowSpeed = 800f;
    private const int ArrowCount = 10;
    private const float TimeBetweenArrows = 0.1f;

    private float _fireTimer = 0f;
    private int _arrowsFired = 0;
    private bool _isFiring = false;
    private Vector2 _firingDirection;

    public ArrowStormAbility() : base(15.0f) { }

    public override void Update(BasePlayer player, GameTime gameTime)
    {
        base.Update(player, gameTime);

        if (!_isFiring) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fireTimer = TimerHelper.DecreaseTimer(_fireTimer, dt);

        if (_fireTimer <= 0f && _arrowsFired < ArrowCount)
        {
            FireSingleArrow(player);
            _arrowsFired++;
            _fireTimer = TimeBetweenArrows;
        }

        if (_arrowsFired >= ArrowCount)
        {
            _isFiring = false;
            _arrowsFired = 0;
        }
    }

    protected override void OnExecute(BasePlayer player)
    {
        if (player is not Archer archer)
            return;

        Vector2 center = archer._position + new Vector2(archer._bodyWidth * 0.5f, archer._bodyWidth * 0.5f);
        Vector2 mousePosition = GameManager.GetGameManager().GetWorldMousePosition();
        Vector2 direction = mousePosition - center;

        if (direction == Vector2.Zero)
            return;

        direction.Normalize();
        _firingDirection = direction;
        _isFiring = true;
        _arrowsFired = 0;
        _fireTimer = 0f;
    }

    private void FireSingleArrow(BasePlayer player)
    {
        if (player is not Archer archer)
            return;

        Vector2 center = archer._position + new Vector2(archer._bodyWidth * 0.5f, archer._bodyWidth * 0.5f);
        var bow = (Bow)archer._Weapon;

        // https://stackoverflow.com/questions/14607640/rotating-a-vector-in-3d-space resource used for vector rotation
        float angleOffset = (float)(GameManager.GetGameManager().RNG.NextDouble() * MathHelper.ToRadians(30) - MathHelper.ToRadians(15));
        Vector2 spreadDirection = VectorHelper.RotateVector(_firingDirection, MathHelper.ToDegrees(angleOffset));

        var arrow = new Arrow(center, spreadDirection, ArrowSpeed, archer, archer.CurrentDamage, archer.CurrentCritChance, bow.piercingArrows,
                              bow.poisonArrows, bow.spreadPoison, bow.increasedPoisonDamage, 
                              bow.explosiveArrows, bow.increasedExplosionRadius, bow.increasedExplosionDamage,
                              bow.OnHitCallBack);
        GameManager.GetGameManager().AddGameObject(arrow);
    }
}