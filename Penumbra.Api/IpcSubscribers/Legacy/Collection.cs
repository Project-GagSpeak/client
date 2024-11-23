using Dalamud.Plugin;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Penumbra.Api.IpcSubscribers.Legacy;

public sealed class GetCollections(IDalamudPluginInterface pi)
    : FuncSubscriber<IList<string>>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetCollections)}";

    public new IList<string> Invoke()
        => base.Invoke();
}

public sealed class GetCurrentCollectionName(IDalamudPluginInterface pi)
    : FuncSubscriber<string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetCurrentCollectionName)}";

    public new string Invoke()
        => base.Invoke();
}

public sealed class GetDefaultCollectionName(IDalamudPluginInterface pi)
    : FuncSubscriber<string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetDefaultCollectionName)}";

    public new string Invoke()
        => base.Invoke();
}

public sealed class GetInterfaceCollectionName(IDalamudPluginInterface pi)
    : FuncSubscriber<string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetInterfaceCollectionName)}";

    public new string Invoke()
        => base.Invoke();
}

public sealed class GetCharacterCollectionName(IDalamudPluginInterface pi)
    : FuncSubscriber<string, (string, bool)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetCharacterCollectionName)}";

    public new (string, bool) Invoke(string characterName)
        => base.Invoke(characterName);
}

public sealed class GetChangedItems(IDalamudPluginInterface pi)
    : FuncSubscriber<string, IReadOnlyDictionary<string, object?>>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetChangedItems)}";

    public new IReadOnlyDictionary<string, object?> Invoke(string collectionName)
        => base.Invoke(collectionName);
}

public sealed class GetCollectionForType(IDalamudPluginInterface pi)
    : FuncSubscriber<ApiCollectionType, string>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetCollectionForType)}";

    public new string Invoke(ApiCollectionType collectionType)
        => base.Invoke(collectionType);
}

public sealed class SetCollectionForType(IDalamudPluginInterface pi)
    : FuncSubscriber<ApiCollectionType, string, bool, bool, (PenumbraApiEc, string)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(SetCollectionForType)}";

    public new (PenumbraApiEc ErrorCode, string OldCollectionName) Invoke(ApiCollectionType collectionType, string collectionName,
        bool allowCreateNew = true, bool allowDelete = true)
        => base.Invoke(collectionType, collectionName, allowCreateNew, allowDelete);
}

public sealed class GetCollectionForObject(IDalamudPluginInterface pi)
    : FuncSubscriber<int, (bool, bool, string)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(GetCollectionForObject)}";

    public new (bool ObjectValid, bool IndividualSet, string CollectionName) Invoke(int objectIndex)
        => base.Invoke(objectIndex);
}

public sealed class SetCollectionForObject(IDalamudPluginInterface pi)
    : FuncSubscriber<int, string, bool, bool, (PenumbraApiEc, string)>(pi, Label)
{
    public const string Label = $"Penumbra.{nameof(SetCollectionForObject)}";

    public new (PenumbraApiEc ErrorCode, string OldCollectionName) Invoke(int objectIndex, string collectionName, bool allowCreateNew = true,
        bool allowDelete = true)
        => base.Invoke(objectIndex, collectionName, allowCreateNew, allowDelete);
}
