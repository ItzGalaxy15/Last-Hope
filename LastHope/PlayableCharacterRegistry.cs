using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Last_Hope.BaseModel;

namespace Last_Hope;

/// <summary>
/// Single place to register playable heroes: roster, character select, and spawning all read from here.
/// To add a character: add a <see cref="PlayerCharacterKind"/> value, then append one <see cref="Definition"/> to <see cref="Ordered"/>.
/// </summary>
public static class PlayableCharacterRegistry
{
    /// <summary>Locked order used by character select grid and roster list (left-to-roster / top-to-bottom).</summary>
    private static readonly Definition[] OrderedDefinitions =
    [
        new Definition
        {
            Kind = PlayerCharacterKind.Warrior,
            DisplayName = "Warrior",
            MaxHp = 100f,
            Attack = 20,
            Speed = 220f,
            CritPercent = 10f,
            CritDamageLabel = "2x (slash)",
            WeaponName = "Sword",
            Tagline = "Close range; melee slashes.",
            PortraitTextureKey = "WarriorSheet",
            PortraitTint = Color.White,
            PortraitFrameColumn = 0,
            PortraitFrameRow = 0,
            PortraitFrameSize = 32,
            Create = static p => new Warrior(p)
        },
        new Definition
        {
            Kind = PlayerCharacterKind.Archer,
            DisplayName = "Archer",
            MaxHp = 100f,
            Attack = 20,
            Speed = 220f,
            CritPercent = 100f,
            CritDamageLabel = "1.5x (arrows)",
            WeaponName = "Bow",
            Tagline = "Ranged shots; high crit chance.",
            PortraitTextureKey = "ArcherSheet",
            PortraitTint = Color.White,
            PortraitFrameColumn = 0,
            PortraitFrameRow = 0,
            PortraitFrameSize = 32,
            Create = static p => new Archer(p)
        }
    ];

    private static readonly Dictionary<PlayerCharacterKind, Definition> ByKind = BuildByKind();

    public sealed class Definition
    {
        public required PlayerCharacterKind Kind { get; init; }
        public required string DisplayName { get; init; }
        public float MaxHp { get; init; }
        public int Attack { get; init; }
        public float Speed { get; init; }
        public float CritPercent { get; init; }
        public required string CritDamageLabel { get; init; }
        public required string WeaponName { get; init; }
        public required string Tagline { get; init; }

        /// <summary>MonoGame content key for the portrait spritesheet; null skips portrait draw.</summary>
        public string? PortraitTextureKey { get; init; }

        public Color PortraitTint { get; init; } = Color.White;
        public int PortraitFrameColumn { get; init; }
        public int PortraitFrameRow { get; init; }
        public int PortraitFrameSize { get; init; } = 32;

        public required Func<Vector2, BasePlayer> Create { get; init; }
    }

    /// <summary>Playable characters in UI order (select screen + roster).</summary>
    public static IReadOnlyList<Definition> Ordered => OrderedDefinitions;

    public static int Count => OrderedDefinitions.Length;

    public static Definition OrderedAt(int index) => OrderedDefinitions[index];

    /// <summary>First registered character; used if save data references an unknown kind.</summary>
    public static PlayerCharacterKind DefaultKind => OrderedDefinitions[0].Kind;

    public static bool TryGet(PlayerCharacterKind kind, out Definition definition) => ByKind.TryGetValue(kind, out definition);

    public static Definition Get(PlayerCharacterKind kind)
    {
        if (!TryGet(kind, out Definition def))
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "No playable definition for this kind. Add it to PlayableCharacterRegistry.");
        return def;
    }

    public static BasePlayer Create(PlayerCharacterKind kind, Vector2 spawnPosition)
    {
        if (!TryGet(kind, out Definition def))
            def = OrderedDefinitions[0];
        return def.Create(spawnPosition);
    }

    private static Dictionary<PlayerCharacterKind, Definition> BuildByKind()
    {
        var map = new Dictionary<PlayerCharacterKind, Definition>();
        foreach (Definition d in OrderedDefinitions)
        {
            if (map.ContainsKey(d.Kind))
                throw new InvalidOperationException($"Duplicate playable character kind in registry: {d.Kind}");
            map[d.Kind] = d;
        }

        return map;
    }
}
