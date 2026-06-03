using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Abilities;

public class AxeSlamAbility : BaseAbility
{
    private const float DamageMultiplier = 3.0f;
    private const float Range = 330f;
    private const float StunDuration = 1.0f;
    public override float CooldownProgress => MathHelper.Clamp(CooldownTimer / Cooldown, 0f, 1f);

    public AxeSlamAbility() : base(8.0f) { }

    public override void Load(ContentManager content)
    {
        Icon = content.Load<Texture2D>("icons/AxeSlamAbilityIcon");
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
            Vector2 aimDir = warrior.GetAbilityAimDirection(); // Gebruik de richting waarin je castte
            Vector2 castAnchor = warrior.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? warrior.GetPosition();

            int damage = (int)(warrior.CurrentDamage * DamageMultiplier);
            warrior.HitFrontalArea(castAnchor, aimDir, Range, damage, StunDuration);

            warrior.PlayAttackSound();
            warrior.ResetAttackTimer();
        }
    }
}