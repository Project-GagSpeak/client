using Dalamud.Plugin;
using Penumbra.Api.Api;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.IpcSubscribers;

/// <inheritdoc cref="IPenumbraApiResolve.ResolveDefaultPath"/>
public sealed class ResolveDefaultPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ResolveDefaultPath)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ResolveDefaultPath"/>
    public new string Invoke(string gamePath)
        => base.Invoke(gamePath);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ResolveDefaultPath);
}

/// <inheritdoc cref="IPenumbraApiResolve.ResolveInterfacePath"/>
public sealed class ResolveInterfacePath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ResolveInterfacePath)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ResolveInterfacePath"/>
    public new string Invoke(string gamePath)
        => base.Invoke(gamePath);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ResolveInterfacePath);
}

/// <inheritdoc cref="IPenumbraApiResolve.ResolveGameObjectPath"/>
public sealed class ResolveGameObjectPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int, string>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ResolveGameObjectPath)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ResolveGameObjectPath"/>
    public new string Invoke(string gamePath, int gameObjectIdx)
        => base.Invoke(gamePath, gameObjectIdx);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, int, string> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ResolveGameObjectPath);
}

/// <inheritdoc cref="IPenumbraApiResolve.ResolvePlayerPath"/>
public sealed class ResolvePlayerPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ResolvePlayerPath)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ResolvePlayerPath"/>
    public new string Invoke(string gamePath)
        => base.Invoke(gamePath);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ResolvePlayerPath);
}

/// <inheritdoc cref="IPenumbraApiResolve.ReverseResolveGameObjectPath"/>
public sealed class ReverseResolveGameObjectPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, int, string[]>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ReverseResolveGameObjectPath)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ReverseResolveGameObjectPath"/>
    public new string[] Invoke(string gamePath, int gameObjectIdx)
        => base.Invoke(gamePath, gameObjectIdx);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, int, string[]> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ReverseResolveGameObjectPath);
}

/// <inheritdoc cref="IPenumbraApiResolve.ReverseResolvePlayerPath"/>
public sealed class ReverseResolvePlayerPath(IDalamudPluginInterface pi)
    : FuncSubscriber<string, string[]>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ReverseResolvePlayerPath)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ReverseResolvePlayerPath"/>
    public new string[] Invoke(string gamePath)
        => base.Invoke(gamePath);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string, string[]> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ReverseResolvePlayerPath);
}

/// <inheritdoc cref="IPenumbraApiResolve.ResolvePlayerPaths"/>
public sealed class ResolvePlayerPaths(IDalamudPluginInterface pi)
    : FuncSubscriber<string[], string[], (string[], string[][])>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ResolvePlayerPaths)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ResolvePlayerPaths"/>
    public new (string[], string[][]) Invoke(string[] forward, string[] reverse)
        => base.Invoke(forward, reverse);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string[], string[], (string[], string[][])> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ResolvePlayerPaths);
}

/// <inheritdoc cref="IPenumbraApiResolve.ResolvePlayerPathsAsync"/>
public sealed class ResolvePlayerPathsAsync(IDalamudPluginInterface pi)
    : FuncSubscriber<string[], string[], Task<(string[], string[][])>>(pi, Label)
{
    /// <summary> The label. </summary>
    public const string Label = $"Penumbra.{nameof(ResolvePlayerPathsAsync)}";

    /// <inheritdoc cref="IPenumbraApiResolve.ResolvePlayerPathsAsync"/>
    public new Task<(string[], string[][])> Invoke(string[] forward, string[] reverse)
        => base.Invoke(forward, reverse);

    /// <summary> Create a provider. </summary>
    public static FuncProvider<string[], string[], Task<(string[], string[][])>> Provider(IDalamudPluginInterface pi, IPenumbraApiResolve api)
        => new(pi, Label, api.ResolvePlayerPathsAsync);
}
