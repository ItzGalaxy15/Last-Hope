namespace Last_Hope;

/// <summary>
/// Identifies a playable hero. Every value used in a run must have a matching entry in
/// <see cref="PlayableCharacterRegistry"/> (see <c>OrderedDefinitions</c> there).
/// </summary>
public enum PlayerCharacterKind
{
    Warrior,
    Archer
}