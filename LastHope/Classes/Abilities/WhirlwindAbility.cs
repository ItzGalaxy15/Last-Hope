using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework.Content;

namespace Last_Hope.Classes.Abilities;

public class WhirlwindAbility : BaseAbility
{
    private float _durationTimer;
    private float _tickTimer;
    private const float Duration = 2.0f;
    private const float TickInterval = 0.15f;
    public override float CooldownProgress => MathHelper.Clamp(CooldownTimer / Cooldown, 0f, 1f);

    public WhirlwindAbility() : base(8.0f) { }

    public override void Load(ContentManager content)
    {
        
    }

    public override void Update(BasePlayer player, GameTime gameTime)
    {
        base.Update(player, gameTime);

        if (_durationTimer > 0f)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _durationTimer -= dt;
            _tickTimer -= dt;

            if (player is Warrior warrior)
            {
                warrior.SetSpinningAnimation(gameTime);

                if (_tickTimer <= 0f)
                {
                    _tickTimer = TickInterval;
                    warrior.FireRadialSlashes();
                }
            }
        }
    }

    protected override void OnExecute(BasePlayer player)
    {
        _durationTimer = Duration;
        _tickTimer = 0f;
    }
}