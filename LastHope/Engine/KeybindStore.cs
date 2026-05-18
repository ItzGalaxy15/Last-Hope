using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.Engine;

//Reference used for inspiration: https://community.monogame.net/t/custom-key-binding-am-i-on-the-right-track/7740

/// <summary>
/// Defines the logical gameplay actions that can be mapped to physical user inputs.
/// </summary>
public enum KeybindId
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Dash,
    Teleport,
    /// <summary>Primary weapon input (default: left mouse button).</summary>
    Attack,
    Ability1,
    Ability2,
    ItemSlot1,
    ItemSlot2,
    PlaceItem,
    ThrowItem,
    AimUp,
    AimDown,
    AimLeft,
    AimRight,
    /// <summary>Keyboard-only attack key — fires in the current aim direction.</summary>
    KeyboardAttack,
}

/// <summary>
/// Specifies the hardware source of an input binding.
/// </summary>
public enum BindingKind : byte
{
    None,
    Keyboard,
    Mouse,
}

/// <summary>
/// Specifies which mouse button is used for a mouse-based input binding.
/// </summary>
public enum MouseBindButton : byte
{
    None,
    Left,
    Right,
    Middle,
}

/// <summary>
/// Represents a single, immutable physical input assignment (either a keyboard key or a mouse button).
/// </summary>
/// <remarks>
/// Acts essentially as a discriminated union, using <see cref="BindingKind"/> to dictate whether 
/// the <see cref="Key"/> or <see cref="Mouse"/> field holds the valid configuration.
/// </remarks>
public readonly struct GameInputBinding : IEquatable<GameInputBinding>
{
    public readonly BindingKind Kind;
    public readonly Keys Key;
    public readonly MouseBindButton Mouse;

    public GameInputBinding(BindingKind kind, Keys key, MouseBindButton mouse)
    {
        Kind = kind;
        Key = key;
        Mouse = mouse;
    }

    /// <summary>
    /// Indicates whether this binding represents an empty or unassigned state.
    /// </summary>
    public bool IsUnbound => Kind == BindingKind.None;

    /// <summary>
    /// Creates a new binding configured for a specific keyboard key.
    /// </summary>
    /// <param name="k">The key to bind. Passing <see cref="Keys.None"/> returns an unbound state.</param>
    public static GameInputBinding Keyboard(Keys k) =>
        k == Keys.None ? default : new(BindingKind.Keyboard, k, MouseBindButton.None);

    /// <summary>
    /// Creates a new binding configured for a specific mouse button.
    /// </summary>
    /// <param name="b">The mouse button to bind. Passing <see cref="MouseBindButton.None"/> returns an unbound state.</param>
    public static GameInputBinding FromMouse(MouseBindButton b) =>
        b == MouseBindButton.None ? default : new(BindingKind.Mouse, Keys.None, b);

    public bool Equals(GameInputBinding o) =>
        Kind == o.Kind
        && (Kind != BindingKind.Keyboard || Key == o.Key)
        && (Kind != BindingKind.Mouse || Mouse == o.Mouse);

    public override bool Equals(object? obj) => obj is GameInputBinding g && Equals(g);

    public override int GetHashCode() => HashCode.Combine(Kind, Key, Mouse);

    public override string ToString() => Format(this);

    /// <summary>Short label for settings UI (e.g. <c>LMB</c>, <c>RMB</c>, key names).</summary>
    public static string Format(GameInputBinding b) => b.Kind switch
    {
        BindingKind.Keyboard => FormatKey(b.Key),
        BindingKind.Mouse => b.Mouse switch
        {
            MouseBindButton.Left => "LMB",
            MouseBindButton.Right => "RMB",
            MouseBindButton.Middle => "MMB",
            _ => "?",
        },
        _ => "(none)",
    };

    private static string FormatKey(Keys k)
    {
        if (k == Keys.None)
            return "(none)";
        if (k >= Keys.D0 && k <= Keys.D9)
            return k.ToString().Replace("D", "");
        return k.ToString();
    }
}

public enum ControlScheme
{
    MouseAndKeyboard,
    KeyboardOnly,
}

