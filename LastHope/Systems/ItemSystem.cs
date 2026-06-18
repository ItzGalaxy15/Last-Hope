using Last_Hope.Classes.Items;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;

namespace Last_Hope.Systems.ItemSystem;

/// <summary>
/// System handling placement, throwing logic, and instant utilization of inventory items.
/// </summary>
public static class ItemSystem
{
    /// <summary>
    /// Drops or consumes the selected item directly at the player's current location feet.
    /// Source: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/selection-statements
    /// </summary>
    public static void PlaceSelectedItem(BasePlayer player)
    {
        GameManager gm = GameManager.GetGameManager();

        Vector2 spawnPosition = player.GetPosition() + new Vector2(player._bodyWidth * 0.5f);

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

    /// <summary>
    /// Throws the selected item toward the mouse cursor or aim direction.
    /// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Vector2.html
    /// </summary>
    public static void ThrowSelectedItemTowardMouse(BasePlayer player)
    {
        GameManager gm = GameManager.GetGameManager();

        Vector2 spawnPosition = player.GetPosition() + new Vector2(player._bodyWidth * 0.5f);

        Vector2 direction;
        if (KeybindStore.CurrentScheme == ControlScheme.KeyboardOnly && player.AimInput != Vector2.Zero)
        {
            direction = player.AimInput;
        }
        else
        {
            direction = gm.GetWorldMousePosition() - spawnPosition;
            if (direction == Vector2.Zero)
                return;
        }

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

    /// <summary>
    /// Removes the old decoy if one exists, then spawns a new one.
    /// </summary>
    public static void SpawnDecoy(GameManager gm, Vector2 spawnPosition, Vector2 initialVelocity)
    {
        if (gm.ActiveDecoy is not null)
            gm.RemoveGameObject(gm.ActiveDecoy);

        Decoy decoy = new Decoy(spawnPosition, initialVelocity, lifetimeSeconds: 5f);
        gm.AddGameObject(decoy);
        gm.ActiveDecoy = decoy;
    }
}
