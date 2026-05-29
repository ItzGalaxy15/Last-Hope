using Last_Hope.BaseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Last_Hope.Classes.Abilities;

public class ShieldSlamAbility : BaseAbility
{
    private const float DamageMultiplier = 0.5f; // Low damage
    private const float Radius = 150f;
    private const float StunDuration = 3.0f; // Stun AoE
    public override float CooldownProgress => MathHelper.Clamp(CooldownTimer / Cooldown, 0f, 1f);

    public ShieldSlamAbility() : base(8.0f) { }

    public override void Load(ContentManager content)
    {
        
    }

    protected override void OnExecute(BasePlayer player)
    {
        if (player is Warrior warrior)
        {
            warrior.StartAbilityAnimation(this);
        }
    }

    public override void PerformHit(BasePlayer player)
    {
        if (player is Warrior warrior)
        {
            int damage = (int)(warrior.CurrentDamage * DamageMultiplier);
            Vector2 aimDir = warrior.GetAbilityAimDirection();
            Vector2 castAnchor = warrior.GetCastAnchor();
            warrior.HitFrontalArea(castAnchor, aimDir, Radius, damage, StunDuration);

            warrior.PlayAttackSound();
            warrior.ResetAttackTimer();
        }
    }
}