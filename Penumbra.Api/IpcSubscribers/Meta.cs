using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiMeta.GetPlayerMetaManipulations"/>
public sealed class GetPlayerMetaManipulations(IDalamudPluginInterface pi)
    : FuncSubscriber<string>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetPlayerMetaManipulations)}";

    /// <inheritdoc cref="IPenumbraApiMeta.GetPlayerMetaManipulations"/>
    public new string Invoke()
        => base.Invoke();

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string> Provider(IDalamudPluginInterface pi, IPenumbraApiMeta api)
        => new(pi, Label, api.GetPlayerMetaManipulations);
}

/// <inheritdoc cref="IPenumbraApiMeta.GetMetaManipulations"/>
public sealed class GetMetaManipulations(IDalamudPluginInterface pi)
    : FuncSubscriber<int, string>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(GetMetaManipulations)}.V5";

    /// <inheritdoc cref="IPenumbraApiMeta.GetMetaManipulations"/>
    public new string Invoke(int gameObjectIdx)
        => base.Invoke(gameObjectIdx);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<int, string> Provider(IDalamudPluginInterface pi, IPenumbraApiMeta api)
        => new(pi, Label, api.GetMetaManipulations);
}
