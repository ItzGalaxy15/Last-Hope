using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Abilities;

/// <summary>
/// Warrior ability that forces a sustained spinning state, triggering rapid radial slashes over time.
/// Source: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/override
/// </summary>
public class WhirlwindAbility : BaseAbility
{
    private float _durationTimer;
    private float _tickTimer;
    private const float Duration = 2.0f;
    private const float TickInterval = 0.15f;

    /// <summary>
    /// Calculates the normalized progress of the ability's cooldown.
    /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.MathHelper.html
    /// </summary>
    public override float CooldownProgress => MathHelper.Clamp(CooldownTimer / Cooldown, 0f, 1f);

    public WhirlwindAbility() : base(8.0f) { }

    public override void Load(ContentManager content)
    {
        Icon = content.Load<Texture2D>("icons/WhirlwindAbilityIcon");
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