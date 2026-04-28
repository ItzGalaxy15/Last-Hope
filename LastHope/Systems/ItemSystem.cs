using Last_Hope.Classes.Items;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;

namespace Last_Hope.Systems.ItemSystem;

public static class ItemSystem
{
    public static void PlaceSelectedItem(BasePlayer player)
    {
        GameManager gm = GameManager.GetGameManager();

        Vector2 spawnPosition = player.GetPosition() + new Vector2(GetBodyWidth(player) * 0.5f);

        ItemType[] inv = player.Inventory!;
        ItemType currentItem = inv[gm.SelectedItemSlot];

        if (currentItem == ItemType.None)
            return;

        switch (currentItem)
        {
            case ItemType.Decoy:
                SpawnDecoy(gm, spawnPosition, Vector2.Zero);
                break;

            case ItemType.Bomb:
                gm.AddGameObject(new Bomb(spawnPosition, Vector2.Zero));
                break;

            case ItemType.HealingPotion:
                player.Heal(50f);
                break;

            case ItemType.OneUp:
                player.AddLife(1);
                gm.HasUsedOneUp = true;
                break;
        }

        inv[gm.SelectedItemSlot] = ItemType.None;
    }

    public static void ThrowSelectedItemTowardMouse(BasePlayer player)
    {
        GameManager gm = GameManager.GetGameManager();

        Vector2 spawnPosition = player.GetPosition() + new Vector2(GetBodyWidth(player) * 0.5f);

        Vector2 mouseWorld = gm.GetWorldMousePosition();
        Vector2 direction = mouseWorld - spawnPosition;

        if (direction == Vector2.Zero)
            return;

        direction.Normalize();

        ItemType[] inv = player.Inventory!;
        ItemType currentItem = inv[gm.SelectedItemSlot];

        if (currentItem == ItemType.None)
            return;

        switch (currentItem)
        {
            case ItemType.Decoy:
                SpawnDecoy(gm, spawnPosition, direction * 420f);
                break;

            case ItemType.Bomb:
                gm.AddGameObject(new Bomb(spawnPosition, direction * 520f));
                break;

            case ItemType.HealingPotion:
                player.Heal(50f);
                break;

            case ItemType.OneUp:
                player.AddLife(1);
                gm.HasUsedOneUp = true;
                break;
        }

        inv[gm.SelectedItemSlot] = ItemType.None;
    }

    public static void SpawnDecoy(GameManager gm, Vector2 spawnPosition, Vector2 initialVelocity)
    {
        if (gm.ActiveDecoy is not null)
            gm.RemoveGameObject(gm.ActiveDecoy);

        Decoy decoy = new Decoy(spawnPosition, initialVelocity, lifetimeSeconds: 5f);
        gm.AddGameObject(decoy);
        gm.ActiveDecoy = decoy;
    }

    // helper (since _bodyWidth is protected abstract)
    private static float GetBodyWidth(BasePlayer player)
    {
        var field = typeof(BasePlayer)
            .GetProperty("_bodyWidth", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        return field != null ? (float)field.GetValue(player)! : 64f;
    }
}