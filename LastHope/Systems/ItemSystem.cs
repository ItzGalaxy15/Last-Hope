using Last_Hope.Classes.Items;
using Last_Hope.Engine;
using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;

namespace Last_Hope.Systems.ItemSystem;

/// <summary>
/// A static utility facade for handling all item placement, throwing, and spawning logic.
/// Abstracts item usage logic away from the core Player controllers.
/// </summary>
/// <remarks>
/// Implements the Facade Pattern to simplify complex subsystem interactions.
/// <see href="https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-application-layer-implementation-web-api">Microsoft Facade Pattern</see>
/// </remarks>
public static class ItemSystem
{
    /// <summary>
    /// Executes the effect of the currently selected item and removes it from the player's inventory.
    /// Spawns physics objects natively at the player's location with no velocity.
    /// </summary>
    /// <param name="player">The player character utilizing the item.</param>
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
    /// Executes the effect of the currently selected item and removes it from the player's inventory.
    /// Calculates normalized vector paths based on input method (Keyboard or Mouse) to apply velocity to physical spawns.
    /// </summary>
    /// <param name="player">The player character utilizing the item.</param>
    /// <remarks>
    /// Utilizes <c>Vector2.Normalize()</c> to establish a consistent directional magnitude regardless of mouse distance.
    /// <see href="https://docs.monogame.net/api/Microsoft.Xna.Framework.Vector2.html">MonoGame Vector2 Structures</see>
    /// </remarks>
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
    /// Instantiates a new Decoy object into the active game simulation.
    /// Automatically detects and cleans up any existing Decoys to prevent overlapping exploitation.
    /// </summary>
    /// <param name="gm">The global GameManager instance.</param>
    /// <param name="spawnPosition">The calculated world-space coordinates to spawn the entity.</param>
    /// <param name="initialVelocity">The directional magnitude applied to the entity upon creation.</param>
    public static void SpawnDecoy(GameManager gm, Vector2 spawnPosition, Vector2 initialVelocity)
    {
        if (gm.ActiveDecoy is not null)
            gm.RemoveGameObject(gm.ActiveDecoy);

        Decoy decoy = new Decoy(spawnPosition, initialVelocity, lifetimeSeconds: 5f);
        gm.AddGameObject(decoy);
        gm.ActiveDecoy = decoy;
    }
}