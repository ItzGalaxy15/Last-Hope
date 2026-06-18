using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Items;

/// <summary>
/// Life extension resource blueprint that instantly implements character fallback revive stacks upon retrieval.
/// Source: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/base
/// </summary>
public class OneUp : BaseItem
{
    /// <summary>
    /// Registers the 1-Up as an instant-use item. Its effect is applied on pickup.
    /// </summary>
    public OneUp()
        : base("1-Up", "Instantly grants an extra revive upon death.", maxCount: 1, startingCount: 1)
    {
    }

    /// <summary>
    /// Grants the player one extra life. This is now handled automatically on pickup.
    /// </summary>
    protected override void OnUse(BasePlayer player)
    {
        player.AddLife(1);
    }
}