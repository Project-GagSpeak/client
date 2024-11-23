namespace Penumbra.Api.Enums;

/// <summary>
/// Describes the way a mod can change its settings.
/// </summary>
public enum ModSettingChange
{
    /// <summary> It was set to inherit from other collections or not to inherit anymore. </summary>
    Inheritance,

    /// <summary> It was enabled or disabled. </summary>
    EnableState,

    /// <summary> Its priority was changed. </summary>
    Priority,

    /// <summary> A specific setting for an option group was changed. </summary>
    Setting,

    /// <summary> Multiple mods were set to inherit from other collections or not inherit anymore at once. </summary>
    MultiInheritance,

    /// <summary> Multiple mods were enabled or disabled at once. </summary>
    MultiEnableState,

    /// <summary> A temporary mod was enabled or disabled. </summary>
    TemporaryMod,

    /// <summary> A mod was edited. Only invoked on edits affecting the current players collection and for that for now. </summary>
    Edited,
}
