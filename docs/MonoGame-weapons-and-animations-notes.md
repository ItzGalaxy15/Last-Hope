# MonoGame: weapons on characters & animation scope (saved notes)

Reference for a future pass on weapon swapping / “Unity-style” attachment in this project (2D pixel art today).

## Unity vs MonoGame (short)

- Unity: rigged model + bones + sockets + (optional) IK; editor wires a lot up.
- MonoGame: you get rendering + math; **no** built-in humanoid rig, sockets, or animation graph. Same *ideas* are possible—you implement or use a library.

## Implementation paths

### A) 2D sprites (closest to current Warrior / Archer style)

- **Weapon** = separate texture (or sheet) + **data**: offset from body, scale, flip, optional per-frame rect.
- **Anchors** = named logical points (`Hand`, `Back`) implemented as `Vector2` offsets + facing, not bones.
- **State machine** (idle / walk / attack) picks body frames + which weapon sheet/offset to draw.

**Per-weapon full body animations?** Usually **not** required if attacks share a silhouette (reuse one swing timing, swap weapon art + hitbox + stats). Use **2–3 attack archetypes** (slash, thrust, shoot) and map weapons to them. Unique full clips only when the pose must look very different (art cost).

### B) 2D skeletal (Spine, Spriter, DragonBones, …)

- Bones + attachments; weapon follows **hand bone**.
- Often **one** swing clip + swap weapon skin/mesh on the attachment.
- Needs a runtime compatible with MonoGame / C#.

### C) 3D rigged (closest to Unity default)

- Skinned mesh + animation; weapon matrix = **bone matrix** × local weapon transform each frame.
- Needs pipeline (e.g. Assimp) or middleware + skinning code.

## “Do I need different animations for every weapon?”

**No, not by default.**

| Approach | Typical use |
|----------|-------------|
| Same motion, different weapon sprite + hitbox + numbers | Most common |
| Few archetype clips (melee vs ranged vs heavy) | When timing/poses differ a lot |
| Full unique clip per weapon | When you want very distinct silhouettes / marketing clarity |

Different **attack types** (bow draw vs sword slash) are often **different states**, not “one animation per weapon SKU.”

## Practical next step for *this* codebase

- Introduce something like `WeaponVisualProfile` (texture key, offsets, optional `AttackArchetype`).
- Centralize “where weapon is drawn relative to body + facing” instead of hardcoding per class only.
- Keep `BaseWeapon` / damage / behaviour separate from visuals if you want swappable gear later.

---

*Saved from design discussion; adjust to match whatever art pipeline you choose.*
