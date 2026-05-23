using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Last_Hope.Engine;

namespace Last_Hope.Classes.Abilities;

public class AxeSlamAbility : BaseAbility
{
    private const float DamageMultiplier = 3.0f;
    private const float Range = 140f;
    private const float StunDuration = 1.0f;

    public AxeSlamAbility() : base(8.0f) { }

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

            int damage = (int)(warrior._Weapon.Damage * DamageMultiplier);
            warrior.HitFrontalArea(aimDir, Range, damage, StunDuration);

            warrior.PlayAttackSound();
            warrior.ResetAttackTimer();
        }
    }
}