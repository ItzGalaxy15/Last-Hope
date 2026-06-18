using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Items;

/// <summary>
/// Standard recovery consumable that replenishes player health parameters immediately when consumed.
/// Source: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/constructors
/// </summary>
public class HealingPotion : BaseItem
{
    public const float DefaultHealAmount = 50f;
    private float _healAmount;

    /// <summary>
    /// Registers the healing potion as a usable item. Defaults to 50 HP healed per use with a max stack of 5.
    /// </summary>
    public HealingPotion(float healAmount = DefaultHealAmount)
        : base("Healing Potion", $"Heals {healAmount} HP", maxCount: 5, startingCount: 1)
    {
        _healAmount = healAmount;
    }

    /// <summary>
    /// Heals the player by the configured amount when the item is used.
    /// </summary>
    protected override void OnUse(BasePlayer player)
    {
        player.Heal(_healAmount);
    }
}