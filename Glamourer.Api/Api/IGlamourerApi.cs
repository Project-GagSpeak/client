namespace Glamourer.Api.Api;

/// <summary> The full API available. </summary>
public interface IGlamourerApi : IGlamourerApiBase
{
    /// <inheritdoc cref="IGlamourerApiDesigns"/>
    public IGlamourerApiDesigns Designs { get; }

    /// <inheritdoc cref="IGlamourerApiItems"/>
    public IGlamourerApiItems   Items   { get; }

    /// <inheritdoc cref="IGlamourerApiState"/>
    public IGlamourerApiState   State   { get; }
}
