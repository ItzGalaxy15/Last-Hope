using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Last_Hope.Classes.Abilities;

public class GiantArrowAbility : BaseAbility
{
    private const float ArrowSpeed = 800f;
    private const float DamageMultiplier = 4f;
    public override float CooldownProgress => MathHelper.Clamp(CooldownTimer / Cooldown, 0f, 1f);

    public GiantArrowAbility() : base(12.0f) { }

    public override void Load(ContentManager content)
    {
        Icon = content.Load<Texture2D>("icons/GiantArrow");
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

        var arrow = new GiantArrow(center, direction, ArrowSpeed, archer, (int)(archer.CurrentDamage * DamageMultiplier));
        GameManager.GetGameManager().AddGameObject(arrow);
    }
}