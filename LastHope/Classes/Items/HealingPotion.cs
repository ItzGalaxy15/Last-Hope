using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Items;

public class HealingPotion : BaseItem
{
    public const float DefaultHealAmount = 50f;
    private float _healAmount;

    public HealingPotion(float healAmount = DefaultHealAmount) 
        : base("Healing Potion", $"Heals {healAmount} HP", maxCount: 5, startingCount: 1)
    {
        _healAmount = healAmount;
    }

    protected override void OnUse(BasePlayer player)
    {
        player.Heal(_healAmount);
    }
}