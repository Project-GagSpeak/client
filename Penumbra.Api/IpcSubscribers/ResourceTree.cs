using System.Linq;
using Dalamud.Plugin;
using Newtonsoft.Json.Linq;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiResourceTree.GetGameObjectResourcePaths"/>
public sealed class GetGameObjectResourcePaths(IDalamudPluginInterface pi)
    : FuncSubscriber<ushort[], Dictionary<string, HashSet<string>>?[]>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetGameObjectResourcePaths)}.V5";

    /// <inheritdoc cref="IPenumbraApiResourceTree.GetGameObjectResourcePaths"/>
    public new Dictionary<string, HashSet<string>>?[] Invoke(params ushort[] gameObjectIndices)
        => base.Invoke(gameObjectIndices);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<ushort[], Dictionary<string, HashSet<string>>?[]> Provider(IDalamudPluginInterface pi,
        IPenumbraApiResourceTree api)
        => new(pi, Label, api.GetGameObjectResourcePaths);
}

/// <inheritdoc cref="IPenumbraApiResourceTree.GetPlayerResourcePaths"/>
public sealed class GetPlayerResourcePaths(IDalamudPluginInterface pi)
    : FuncSubscriber<Dictionary<ushort, Dictionary<string, HashSet<string>>>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetPlayerResourcePaths)}.V5";

    /// <inheritdoc cref="IPenumbraApiResourceTree.GetPlayerResourcePaths"/>
    public new Dictionary<ushort, Dictionary<string, HashSet<string>>> Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Dictionary<ushort, Dictionary<string, HashSet<string>>>> Provider(IDalamudPluginInterface pi,
        IPenumbraApiResourceTree api)
        => new(pi, Label, api.GetPlayerResourcePaths);
}

/// <inheritdoc cref="IPenumbraApiResourceTree.GetGameObjectResourcesOfType"/>
public sealed class GetGameObjectResourcesOfType(IDalamudPluginInterface pi)
    : FuncSubscriber<uint, bool, ushort[], IReadOnlyDictionary<nint, (string, string, uint)>?[]>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetGameObjectResourcesOfType)}.V5";

    /// <inheritdoc cref="IPenumbraApiResourceTree.GetGameObjectResourcesOfType"/>
    public IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>?[] Invoke(ResourceType type, bool withUiData = false,
        params ushort[] gameObjectIndices)
        => Array.ConvertAll(Invoke((uint)type, withUiData, gameObjectIndices),
            d => (IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>?)GameResourceDict.Create(d));

    /// <summary> Create a provider. </summary>
    public static FuncProvider<uint, bool, ushort[], IReadOnlyDictionary<nint, (string, string, uint)>?[]> Provider(IDalamudPluginInterface pi,
        IPenumbraApiResourceTree api)
        => new(pi, Label,
            (a, b, c) => Array.ConvertAll(api.GetGameObjectResourcesOfType((ResourceType)a, b, c), d => d?.Original));
}

/// <inheritdoc cref="IPenumbraApiResourceTree.GetPlayerResourcesOfType"/>
public sealed class GetPlayerResourcesOfType(IDalamudPluginInterface pi)
    : FuncSubscriber<uint, bool, Dictionary<ushort, IReadOnlyDictionary<nint, (string, string, uint)>>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetPlayerResourcesOfType)}.V5";

    /// <inheritdoc cref="IPenumbraApiResourceTree.GetPlayerResourcesOfType"/>
    public Dictionary<ushort, IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>> Invoke(ResourceType type, bool withUiData = false)
        => Invoke((uint)type, withUiData)
            .ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>)new GameResourceDict(kvp.Value));

    /// <summary> Create a provider. </summary>
    public static FuncProvider<uint, bool, Dictionary<ushort, IReadOnlyDictionary<nint, (string, string, uint)>>> Provider(
        IDalamudPluginInterface pi,
        IPenumbraApiResourceTree api)
        => new(pi, Label,
            (a, b) => api.GetPlayerResourcesOfType((ResourceType)a, b)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Original));
}

/// <inheritdoc cref="IPenumbraApiResourceTree.GetGameObjectResourceTrees"/>
public sealed class GetGameObjectResourceTrees(IDalamudPluginInterface pi)
    : FuncSubscriber<bool, ushort[], JObject?[]>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetGameObjectResourceTrees)}.V5";

    /// <inheritdoc cref="IPenumbraApiResourceTree.GetGameObjectResourceTrees"/>
    public new ResourceTreeDto?[] Invoke(bool withUiData = false, params ushort[] gameObjectIndices)
        => Array.ConvertAll(base.Invoke(withUiData, gameObjectIndices), o => o?.ToObject<ResourceTreeDto>());

    /// <summary> Create a provider. </summary>
    public static FuncProvider<bool, ushort[], JObject?[]> Provider(IDalamudPluginInterface pi,
        IPenumbraApiResourceTree api)
        => new(pi, Label, api.GetGameObjectResourceTrees);
}

/// <inheritdoc cref="IPenumbraApiResourceTree.GetPlayerResourceTrees"/>
public sealed class GetPlayerResourceTrees(IDalamudPluginInterface pi)
    : FuncSubscriber<bool, Dictionary<ushort, JObject>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetPlayerResourceTrees)}.V5";

    /// <inheritdoc cref="IPenumbraApiResourceTree.GetPlayerResourceTrees"/>
    public new Dictionary<ushort, ResourceTreeDto> Invoke(bool withUiData = false)
        => base.Invoke(withUiData).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToObject<ResourceTreeDto>()!);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<bool, Dictionary<ushort, JObject>> Provider(IDalamudPluginInterface pi, IPenumbraApiResourceTree api)
        => new(pi, Label, api.GetPlayerResourceTrees);
}
