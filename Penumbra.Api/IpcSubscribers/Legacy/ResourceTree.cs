using Dalamud.Plugin;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using ResourceType = Penumbra.Api.Enums.ResourceType;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class GetGameObjectResourcePaths(IDalamudPluginInterface pi)
    : FuncSubscriber<ushort[], IReadOnlyDictionary<string, string[]>?[]>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetGameObjectResourcePaths)}";

    public new IReadOnlyDictionary<string, string[]>?[] Invoke(params ushort[] objectIndices)
        => base.Invoke(objectIndices);
}

public sealed class GetPlayerResourcePaths(IDalamudPluginInterface pi)
    : FuncSubscriber<IReadOnlyDictionary<ushort, IReadOnlyDictionary<string, string[]>>>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetPlayerResourcePaths)}";

    public new IReadOnlyDictionary<ushort, IReadOnlyDictionary<string, string[]>> Invoke()
        => base.Invoke();
}

public sealed class GetGameObjectResourcesOfType(IDalamudPluginInterface pi)
    : FuncSubscriber<ResourceType, bool, ushort[], IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>?[]>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetGameObjectResourcesOfType)}";

    public new IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>?[] Invoke(ResourceType type, bool withUiData = false,
        params ushort[] indices)
        => base.Invoke(type, withUiData, indices);
}

public sealed class GetPlayerResourcesOfType(IDalamudPluginInterface pi)
    : FuncSubscriber<ResourceType, bool, IReadOnlyDictionary<ushort, IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>>>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetPlayerResourcesOfType)}";

    public new IReadOnlyDictionary<ushort, IReadOnlyDictionary<nint, (string, string, ChangedItemIcon)>> Invoke(ResourceType type,
        bool withUiData = false)
        => base.Invoke(type, withUiData);
}

public sealed class GetGameObjectResourceTrees(IDalamudPluginInterface pi)
    : FuncSubscriber<bool, ushort[], ResourceTree?[]>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetGameObjectResourceTrees)}";

    public new ResourceTree?[] Invoke(bool withUiData = false, params ushort[] indices)
        => base.Invoke(withUiData, indices);
}

public sealed class GetPlayerResourceTrees(IDalamudPluginInterface pi)
    : FuncSubscriber<bool, IReadOnlyDictionary<ushort, ResourceTree>>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetPlayerResourceTrees)}";

    public new IReadOnlyDictionary<ushort, ResourceTree> Invoke(bool withUiData = false)
        => base.Invoke(withUiData);
}

public record ResourceTree
{
    public required string             Name     { get; init; }
    public required ushort             RaceCode { get; init; }
    public required List<ResourceNode> Nodes    { get; init; }
}

public record ResourceNode
{
    public required ResourceType       Type           { get; init; }
    public required ChangedItemIcon    Icon           { get; init; }
    public required string?            Name           { get; init; }
    public required string?            GamePath       { get; init; }
    public required string             ActualPath     { get; init; }
    public required nint               ObjectAddress  { get; init; }
    public required nint               ResourceHandle { get; init; }
    public required List<ResourceNode> Children       { get; init; }
}