/// <summary>
/// Manages the mapping between logical gameplay actions (<see cref="KeybindId"/>) and physical inputs (<see cref="GameInputBinding"/>).
/// In-memory gameplay bindings for the current run only (not written to disk).
/// </summary>
/// <remarks>
/// Based on the standard Input Mapping pattern to decouple hardware inputs from gameplay logic. 
/// Provides conflict resolution to ensure multiple actions aren't unintentionally mapped to the same key.
/// </remarks>
public static class KeybindStore
{
    private static readonly Dictionary<KeybindId, GameInputBinding> Defaults = new()
    {
        [KeybindId.MoveUp] = GameInputBinding.Keyboard(Keys.W),
        [KeybindId.MoveDown] = GameInputBinding.Keyboard(Keys.S),
        [KeybindId.MoveLeft] = GameInputBinding.Keyboard(Keys.A),
        [KeybindId.MoveRight] = GameInputBinding.Keyboard(Keys.D),
        [KeybindId.Dash] = GameInputBinding.Keyboard(Keys.LeftShift),
        [KeybindId.Teleport] = GameInputBinding.Keyboard(Keys.R),
        [KeybindId.Attack] = GameInputBinding.FromMouse(MouseBindButton.Left),
        [KeybindId.Ability1] = GameInputBinding.Keyboard(Keys.H),
        [KeybindId.Ability2] = GameInputBinding.Keyboard(Keys.E),
        [KeybindId.ItemSlot1] = GameInputBinding.Keyboard(Keys.D1),
        [KeybindId.ItemSlot2] = GameInputBinding.Keyboard(Keys.D2),
        [KeybindId.PlaceItem] = GameInputBinding.Keyboard(Keys.G),
        [KeybindId.ThrowItem] = GameInputBinding.FromMouse(MouseBindButton.Right),
        [KeybindId.AimUp] = GameInputBinding.Keyboard(Keys.Up),
        [KeybindId.AimDown] = GameInputBinding.Keyboard(Keys.Down),
        [KeybindId.AimLeft] = GameInputBinding.Keyboard(Keys.Left),
        [KeybindId.AimRight] = GameInputBinding.Keyboard(Keys.Right),
        [KeybindId.KeyboardAttack] = GameInputBinding.Keyboard(Keys.Space),
    };

    private static readonly Dictionary<KeybindId, GameInputBinding> Current = new(Defaults);

    public static ControlScheme CurrentScheme { get; set; } = ControlScheme.MouseAndKeyboard;

    /// <summary>
    /// Retrieves the current physical input assigned to a logical action.
    /// Falls back to the default binding if the action has no specific current override.
    /// </summary>
    /// <param name="id">The logical action to look up.</param>
    /// <returns>The active <see cref="GameInputBinding"/>.</returns>
    public static GameInputBinding GetBinding(KeybindId id) =>
        Current.TryGetValue(id, out GameInputBinding b) ? b : Defaults[id];

    /// <summary>
    /// Retrieves a human-readable string representation of the binding currently assigned to an action.
    /// </summary>
    public static string FormatBinding(KeybindId id) => GameInputBinding.Format(GetBinding(id));

    /// <summary>Returns another action that already uses <paramref name="binding"/>, if any (excluding <paramref name="exceptId"/>).</summary>
    public static KeybindId? FindBindingOwner(GameInputBinding binding, KeybindId exceptId)
    {
        if (binding.IsUnbound)
            return null;
        foreach (KeybindId id in Enum.GetValues<KeybindId>())
        {
            if (id == exceptId)
                continue;
            if (GetBinding(id).Equals(binding))
                return id;
        }
        return null;
    }

    /// <summary>
    /// Assigns <paramref name="newBinding"/> to <paramref name="targetId"/> and removes it from all other actions.
    /// Acts as a safe reassignment to prevent input mapping conflicts.
    /// </summary>
    public static void ApplyRebind(KeybindId targetId, GameInputBinding newBinding)
    {
        if (newBinding.IsUnbound)
            return;
        if (newBinding.Kind == BindingKind.Keyboard && newBinding.Key == Keys.Escape)
            return;

        foreach (KeybindId id in Enum.GetValues<KeybindId>())
        {
            if (id == targetId)
                continue;
            if (GetBinding(id).Equals(newBinding))
                Current[id] = default;
        }

        Current[targetId] = newBinding;
    }

    /// <summary>
    /// Directly forces a binding onto an action without resolving conflicts for other actions.
    /// Internally used to clear bindings if an unbound state is passed.
    /// </summary>
    public static void SetBinding(KeybindId id, GameInputBinding binding)
    {
        Current[id] = binding.IsUnbound ? default : binding;
    }

    /// <summary>
    /// Wipes all user-defined bindings and restores the default control scheme.
    /// </summary>
    public static void ResetToDefaults()
    {
        foreach (var kv in Defaults)
            Current[kv.Key] = kv.Value;
    }

    /// <summary>
    /// Provides a human-readable, UI-friendly label for a given logical action.
    /// </summary>
    /// <param name="id">The action to format.</param>
    /// <returns>A formatted string description (e.g. "Move up" instead of "MoveUp").</returns>
    public static string Label(KeybindId id) => id switch
    {
        KeybindId.MoveUp => "Move up",
        KeybindId.MoveDown => "Move down",
        KeybindId.MoveLeft => "Move left",
        KeybindId.MoveRight => "Move right",
        KeybindId.Dash => "Dash",
        KeybindId.Teleport => "Teleport",
        KeybindId.Attack => "Attack",
        KeybindId.Ability1 => "Ability 1",
        KeybindId.Ability2 => "Ability 2",
        KeybindId.ItemSlot1 => "Item slot 1",
        KeybindId.ItemSlot2 => "Item slot 2",
        KeybindId.PlaceItem => "Place Item (at feet)",
        KeybindId.ThrowItem => "Use/Throw Item",
        KeybindId.AimUp => "Aim up",
        KeybindId.AimDown => "Aim down",
        KeybindId.AimLeft => "Aim left",
        KeybindId.AimRight => "Aim right",
        KeybindId.KeyboardAttack => "Attack (keyboard)",
        _ => id.ToString(),
    };
}
