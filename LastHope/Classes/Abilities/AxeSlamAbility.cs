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
            Vector2 aimDir = GameManager.GetGameManager().GetWorldMousePosition() - warrior.GetPosition();
            if (aimDir != Vector2.Zero) aimDir.Normalize();

            int damage = (int)(warrior._Weapon.Damage * DamageMultiplier);
            warrior.HitFrontalArea(aimDir, Range, damage, StunDuration);

            warrior.PlayAttackSound();
            warrior.ResetAttackTimer();
            warrior._Weapon.Attack(aimDir, warrior.GetCastAnchor(), warrior.CurrentDamage, warrior.CurrentCritChance);
        }
    }
}