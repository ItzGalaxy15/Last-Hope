using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Abilities;

public class ShieldSlamAbility : BaseAbility
{
    private const float DamageMultiplier = 0.5f; // Low damage
    private const float Radius = 150f;
    private const float StunDuration = 3.0f; // Stun AoE

    public ShieldSlamAbility() : base(8.0f) { }

    protected override void OnExecute(BasePlayer player)
    {
        if (player is Warrior warrior)
        {
            int damage = (int)(warrior._Weapon.Damage * DamageMultiplier);
            warrior.HitRadialArea(Radius, damage, StunDuration);

            warrior.PlayAttackSound();
            warrior.ResetAttackTimer();
            warrior.FireRadialSlashes(); // Re-use multiple slashes to indicate the radial shockwave
        }
    }
}