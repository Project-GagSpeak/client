using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiCollection.GetCollections"/>
public sealed class GetCollections(IDalamudPluginInterface pi)
    : FuncSubscriber<Dictionary<Guid, string>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetCollections)}.V5";

    /// <inheritdoc cref="IPenumbraApiCollection.GetCollections"/>
    public new Dictionary<Guid, string> Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Dictionary<Guid, string>> Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, api.GetCollections);
}

/// <inheritdoc cref="IPenumbraApiCollection.GetCollectionsByIdentifier"/>
public sealed class GetCollectionsByIdentifier(IDalamudPluginInterface pi)
    : FuncSubscriber<string, List<(Guid Id, string Name)>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetCollectionsByIdentifier)}";

    /// <inheritdoc cref="IPenumbraApiCollection.GetCollectionsByIdentifier"/>
    public new List<(Guid Id, string Name)> Invoke(string name)
        => base.Invoke(name);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, List<(Guid Id, string Name)>> Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, api.GetCollectionsByIdentifier);
}

/// <inheritdoc cref="IPenumbraApiCollection.GetChangedItemsForCollection"/>
public sealed class GetChangedItemsForCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<Guid, Dictionary<string, object?>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetChangedItemsForCollection)}";

    /// <inheritdoc cref="IPenumbraApiCollection.GetChangedItemsForCollection"/>
    public new Dictionary<string, object?> Invoke(Guid collectionId)
        => base.Invoke(collectionId);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<Guid, Dictionary<string, object?>> Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, api.GetChangedItemsForCollection);
}

/// <inheritdoc cref="IPenumbraApiCollection.GetCollection"/>
public sealed class GetCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<byte, (Guid Id, string Name)?>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetCollection)}";

    /// <inheritdoc cref="IPenumbraApiCollection.GetCollection"/>
    public (Guid Id, string Name)? Invoke(ApiCollectionType type)
        => Invoke((byte)type);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<byte, (Guid Id, string Name)?> Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, b => api.GetCollection((ApiCollectionType)b));
}

/// <inheritdoc cref="IPenumbraApiCollection.GetCollectionForObject"/>
public sealed class GetCollectionForObject(IDalamudPluginInterface pi)
    : FuncSubscriber<int, (bool ObjectValid, bool IndividualSet, (Guid Id, string Name) EffectiveCollection)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetCollectionForObject)}.V5";

    /// <inheritdoc cref="IPenumbraApiCollection.GetCollectionForObject"/>
    public new (bool ObjectValid, bool IndividualSet, (Guid Id, string Name) EffectiveCollection) Invoke(int gameObjectIdx)
        => base.Invoke(gameObjectIdx);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, (bool ObjectValid, bool IndividualSet, (Guid Id, string Name) EffectiveCollection)>
        Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, api.GetCollectionForObject);
}

/// <inheritdoc cref="IPenumbraApiCollection.SetCollection"/>
public sealed class SetCollection(IDalamudPluginInterface pi)
    : FuncSubscriber<byte, Guid?, bool, bool, (int, (Guid Id, string Name)? OldCollection)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(SetCollection)}";

    /// <inheritdoc cref="IPenumbraApiCollection.SetCollectionForObject"/>
    public (PenumbraApiEc, (Guid Id, string Name)? OldCollection) Invoke(ApiCollectionType type, Guid? collectionId,
        bool allowCreateNew = true, bool allowDelete = true)
    {
        var (ec, pair) = Invoke((byte)type, collectionId, allowCreateNew, allowDelete);
        return ((PenumbraApiEc)ec, pair);
    }

    /// <summary> Create a provider. </summary>
    public static FuncProvider<byte, Guid?, bool, bool, (int, (Guid Id, string Name)? OldCollection)>
        Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, (t, g, a, b) =>
        {
            var (ret, collection) = api.SetCollection((ApiCollectionType)t, g, a, b);
            return ((int)ret, collection);
        });
}

/// <inheritdoc cref="IPenumbraApiCollection.SetCollectionForObject"/>
public sealed class SetCollectionForObject(IDalamudPluginInterface pi)
    : FuncSubscriber<int, Guid?, bool, bool, (int, (Guid Id, string Name)? OldCollection)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(SetCollectionForObject)}.V5";

    /// <inheritdoc cref="IPenumbraApiCollection.SetCollectionForObject"/>
    public new (PenumbraApiEc, (Guid Id, string Name)? OldCollection) Invoke(int gameObjectIdx, Guid? collectionId, bool allowCreateNew = true,
        bool allowDelete = true)
    {
        var (ec, pair) = base.Invoke(gameObjectIdx, collectionId, allowCreateNew, allowDelete);
        return ((PenumbraApiEc)ec, pair);
    }

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, Guid?, bool, bool, (int, (Guid Id, string Name)? OldCollection)>
        Provider(IDalamudPluginInterface pi, IPenumbraApiCollection api)
        => new(pi, Label, (i, g, a, b) =>
        {
            var (ret, collection) = api.SetCollectionForObject(i, g, a, b);
            return ((int)ret, collection);
        });
}
