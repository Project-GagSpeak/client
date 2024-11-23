using Dalamud.Plugin;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class CreateTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, bool, (PenumbraApiEc, string)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(CreateTemporaryCollection)}";

    public new (PenumbraApiEc ErrorCode, string CollectionName) Invoke(string tag, string character, bool forceOverwrite)
        => base.Invoke(tag, character, forceOverwrite);
}

public sealed class RemoveTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<string, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(RemoveTemporaryCollection)}";

    public new PenumbraApiEc Invoke(string collectionName)
        => base.Invoke(collectionName);
}

public sealed class CreateNamedTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<string, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(CreateNamedTemporaryCollection)}";

    public new PenumbraApiEc Invoke(string collectionName)
        => base.Invoke(collectionName);
}

public sealed class RemoveTemporaryCollectionByName(IDalamudPluginInterface pi)
    : FuncSubscriber<string, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(RemoveTemporaryCollectionByName)}";

    public new PenumbraApiEc Invoke(string collectionName)
        => base.Invoke(collectionName);
}

public sealed class AssignTemporaryCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int, bool, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(AssignTemporaryCollection)}";

    public new PenumbraApiEc Invoke(string collectionName, int gameObjectIndex, bool force)
        => base.Invoke(collectionName, gameObjectIndex, force);
}

public sealed class AddTemporaryModAll(IDalamudPluginInterface pi)
    : FuncSubscriber<string, Dictionary<string, string>, string, int, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(AddTemporaryModAll)}";

    public new PenumbraApiEc Invoke(string tag, Dictionary<string, string> files, string meta, int priority = 0)
        => base.Invoke(tag, files, meta, priority);
}

public sealed class AddTemporaryMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, Dictionary<string, string>, string, int, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(AddTemporaryMod)}";

    public new PenumbraApiEc Invoke(string tag, string collectionName, Dictionary<string, string> files, string meta, int priority = 0)
        => base.Invoke(tag, collectionName, files, meta, priority);
}

public sealed class RemoveTemporaryModAll(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(RemoveTemporaryModAll)}";

    public new PenumbraApiEc Invoke(string tag, int priority = 0)
        => base.Invoke(tag, priority);
}

public sealed class RemoveTemporaryMod(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string, int, PenumbraApiEc>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(RemoveTemporaryMod)}";

    public new PenumbraApiEc Invoke(string tag, string collectionName, int priority = 0)
        => base.Invoke(tag, collectionName, priority);
}
