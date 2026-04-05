using Last_Hope.BaseModel;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;

namespace Last_Hope.Classes.Items;

public class BombItem : BaseItem
{
    public BombItem() 
        : base("Bomb", "Drops a bomb that damages nearby enemies.", maxCount: 3, startingCount: 1)
    {
    }

    protected override void OnUse(BasePlayer player)
    {
        var bomb = new Bomb(player.GetPosition(), Vector2.Zero);
        GameManager.GetGameManager().AddGameObject(bomb);
    }
}