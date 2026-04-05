using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Classes.Items;

public class DecoyItem : BaseItem
{
    public DecoyItem() 
        : base("Decoy", "Places a decoy to distract enemies.", maxCount: 2, startingCount: 1)
    {
    }

    protected override void OnUse(BasePlayer player)
    {
        var decoy = new Decoy(player.GetPosition(), Vector2.Zero, 100f);
        GameManager.GetGameManager().AddGameObject(decoy);
        GameManager.GetGameManager().ActiveDecoy = decoy;
    }
}