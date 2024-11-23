using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiMods.GetModList"/>
public sealed class GetModList(IDalamudPluginInterface pi)
    : FuncSubscriber<Dictionary<string, string>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetModList)}";

    /// <inheritdoc cref="IPenumbraApiMods.GetModList"/>
    public new Dictionary<string, string> Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Dictionary<string, string>> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, api.GetModList);
}

/// <inheritdoc cref="IPenumbraApiMods.InstallMod"/>
public sealed class InstallMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(InstallMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.InstallMod"/>
    public new PenumbraApiEc Invoke(string modFilePackagePath)
        => (PenumbraApiEc)base.Invoke(modFilePackagePath);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, a => (int)api.InstallMod(a));
}

/// <inheritdoc cref="IPenumbraApiMods.ReloadMod"/>
public sealed class ReloadMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ReloadMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.ReloadMod"/>
    public new PenumbraApiEc Invoke(string modDirectory, string modName = "")
        => (PenumbraApiEc)base.Invoke(modDirectory, modName);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (a, b) => (int)api.ReloadMod(a, b));
}

/// <inheritdoc cref="IPenumbraApiMods.AddMod"/>
public sealed class AddMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(AddMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.AddMod"/>
    public new PenumbraApiEc Invoke(string modDirectory)
        => (PenumbraApiEc)base.Invoke(modDirectory);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, a => (int)api.AddMod(a));
}

/// <inheritdoc cref="IPenumbraApiMods.DeleteMod"/>
public sealed class DeleteMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(DeleteMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.DeleteMod"/>
    public new PenumbraApiEc Invoke(string modDirectory, string modName = "")
        => (PenumbraApiEc)base.Invoke(modDirectory, modName);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (a, b) => (int)api.DeleteMod(a, b));
}

/// <inheritdoc cref="IPenumbraApiMods.ModDeleted" />
public static class ModDeleted
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ModDeleted)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string> Subscriber(IDalamudPluginInterface pi, params Action<string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (t => api.ModDeleted += t, t => api.ModDeleted -= t));
}

/// <inheritdoc cref="IPenumbraApiMods.ModAdded" />
public static class ModAdded
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ModAdded)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string> Subscriber(IDalamudPluginInterface pi, params Action<string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (t => api.ModAdded += t, t => api.ModAdded -= t));
}

/// <inheritdoc cref="IPenumbraApiMods.ModMoved" />
public static class ModMoved
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ModMoved)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string, string> Subscriber(IDalamudPluginInterface pi, params Action<string, string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string, string> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (t => api.ModMoved += t, t => api.ModMoved -= t));
}

/// <inheritdoc cref="IPenumbraApiMods.GetModPath"/>
public sealed class GetModPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, (int, string, bool, bool)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetModPath)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.GetModPath"/>
    public new (PenumbraApiEc, string FullPath, bool FullDefault, bool NameDefault) Invoke(string modDirectory, string modName = "")
    {
        var (ret, fullPath, fullDefault, nameDefault) = base.Invoke(modDirectory, modName);
        return ((PenumbraApiEc)ret, fullPath, fullDefault, nameDefault);
    }

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, (int, string, bool, bool)> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (a, b) =>
        {
            var (ret, fullPath, fullDefault, nameDefault) = api.GetModPath(a, b);
            return ((int)ret, fullPath, fullDefault, nameDefault);
        });
}

/// <inheritdoc cref="IPenumbraApiMods.SetModPath"/>
public sealed class SetModPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(SetModPath)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.SetModPath"/>
    public new PenumbraApiEc Invoke(string modDirectory, string newPath, string modName = "")
        => (PenumbraApiEc)base.Invoke(modDirectory, modName, newPath);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, (a, b, c) => (int)api.SetModPath(a, b, c));
}

/// <inheritdoc cref="IPenumbraApiMods.GetChangedItems"/>
public sealed class GetChangedItems(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, Dictionary<string, object?>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetChangedItems)}.V5";

    /// <inheritdoc cref="IPenumbraApiMods.GetChangedItems"/>
    public new Dictionary<string, object?> Invoke(string modDirectory, string modName)
        => base.Invoke(modDirectory, modName);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string, Dictionary<string, object?>> Provider(IDalamudPluginInterface pi, IPenumbraApiMods api)
        => new(pi, Label, api.GetChangedItems);
}
