namespace Glamourer.Api.Api;

/// <summary> Basic API functions. </summary>
public interface IGlamourerApiBase
{
    /// <summary>
    /// Get the current API version of the Glamourer available in this installation.
    /// Major version changes indicate incompatibilities, minor version changes are backward-compatible additions.
    /// </summary>
    public (int Major, int Minor) ApiVersion { get; }
}
