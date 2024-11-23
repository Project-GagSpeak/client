using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiRedraw.RedrawObject"/>
public sealed class RedrawObject(IDalamudPluginInterface pi)
    : ActionSubscriber<int, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(RedrawObject)}.V5";

    /// <inheritdoc cref="IPenumbraApiRedraw.RedrawObject"/>
    public void Invoke(int gameObjectIndex, RedrawType setting = RedrawType.Redraw)
        => base.Invoke(gameObjectIndex, (int)setting);

    /// <summary> Create a provider. </summary>
    public static ActionProvider<int, int> Provider(IDalamudPluginInterface pi, IPenumbraApiRedraw api)
        => new(pi, Label, (a, b) => api.RedrawObject(a, (RedrawType)b));
}

/// <inheritdoc cref="IPenumbraApiRedraw.RedrawAll"/>
public sealed class RedrawAll(IDalamudPluginInterface pi)
    : ActionSubscriber<int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(RedrawAll)}.V5";

    /// <inheritdoc cref="IPenumbraApiRedraw.RedrawAll"/>
    public void Invoke(RedrawType setting = RedrawType.Redraw)
        => base.Invoke((int)setting);

    /// <summary> Create a provider. </summary>
    public static ActionProvider<int> Provider(IDalamudPluginInterface pi, IPenumbraApiRedraw api)
        => new(pi, Label, a => api.RedrawAll((RedrawType)a));
}

/// <inheritdoc cref="IPenumbraApiRedraw.GameObjectRedrawn" />
public static class GameObjectRedrawn
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GameObjectRedrawn)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<nint, int> Subscriber(IDalamudPluginInterface pi, params Action<nint, int>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<nint, int> Provider(IDalamudPluginInterface pi, IPenumbraApiRedraw api)
        => new(pi, Label, t => api.GameObjectRedrawn += t.Invoke, t => api.GameObjectRedrawn -= t.Invoke);
}
