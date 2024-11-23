using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

using CurrentSettings = ValueTuple<PenumbraApiEc, (bool, int, IDictionary<string, IList<string>>, bool)?>;

public sealed class GetAvailableModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, IDictionary<string, (IList<string>, GroupType)>?>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetAvailableModSettings)}";

    public new IDictionary<string, (IList<string>, GroupType)>? Invoke(string modDirectory, string modName = "")
        => base.Invoke(modDirectory, modName);
}

public sealed class GetCurrentModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, bool, CurrentSettings>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetCurrentModSettings)}";

    public new CurrentSettings Invoke(string collectionName, string modDirectory, string modName = "", bool ignoreInheritance = false)
        => base.Invoke(collectionName, modDirectory, modName, ignoreInheritance);
}

public sealed class TryInheritMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, bool, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(TryInheritMod)}";

    public PenumbraApiEc Invoke(string collectionName, string modDirectory, bool inherit, string modName = "")
        => Invoke(collectionName, modDirectory, modName, inherit);
}

public sealed class TrySetMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, bool, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(TrySetMod)}";

    public PenumbraApiEc Invoke(string collectionName, string modDirectory, bool enabled, string modName = "")
        => Invoke(collectionName, modDirectory, modName, enabled);
}

public sealed class TrySetModPriority(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, int, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(TrySetModPriority)}";

    public PenumbraApiEc Invoke(string collectionName, string modDirectory, int priority, string modName = "")
        => Invoke(collectionName, modDirectory, modName, priority);
}

public sealed class TrySetModSetting(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, string, string, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(TrySetModSetting)}";

    public new PenumbraApiEc Invoke(string collectionName, string modDirectory, string groupName, string setting, string modName = "")
        => base.Invoke(collectionName, modDirectory, modName, groupName, setting);
}

public sealed class TrySetModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, string, IReadOnlyList<string>, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(TrySetModSettings)}";

    public PenumbraApiEc Invoke(string collectionName, string modDirectory, string groupName, IReadOnlyList<string> settings,
        string modName = "")
        => Invoke(collectionName, modDirectory, modName, groupName, settings);
}

public static class ModSettingChanged
{
    public const string Label = $"Penumbra.{nameof(ModSettingChanged)}.V5";

    public static EventSubscriber<ModSettingChange, string, string, bool> Subscriber(IDalamudPluginInterface pi,
        params Action<ModSettingChange, string, string, bool>[] actions)
        => new(pi, Label, actions);
}

public sealed class CopyModSettings(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(CopyModSettings)}";

    public new PenumbraApiEc Invoke(string collectionName, string modDirectoryFrom, string modDirectoryTo)
        => base.Invoke(collectionName, modDirectoryFrom, modDirectoryTo);
}
