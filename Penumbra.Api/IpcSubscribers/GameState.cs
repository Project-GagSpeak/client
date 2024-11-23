using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiGameState.GetDrawObjectInfo"/>
public sealed class GetDrawObjectInfo(IDalamudPluginInterface pi)
    : FuncSubscriber<nint, (nint, (Guid, string))>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetDrawObjectInfo)}.V5";

    /// <inheritdoc cref="IPenumbraApiGameState.GetDrawObjectInfo"/>
    public new (nint GameObject, (Guid Id, string Name) AssociatedCollection) Invoke(nint drawObjectAddress)
        => base.Invoke(drawObjectAddress);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<nint, (nint, (Guid, string))> Provider(IDalamudPluginInterface pi, IPenumbraApiGameState api)
        => new(pi, Label, api.GetDrawObjectInfo);
}

/// <inheritdoc cref="IPenumbraApiGameState.GetCutsceneParentIndex"/>
public sealed class GetCutsceneParentIndex(IDalamudPluginInterface pi)
    : FuncSubscriber<int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetCutsceneParentIndex)}";

    /// <inheritdoc cref="IPenumbraApiGameState.GetCutsceneParentIndex"/>
    public new int Invoke(int actorIndex)
        => base.Invoke(actorIndex);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, int> Provider(IDalamudPluginInterface pi, IPenumbraApiGameState api)
        => new(pi, Label, api.GetCutsceneParentIndex);
}

/// <inheritdoc cref="IPenumbraApiGameState.SetCutsceneParentIndex"/>
public sealed class SetCutsceneParentIndex(IDalamudPluginInterface pi)
    : FuncSubscriber<int, int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(SetCutsceneParentIndex)}.V5";

    /// <inheritdoc cref="IPenumbraApiGameState.SetCutsceneParentIndex"/>
    public new PenumbraApiEc Invoke(int copyIdx, int newParentIdx)
        => (PenumbraApiEc)base.Invoke(copyIdx, newParentIdx);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, int, int> Provider(IDalamudPluginInterface pi, IPenumbraApiGameState api)
        => new(pi, Label, (a, b) => (int) api.SetCutsceneParentIndex(a, b));
}

/// <inheritdoc cref="IPenumbraApiGameState.CreatingCharacterBase"/>
public static class CreatingCharacterBase
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(CreatingCharacterBase)}.V5";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<nint, Guid, nint, nint, nint> Subscriber(IDalamudPluginInterface pi,
        params Action<nint, Guid, nint, nint, nint>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<nint, Guid, nint, nint, nint> Provider(IDalamudPluginInterface pi, IPenumbraApiGameState api)
        => new(pi, Label, t => api.CreatingCharacterBase += t.Invoke, t => api.CreatingCharacterBase -= t.Invoke);
}

/// <inheritdoc cref="IPenumbraApiGameState.CreatedCharacterBase"/>
public static class CreatedCharacterBase
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(CreatedCharacterBase)}.V5";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<nint, Guid, nint> Subscriber(IDalamudPluginInterface pi, params Action<nint, Guid, nint>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<nint, Guid, nint> Provider(IDalamudPluginInterface pi, IPenumbraApiGameState api)
        => new(pi, Label, t => api.CreatedCharacterBase += t.Invoke, t => api.CreatedCharacterBase -= t.Invoke);
}

/// <inheritdoc cref="IPenumbraApiGameState.GameObjectResourceResolved"/>
public static class GameObjectResourcePathResolved
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GameObjectResourcePathResolved)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<nint, string, string> Subscriber(IDalamudPluginInterface pi, params Action<nint, string, string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<nint, string, string> Provider(IDalamudPluginInterface pi, IPenumbraApiGameState api)
        => new(pi, Label, t => api.GameObjectResourceResolved += t.Invoke, t => api.GameObjectResourceResolved -= t.Invoke);
}
