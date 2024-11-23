using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiUi.ChangedItemTooltip" />
public static class ChangedItemTooltip
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ChangedItemTooltip)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<ChangedItemType, uint> Subscriber(IDalamudPluginInterface pi,
        params Action<ChangedItemType, uint>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<ChangedItemType, uint> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (d => api.ChangedItemTooltip += d, d => api.ChangedItemTooltip -= d));
}

/// <inheritdoc cref="IPenumbraApiUi.ChangedItemClicked" />
public static class ChangedItemClicked
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ChangedItemClicked)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<MouseButton, ChangedItemType, uint> Subscriber(IDalamudPluginInterface pi,
        params Action<MouseButton, ChangedItemType, uint>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<MouseButton, ChangedItemType, uint> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (d => api.ChangedItemClicked += d, d => api.ChangedItemClicked -= d));
}

/// <inheritdoc cref="IPenumbraApiUi.PreSettingsTabBarDraw" />
public static class PreSettingsTabBarDraw
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(PreSettingsTabBarDraw)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string, float, float> Subscriber(IDalamudPluginInterface pi, params Action<string, float, float>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string, float, float> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (d => api.PreSettingsTabBarDraw += d, d => api.PreSettingsTabBarDraw -= d));
}

/// <inheritdoc cref="IPenumbraApiUi.PreSettingsPanelDraw" />
public static class PreSettingsDraw
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(PreSettingsDraw)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string> Subscriber(IDalamudPluginInterface pi, params Action<string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (d => api.PreSettingsPanelDraw += d, d => api.PreSettingsPanelDraw -= d));
}

/// <inheritdoc cref="IPenumbraApiUi.PostEnabledDraw" />
public static class PostEnabledDraw
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(PostEnabledDraw)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string> Subscriber(IDalamudPluginInterface pi, params Action<string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (d => api.PostEnabledDraw += d, d => api.PostEnabledDraw -= d));
}

/// <inheritdoc cref="IPenumbraApiUi.PostSettingsPanelDraw" />
public static class PostSettingsDraw
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(PostSettingsDraw)}";

    /// <summary> Create a new event subscriber. </summary>
    public static EventSubscriber<string> Subscriber(IDalamudPluginInterface pi, params Action<string>[] actions)
        => new(pi, Label, actions);

    /// <summary> Create a provider. </summary>
    public static EventProvider<string> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (d => api.PostSettingsPanelDraw += d, d => api.PostSettingsPanelDraw -= d));
}

/// <inheritdoc cref="IPenumbraApiUi.OpenMainWindow"/>
public sealed class OpenMainWindow(IDalamudPluginInterface pi)
    : FuncSubscriber<int, string, string, int>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(OpenMainWindow)}.V5";

    /// <inheritdoc cref="IPenumbraApiUi.OpenMainWindow"/>
    public PenumbraApiEc Invoke(TabType tab, string modDirectory = "", string modName = "")
        => (PenumbraApiEc)Invoke((int)tab, modDirectory, modName);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, string, string, int> Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, (a, b, c) => (int)api.OpenMainWindow((TabType)a, b, c));
}

/// <inheritdoc cref="IPenumbraApiUi.CloseMainWindow"/>
public sealed class CloseMainWindow(IDalamudPluginInterface pi)
    : ActionSubscriber(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra{nameof(CloseMainWindow)}";

    /// <inheritdoc cref="IPenumbraApiUi.CloseMainWindow"/>
    public new void Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static ActionProvider Provider(IDalamudPluginInterface pi, IPenumbraApiUi api)
        => new(pi, Label, api.CloseMainWindow);
}
