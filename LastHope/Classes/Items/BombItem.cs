using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Classes.Items;

/// <summary>
/// Item definition for the bomb consumable.
/// Source: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/object-oriented/inheritance
/// </summary>
public class BombItem : BaseItem
{
    /// <summary>
    /// Registers the bomb as a usable item with a maximum stack of 3.
    /// </summary>
    public BombItem()
        : base("Bomb", "Drops a bomb that damages nearby enemies.", maxCount: 3, startingCount: 1)
    {
    }

    /// <summary>
    /// Spawns a bomb at the player's current position when the item is used.
    /// </summary>
    protected override void OnUse(BasePlayer player)
    {
        var bomb = new Bomb(player.GetPosition(), Vector2.Zero);
        GameManager.GetGameManager().AddGameObject(bomb);
    }
}