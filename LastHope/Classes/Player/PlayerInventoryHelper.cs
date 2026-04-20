using Last_Hope.BaseModel;
using Last_Hope.Classes.Items;

namespace Last_Hope;

/// <summary>
/// Hotbar / pickup helpers: any <see cref="BasePlayer"/> with <see cref="BasePlayer.Inventory"/> set is included automatically.
/// </summary>
internal static class PlayerInventoryHelper
{
    internal static ItemType[]? GetInventorySlots(BasePlayer? player) => player?.Inventory;

    internal static bool TryPickup(BasePlayer? player, ItemType item) =>
        player?.TryPickupItem(item) ?? false;

    internal static int GetHudExtraLives(BasePlayer? player) => player?.ExtraLives ?? 0;
}
