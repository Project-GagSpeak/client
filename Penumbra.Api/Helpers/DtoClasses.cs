using System.Runtime.CompilerServices;
using Penumbra.Api.Enums;

namespace Penumbra.Api.Helpers;

/// <summary> Wrapper dictionary. </summary>
public sealed class GameResourceDict(IReadOnlyDictionary<nint, (string, string, uint)> dict)
    : ConvertingDict<nint, (string, string, uint), (string, string, ChangedItemIcon)>(dict)
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected override (string, string, ChangedItemIcon) ConvertValue(in (string, string, uint) from)
        => (from.Item1, from.Item2, (ChangedItemIcon)from.Item3);

    /// <summary> Create dictionary or null. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static GameResourceDict? Create(IReadOnlyDictionary<nint, (string, string, uint)>? dict)
        => dict == null ? null : new GameResourceDict(dict);
}

/// <summary> Wrapper dictionary. </summary>
public sealed class AvailableModSettings(IReadOnlyDictionary<string, (string[], int)> dict)
    : ConvertingDict<string, (string[], int), (string[], GroupType)>(dict)
{
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    protected override (string[], GroupType) ConvertValue(in (string[], int) from)
        => (from.Item1, (GroupType)from.Item2);

    /// <summary> Create dictionary or null. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static AvailableModSettings? Create(IReadOnlyDictionary<string, (string[], int)>? dict)
        => dict == null ? null : new AvailableModSettings(dict);
}

public record ResourceNodeDto
{
    public required ResourceType          Type           { get; init; }
    public required ChangedItemIcon       Icon           { get; init; }
    public required string?               Name           { get; init; }
    public required string?               GamePath       { get; init; }
    public required string                ActualPath     { get; init; }
    public required nint                  ObjectAddress  { get; init; }
    public required nint                  ResourceHandle { get; init; }
    public required List<ResourceNodeDto> Children       { get; init; }
}

public record ResourceTreeDto
{
    public required string                Name     { get; init; }
    public required ushort                RaceCode { get; init; }
    public required List<ResourceNodeDto> Nodes    { get; init; }
}
