using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

using CurrentSettingsBase = ValueTuple<int, (bool, int, Dictionary<string, List<string>>, bool)?>;
using CurrentSettings = ValueTuple<PenumbraApiEc, (bool, int, Dictionary<string, List<string>>, bool)?>;

/// <inheritdoc cref="IPenumbraApiModSettings.GetAvailableModSettings"/>
public sealed class GetAvailableModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, IReadOnlyDictionary<string, (string[], int)>?>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetAvailableModSettings)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.GetAvailableModSettings"/>
    public new IReadOnlyDictionary<string, (string[], GroupType)>? Invoke(string modDirectory, string modName = "")
        => AvailableModSettings.Create(base.Invoke(modDirectory, modName));

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, IReadOnlyDictionary<string, (string[], int)>?> Provider(IDalamudPluginInterface pi,
        IPenumbraApiModSettings api)
        => new(pi, Label, (a, b) => api.GetAvailableModSettings(a, b)?.Original);
}

/// <inheritdoc cref="IPenumbraApiModSettings.GetCurrentModSettings"/>
public sealed class GetCurrentModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, string, string, bool, CurrentSettingsBase>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetCurrentModSettings)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.GetCurrentModSettings"/>
    public new CurrentSettings Invoke(Guid collectionId, string modDirectory, string modName = "", bool ignoreInheritance = false)
    {
        var (ret, t) = base.Invoke(collectionId, modDirectory, modName, ignoreInheritance);
        return ((PenumbraApiEc)ret, t);
    }

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, string, bool, CurrentSettingsBase> Provider(IDalamudPluginInterface pi,
        IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c, d) =>
        {
            var (ret, t) = api.GetCurrentModSettings(a, b, c, d);
            return ((int)ret, t);
        });
}

/// <inheritdoc cref="IPenumbraApiModSettings.TryInheritMod"/>
public sealed class TryInheritMod(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, string, string, bool, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(TryInheritMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.TryInheritMod"/>
    public PenumbraApiEc Invoke(Guid collectionId, string modDirectory, bool inherit, string modName = "")
        => (PenumbraApiEc)Invoke(collectionId, modDirectory, modName, inherit);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, string, bool, int> Provider(IDalamudPluginInterface pi, IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c, d) => (int)api.TryInheritMod(a, b, c, d));
}

/// <inheritdoc cref="IPenumbraApiModSettings.TrySetMod"/>
public sealed class TrySetMod(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, string, string, bool, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(TrySetMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.TrySetMod"/>
    public PenumbraApiEc Invoke(Guid collectionId, string modDirectory, bool inherit, string modName = "")
        => (PenumbraApiEc)Invoke(collectionId, modDirectory, modName, inherit);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, string, bool, int> Provider(IDalamudPluginInterface pi, IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c, d) => (int)api.TrySetMod(a, b, c, d));
}

/// <inheritdoc cref="IPenumbraApiModSettings.TrySetModPriority"/>
public sealed class TrySetModPriority(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, string, string, int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(TrySetModPriority)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.TrySetModPriority"/>
    public PenumbraApiEc Invoke(Guid collectionId, string modDirectory, int priority, string modName = "")
        => (PenumbraApiEc)Invoke(collectionId, modDirectory, modName, priority);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, string, int, int> Provider(IDalamudPluginInterface pi, IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c, d) => (int)api.TrySetModPriority(a, b, c, d));
}

/// <inheritdoc cref="IPenumbraApiModSettings.TrySetModSetting"/>
public sealed class TrySetModSetting(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, string, string, string, string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(TrySetModSetting)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.TrySetModSetting"/>
    public new PenumbraApiEc Invoke(Guid collectionId, string modDirectory, string optionGroupName, string optionName, string modName = "")
        => (PenumbraApiEc)base.Invoke(collectionId, modDirectory, modName, optionGroupName, optionName);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, string, string, string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c, d, e) => (int)api.TrySetModSetting(a, b, c, d, e));
}

/// <inheritdoc cref="IPenumbraApiModSettings.TrySetModSettings"/>
public sealed class TrySetModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, string, string, string, IReadOnlyList<string>, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(TrySetModSettings)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.TrySetModSettings"/>
    public PenumbraApiEc Invoke(Guid collectionId, string modDirectory, string optionGroupName,
        IReadOnlyList<string> optionNames, string modName = "")
        => (PenumbraApiEc)Invoke(collectionId, modDirectory, modName, optionGroupName, optionNames);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, string, string, string, IReadOnlyList<string>, int> Provider(IDalamudPluginInterface pi,
        IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c, d, e) => (int)api.TrySetModSettings(a, b, c, d, e));
}

/// <inheritdoc cref="IPenumbraApiModSettings.ModSettingChanged" />
public static class ModSettingChanged
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ModSettingChanged)}.V5";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<ModSettingChange, Guid, string, bool> Subscriber(IDalamudPluginInterface pi,
        params Action<ModSettingChange, Guid, string, bool>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<ModSettingChange, Guid, string, bool> Provider(IDalamudPluginInterface pi, IPenumbraApiModSettings api)
        => new(pi, Label, t => api.ModSettingChanged += t.Invoke, t => api.ModSettingChanged -= t.Invoke);
}

/// <inheritdoc cref="IPenumbraApiModSettings.CopyModSettings"/>
public sealed class CopyModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid?, string, string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(CopyModSettings)}.V5";

    /// <inheritdoc cref="IPenumbraApiModSettings.CopyModSettings"/>
    public new PenumbraApiEc Invoke(Guid? collectionId, string modDirectoryFrom, string modDirectoryTo)
        => (PenumbraApiEc)base.Invoke(collectionId, modDirectoryFrom, modDirectoryTo);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid?, string, string, int> Provider(IDalamudPluginInterface pi,
        IPenumbraApiModSettings api)
        => new(pi, Label, (a, b, c) => (int)api.CopyModSettings(a, b, c));
}
