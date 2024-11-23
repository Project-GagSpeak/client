namespace Penumbra.Api.Api;

/// <summary> The entire API. </summary>
public interface IPenumbraApi : IPenumbraApiBase
{
    /// <inheritdoc cref="IPenumbraApiCollection"/>
    public IPenumbraApiCollection Collection { get; }

    /// <inheritdoc cref="IPenumbraApiEditing"/>
    public IPenumbraApiEditing Editing { get; }

    /// <inheritdoc cref="IPenumbraApiGameState"/>
    public IPenumbraApiGameState GameState { get; }

    /// <inheritdoc cref="IPenumbraApiMeta"/>
    public IPenumbraApiMeta Meta { get; }

    /// <inheritdoc cref="IPenumbraApiMods"/>
    public IPenumbraApiMods Mods { get; }

    /// <inheritdoc cref="IPenumbraApiModSettings"/>
    public IPenumbraApiModSettings ModSettings { get; }

    /// <inheritdoc cref="IPenumbraApiPluginState"/>
    public IPenumbraApiPluginState PluginState { get; }

    /// <inheritdoc cref="IPenumbraApiRedraw"/>
    public IPenumbraApiRedraw Redraw { get; }

    /// <inheritdoc cref="IPenumbraApiResolve"/>
    public IPenumbraApiResolve Resolve { get; }

    /// <inheritdoc cref="IPenumbraApiResourceTree"/>
    public IPenumbraApiResourceTree ResourceTree { get; }

    /// <inheritdoc cref="IPenumbraApiTemporary"/>
    public IPenumbraApiTemporary Temporary { get; }

    /// <inheritdoc cref="IPenumbraApiUi"/>
    public IPenumbraApiUi Ui { get; }
}
