using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Items;

public class HealingPotion : BaseItem
{
    private float _healAmount;

    public HealingPotion(float healAmount = 50f) 
        : base("Healing Potion", $"Heals {healAmount} HP", maxCount: 5, startingCount: 1)
    {
        _healAmount = healAmount;
    }

    protected override void OnUse(BasePlayer player)
    {
        player.Heal(_healAmount);
    }
}