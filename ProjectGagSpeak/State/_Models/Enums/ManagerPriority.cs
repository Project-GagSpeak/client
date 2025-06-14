namespace GagSpeak.State;

/// <summary>
///     The Priority order of all Managers for the visual state.
/// </summary>
public enum ManagerPriority : byte
{
    Restraints = 0,
    Restrictions = 1,
    Gags = 2,
    CursedLoot = 3
}
