namespace GagSpeak.FileSystems;

/// <summary>
///     The kind of change being made to the current restriction
/// </summary>
/// <remarks> May remove later? </remarks>
public enum StorageChangeType
{
    Created,
    Deleted,
    Renamed,
    Modified,
}
