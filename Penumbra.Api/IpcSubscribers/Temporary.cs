using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiTemporary.CreateTemporaryCollection"/>
public sealed class CreateTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<string, Guid>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(CreateTemporaryCollection)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.CreateTemporaryCollection"/>
    public new Guid Invoke(string name = "")
        => base.Invoke(name);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, Guid> Provider(IDalamudPluginInterface pi, IPenumbraApiTemporary api)
        => new(pi, Label, api.CreateTemporaryCollection);
}

/// <inheritdoc cref="IPenumbraApiTemporary.DeleteTemporaryCollection"/>
public sealed class DeleteTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(DeleteTemporaryCollection)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.DeleteTemporaryCollection"/>
    public new PenumbraApiEc Invoke(Guid collectionId)
        => (PenumbraApiEc)base.Invoke(collectionId);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, int> Provider(IDalamudPluginInterface pi, IPenumbraApiTemporary api)
        => new(pi, Label, g => (int)api.DeleteTemporaryCollection(g));
}

/// <inheritdoc cref="IPenumbraApiTemporary.AssignTemporaryCollection"/>
public sealed class AssignTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, int, bool, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(AssignTemporaryCollection)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.AssignTemporaryCollection"/>
    public new PenumbraApiEc Invoke(Guid collectionId, int actorIndex, bool forceAssignment = true)
        => (PenumbraApiEc)base.Invoke(collectionId, actorIndex, forceAssignment);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, int, bool, int> Provider(IDalamudPluginInterface pi, IPenumbraApiTemporary api)
        => new(pi, Label, (a, b, c) => (int)api.AssignTemporaryCollection(a, b, c));
}

/// <inheritdoc cref="IPenumbraApiTemporary.AddTemporaryModAll"/>
public sealed class AddTemporaryModAll(IDalamudPluginInterface pi)
    : FuncSubscriber<string, Dictionary<string, string>, string, int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(AddTemporaryModAll)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.AddTemporaryModAll"/>
    public new PenumbraApiEc Invoke(string tag, Dictionary<string, string> paths, string manipString, int priority)
        => (PenumbraApiEc)base.Invoke(tag, paths, manipString, priority);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, Dictionary<string, string>, string, int, int> Provider(IDalamudPluginInterface pi,
        IPenumbraApiTemporary api)
        => new(pi, Label, (a, b, c, d) => (int)api.AddTemporaryModAll(a, b, c, d));
}

/// <inheritdoc cref="IPenumbraApiTemporary.AddTemporaryMod"/>
public sealed class AddTemporaryMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, Guid, Dictionary<string, string>, string, int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(AddTemporaryMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.AddTemporaryMod"/>
    public new PenumbraApiEc Invoke(string tag, Guid collectionId, Dictionary<string, string> paths, string manipString, int priority)
        => (PenumbraApiEc)base.Invoke(tag, collectionId, paths, manipString, priority);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, Guid, Dictionary<string, string>, string, int, int> Provider(IDalamudPluginInterface pi,
        IPenumbraApiTemporary api)
        => new(pi, Label, (a, b, c, d, e) => (int)api.AddTemporaryMod(a, b, c, d, e));
}

/// <inheritdoc cref="IPenumbraApiTemporary.RemoveTemporaryModAll"/>
public sealed class RemoveTemporaryModAll(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(RemoveTemporaryModAll)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.RemoveTemporaryModAll"/>
    public new PenumbraApiEc Invoke(string tag, int priority)
        => (PenumbraApiEc)base.Invoke(tag, priority);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, int, int> Provider(IDalamudPluginInterface pi,
        IPenumbraApiTemporary api)
        => new(pi, Label, (a, b) => (int)api.RemoveTemporaryModAll(a, b));
}

/// <inheritdoc cref="IPenumbraApiTemporary.RemoveTemporaryMod"/>
public sealed class RemoveTemporaryMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, Guid, int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(RemoveTemporaryMod)}.V5";

    /// <inheritdoc cref="IPenumbraApiTemporary.RemoveTemporaryMod"/>
    public new PenumbraApiEc Invoke(string tag, Guid collectionId, int priority)
        => (PenumbraApiEc)base.Invoke(tag, collectionId, priority);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, Guid, int, int> Provider(IDalamudPluginInterface pi, IPenumbraApiTemporary api)
        => new(pi, Label, (a, b, c) => (int)api.RemoveTemporaryMod(a, b, c));
}
