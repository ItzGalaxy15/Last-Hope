using Last_Hope.BaseModel;

namespace Last_Hope.Classes.Items;

public class OneUp : BaseItem
{
    public OneUp() 
        : base("1-Up", "Grants an extra revive upon death.", maxCount: 1, startingCount: 1)
    {
    }

    protected override void OnUse(BasePlayer player)
    {
        player.AddLife(1);
    }
}