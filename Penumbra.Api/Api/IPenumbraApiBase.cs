namespace Penumbra.Api.Api;

/// <summary> Base interface for the API that is always available, regardless of version. </summary>
public interface IPenumbraApiBase
{
    /// <summary>
    /// The API version is staggered in two parts.
    /// The major/Breaking version only increments if there are changes breaking backwards compatibility.
    /// The minor/Feature version increments any time there is something added
    /// and resets when Breaking is incremented.
    /// </summary>
    public (int Breaking, int Feature) ApiVersion { get; }

    /// <summary> Whether the API is still usable. </summary>
    public bool Valid { get; }
}
