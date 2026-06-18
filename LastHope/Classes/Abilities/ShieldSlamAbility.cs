using Last_Hope.BaseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Abilities;

/// <summary>
/// Warrior ability that trades raw damage for a large crowd-control stun radius centered on the player.
/// Source: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/inheritance
/// </summary>
public class ShieldSlamAbility : BaseAbility
{
    private const float DamageMultiplier = 0.5f;
    private const float Radius = 180f;
    private const float StunDuration = 3.0f;
    /// <summary>
    /// Calculates the normalized progress of the ability's cooldown.
    /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.MathHelper.html
    /// </summary>
    public override float CooldownProgress => MathHelper.Clamp(CooldownTimer / Cooldown, 0f, 1f);

    public ShieldSlamAbility() : base(8.0f) { }

    public override void Load(ContentManager content)
    {
        Icon = content.Load<Texture2D>("icons/ShieldSlamAbilityIcon");
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
            // Shield slam needs a center. Using the center of the player instead of looking for aim direction anchor.
            Vector2 castAnchor = warrior.GetCollider()?.GetBoundingBox().Center.ToVector2() ?? warrior.GetPosition();
            warrior.HitCircularArea(castAnchor, Radius, damage, StunDuration);

            warrior.PlayAttackSound();
            warrior.ResetAttackTimer();
        }
    }
}