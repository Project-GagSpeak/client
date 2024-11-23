using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to the management of mod settings. </summary>
public interface IPenumbraApiModSettings
{
    /// <summary>
    /// Obtain the potential settings of a mod given by its <paramref name="modDirectory" /> name or <paramref name="modName" />.
    /// </summary>
    /// <returns>A dictionary of group names to lists of option names and the group type. Null if the mod could not be found.</returns>
    public AvailableModSettings? GetAvailableModSettings(string modDirectory, string modName);

    /// <summary>
    /// Obtain the enabled state, the priority, the settings of a mod given by its <paramref name="modDirectory" /> name or <paramref name="modName" /> in the specified collection.
    /// </summary>
    /// <param name="collectionId">Specify the collection.</param>
    /// <param name="modDirectory">Specify the mod via its directory name.</param>
    /// <param name="modName">Specify the mod via its (non-unique) display name.</param>
    /// <param name="ignoreInheritance">Whether the settings need to be from the given collection or can be inherited from any other by it. (True: given collection only)</param>
    /// <returns>ModMissing, CollectionMissing or Success. <para />
    /// On Success, a tuple of Enabled State, Priority, a dictionary of option group names and lists of enabled option names and a bool whether the settings are inherited (true) or not.</returns>
    public (PenumbraApiEc, (bool, int, Dictionary<string, List<string>>, bool)?) GetCurrentModSettings(Guid collectionId,
        string modDirectory, string modName, bool ignoreInheritance);

    /// <summary> Try to set the inheritance state of a mod in a collection. </summary>
    /// <returns>ModMissing, CollectionMissing, InvalidArgument (GUID is nil), NothingChanged or Success.</returns>
    public PenumbraApiEc TryInheritMod(Guid collectionId, string modDirectory, string modName, bool inherit);

    /// <summary> Try to set the enabled state of a mod in a collection. </summary>
    /// <returns>ModMissing, CollectionMissing, InvalidArgument (GUID is nil), NothingChanged or Success.</returns>
    public PenumbraApiEc TrySetMod(Guid collectionId, string modDirectory, string modName, bool enabled);

    /// <summary> Try to set the priority of a mod in a collection. </summary>
    /// <returns>ModMissing, CollectionMissing, InvalidArgument (GUID is nil), NothingChanged or Success.</returns>
    public PenumbraApiEc TrySetModPriority(Guid collectionId, string modDirectory, string modName, int priority);

    /// <summary> Try to set a specific option group of a mod in the given collection to a specific value. </summary>
    /// <remarks>Removes inheritance. Single Selection groups should provide a single option, Multi Selection can provide multiple.
    /// If any setting can not be found, it will not change anything.</remarks>
    /// <returns>ModMissing, CollectionMissing, OptionGroupMissing, SettingMissing, InvalidArgument (GUID is nil), NothingChanged or Success.</returns>
    public PenumbraApiEc TrySetModSetting(Guid collectionId, string modDirectory, string modName, string optionGroupName, string optionName);

    /// <inheritdoc cref="TrySetModSetting"/>
    public PenumbraApiEc TrySetModSettings(Guid collectionId, string modDirectory, string modName, string optionGroupName,
        IReadOnlyList<string> optionNames);

    /// <summary> This event gets fired when any setting in any collection changes. </summary>
    /// <returns><inheritdoc cref="ModSettingChangedDelegate" /></returns>
    public event ModSettingChangedDelegate? ModSettingChanged;

    /// <summary>
    /// Copy all current settings for a mod to another mod.
    /// </summary>
    /// <param name="collectionId">Specify the collection to work in, leave null to do it in all collections.</param>
    /// <param name="modDirectoryFrom">Specify the mod to take the settings from via its directory name.</param>
    /// <param name="modDirectoryTo">Specify the mod to put the settings on via its directory name. If the mod does not exist, it will be added as unused settings.</param>
    /// <returns>CollectionMissing if collectionName is not empty but does not exist or Success.</returns>
    /// <remarks>If the target mod exists, the settings will be fixed before being applied. If the source mod does not exist, it will use unused settings if available and remove existing settings otherwise.</remarks>
    public PenumbraApiEc CopyModSettings(Guid? collectionId, string modDirectoryFrom, string modDirectoryTo);
}
