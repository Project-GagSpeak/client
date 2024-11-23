using Dalamud.Plugin;
using Glamourer.Api.Api;
using Glamourer.Api.Helpers;

namespace Glamourer.Api.IpcSubscribers;

/// <inheritdoc cref="IGlamourerApiBase.ApiVersion"/>
public sealed class ApiVersion(IDalamudPluginInterface pi)
    : FuncSubscriber<(int, int)>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(ApiVersion)}.V2";

    /// <inheritdoc cref="IGlamourerApiBase.ApiVersion"/>
    public new (int Major, int Minor) Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static FuncProvider<(int, int)> Provider(IDalamudPluginInterface pi, IGlamourerApiBase api)
        => new(pi, Label, () => api.ApiVersion);
}

/// <summary> Triggered when the Glamourer API is initialized and ready. </summary>
public static class Initialized
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(Initialized)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber Subscriber(IDalamudPluginInterface pi, params Action[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider Provider(IDalamudPluginInterface pi)
        => new(pi, Label);
}

/// <summary> Triggered when the Glamourer API is fully disposed and unavailable. </summary>
public static class Disposed
{
    /// <summary> The label. </summary>
    public const string Label = $"Glamourer.{nameof(Disposed)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber Subscriber(IDalamudPluginInterface pi, params Action[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider Provider(IDalamudPluginInterface pi)
        => new(pi, Label);
}
