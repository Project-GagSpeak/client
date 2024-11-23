namespace Penumbra.Api.Enums;

/// <summary>
/// The selection type for mod option groups.
/// </summary>
public enum GroupType
{
    /// <summary>
    /// Exactly one option of this group has to be selected (if any exist).
    /// </summary>
    Single,

    /// <summary>
    /// Any number of options in this group can be toggled on or off at the same time.
    /// Limits the number of options in a single group to 32 at the most.
    /// </summary>
    Multi,

    /// <summary>
    /// Any number of options in this group can be toggled on or off at the same time.
    /// Affects a single IMC entry, to manipulate different parts of a model in a user-facing way.
    /// </summary>
    Imc,
}
