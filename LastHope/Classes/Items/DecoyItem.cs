using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Classes.Items;

public class DecoyItem : BaseItem
{
    /// <summary>
    /// Registers the decoy as a usable item with a maximum stack of 2.
    /// </summary>
    public DecoyItem()
        : base("Decoy", "Places a decoy to distract enemies.", maxCount: 2, startingCount: 1)
    {
    }

    /// <summary>
    /// Spawns a decoy at the player's current position, sets it as the active decoy so enemies will target it.
    /// </summary>
    protected override void OnUse(BasePlayer player)
    {
        var decoy = new Decoy(player.GetPosition(), Vector2.Zero, 100f);
        GameManager.GetGameManager().AddGameObject(decoy);
        GameManager.GetGameManager().ActiveDecoy = decoy;
    }
}