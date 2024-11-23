using Penumbra.Api.Enums;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to management of mods. </summary>
public interface IPenumbraApiMods
{
    /// <returns>A list of all installed mods. The first string is their directory name, the second string is their mod name.</returns>
    public Dictionary<string, string> GetModList();

    /// <summary> Try to unpack and install a valid mod file (.pmp, .ttmp, .ttmp2) as if installed manually. </summary>
    /// <param name="modFilePackagePath">The file that should be unpacked.</param>
    /// <returns>Success, MissingFile. Success does not indicate successful installing, just successful queueing for install.</returns>
    public PenumbraApiEc InstallMod(string modFilePackagePath);

    /// <summary> Try to reload an existing mod given by its <paramref name="modDirectory" /> name or <paramref name="modName" />.</summary>
    /// <remarks>Reload is the same as if triggered by button press and might delete the mod if it is not valid anymore.</remarks>
    /// <returns>ModMissing if the mod can not be found or Success</returns>
    public PenumbraApiEc ReloadMod(string modDirectory, string modName);

    /// <summary> Try to add a new mod inside the mod root directory.</summary>
    /// <remarks>Note that success does only imply a successful call, not a successful mod load.</remarks>
    /// <param name="modDirectory">The name (not full name) of the mod directory.</param>
    /// <returns>FileMissing if <paramref name="modDirectory" /> does not exist, InvalidArgument if the path leads outside the root directory, Success otherwise.</returns>
    public PenumbraApiEc AddMod(string modDirectory);

    /// <summary>Try to delete a mod  given by its <paramref name="modDirectory" /> name or <paramref name="modName" />.</summary>
    /// <remarks>Note that success does only imply a successful call, not successful deletion.</remarks>
    /// <returns>NothingDone if the mod can not be found, Success otherwise.</returns>
    public PenumbraApiEc DeleteMod(string modDirectory, string modName);

    /// <summary> Triggers whenever a mod is deleted. </summary>
    /// <returns>The base directory name of the deleted mod.</returns>
    public event Action<string>? ModDeleted;

    /// <summary> Triggers whenever a mod is deleted. </summary>
    /// <returns>The base directory name of the new mod.</returns>
    public event Action<string>? ModAdded;

    /// <summary> Triggers whenever a mods base name is changed from inside Penumbra. </summary>
    /// <returns>The previous base directory name of the mod and the new base directory name of the mod.</returns>
    public event Action<string, string>? ModMoved;

    /// <summary>
    /// Get the internal full filesystem path including search order for the specified mod
    /// given by its <paramref name="modDirectory" /> name or <paramref name="modName" />.
    /// </summary>
    /// <returns>On Success, the full path, a bool indicating whether the entire path is default (true) or manually set (false),
    /// and a bool indicating whether the sort order name ignoring the folder path is default (true) or manually set (false).
    /// Otherwise, returns ModMissing if the mod can not be found.</returns>
    public (PenumbraApiEc, string, bool, bool) GetModPath(string modDirectory, string modName);

    /// <summary>
    /// Set the internal search order and filesystem path of the specified mod
    /// given by its <paramref name="modDirectory" /> name or <paramref name="modName" />
    /// to the <paramref name="newPath" />.
    /// </summary>
    /// <returns>InvalidArgument if <paramref name="newPath" /> is empty, ModMissing if the mod can not be found,
    /// PathRenameFailed if <paramref name="newPath"/> could not be set or Success.</returns>
    public PenumbraApiEc SetModPath(string modDirectory, string modName, string newPath);

    /// <summary> Get the overall changed items of a single mod given by its <paramref name="modDirectory"/> name or <paramref name="modName"/>, regardless of settings. </summary>
    /// <returns> A possibly empty dictionary of affected items and known objects or null. </returns>
    public Dictionary<string, object?> GetChangedItems(string modDirectory, string modName);
}
