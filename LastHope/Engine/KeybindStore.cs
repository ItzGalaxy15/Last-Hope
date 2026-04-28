using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Last_Hope.Engine;

//Reference used for inspiration: https://community.monogame.net/t/custom-key-binding-am-i-on-the-right-track/7740

public enum KeybindId
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Dash,
    /// <summary>Primary weapon input (default: left mouse button).</summary>
    Attack,
    ItemSlot1,
    ItemSlot2,
    PlaceItem,
    ThrowItem,
}

public enum BindingKind : byte
{
    None,
    Keyboard,
    Mouse,
}

public enum MouseBindButton : byte
{
    None,
    Left,
    Right,
    Middle,
}

/// <summary>Single gameplay input: a keyboard key or a mouse button.</summary>
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

    public bool IsUnbound => Kind == BindingKind.None;

    public static GameInputBinding Keyboard(Keys k) =>
        k == Keys.None ? default : new(BindingKind.Keyboard, k, MouseBindButton.None);

    public static GameInputBinding FromMouse(MouseBindButton b) =>
        b == MouseBindButton.None ? default : new(BindingKind.Mouse, Keys.None, b);

    public bool Equals(GameInputBinding o) =>
        Kind == o.Kind
        && (Kind != BindingKind.Keyboard || Key == o.Key)
        && (Kind != BindingKind.Mouse || Mouse == o.Mouse);

    public override bool Equals(object? obj) => obj is GameInputBinding g && Equals(g);

    public override int GetHashCode() => HashCode.Combine(Kind, Key, Mouse);

    /// <summary>Short label for settings UI (e.g. <c>LMB</c>, <c>RMB</c>, key names).</summary>
    public static string Format(GameInputBinding b) => b.Kind switch
    {
        BindingKind.None => "(none)",
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

/// <summary>In-memory gameplay bindings for the current run only (not written to disk).</summary>
public static class KeybindStore
{
    private static readonly Dictionary<KeybindId, GameInputBinding> Defaults = new()
    {
        [KeybindId.MoveUp] = GameInputBinding.Keyboard(Keys.W),
        [KeybindId.MoveDown] = GameInputBinding.Keyboard(Keys.S),
        [KeybindId.MoveLeft] = GameInputBinding.Keyboard(Keys.A),
        [KeybindId.MoveRight] = GameInputBinding.Keyboard(Keys.D),
        [KeybindId.Dash] = GameInputBinding.Keyboard(Keys.LeftShift),
        [KeybindId.Attack] = GameInputBinding.FromMouse(MouseBindButton.Left),
        [KeybindId.ItemSlot1] = GameInputBinding.Keyboard(Keys.D1),
        [KeybindId.ItemSlot2] = GameInputBinding.Keyboard(Keys.D2),
        [KeybindId.PlaceItem] = GameInputBinding.Keyboard(Keys.G),
        [KeybindId.ThrowItem] = GameInputBinding.Keyboard(Keys.T),
    };

    private static readonly Dictionary<KeybindId, GameInputBinding> Current = new(Defaults);

    public static GameInputBinding GetBinding(KeybindId id) =>
        Current.TryGetValue(id, out GameInputBinding b) ? b : Defaults[id];

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

    /// <summary>Assigns <paramref name="newBinding"/> to <paramref name="targetId"/> and removes it from all other actions.</summary>
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

    public static void SetBinding(KeybindId id, GameInputBinding binding)
    {
        if (binding.IsUnbound)
        {
            Current[id] = default;
            return;
        }

        Current[id] = binding;
    }

    public static void ResetToDefaults()
    {
        foreach (var kv in Defaults)
            Current[kv.Key] = kv.Value;
    }

    public static string Label(KeybindId id) => id switch
    {
        KeybindId.MoveUp => "Move up",
        KeybindId.MoveDown => "Move down",
        KeybindId.MoveLeft => "Move left",
        KeybindId.MoveRight => "Move right",
        KeybindId.Dash => "Dash",
        KeybindId.Attack => "Attack",
        KeybindId.ItemSlot1 => "Item slot 1",
        KeybindId.ItemSlot2 => "Item slot 2",
        KeybindId.PlaceItem => "Place item",
        KeybindId.ThrowItem => "Throw item",
        _ => id.ToString(),
    };
}
