using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Items;

public class OneUp : BaseItem
{
    /// <summary>
    /// Registers the 1-Up as a usable item. Only one can be held at a time.
    /// </summary>
    public OneUp()
        : base("1-Up", "Grants an extra revive upon death.", maxCount: 1, startingCount: 1)
    {
    }

    /// <summary>
    /// Grants the player one extra life when the item is used.
    /// </summary>
    protected override void OnUse(BasePlayer player)
    {
        player.AddLife(1);
    }
}