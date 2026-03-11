namespace GagSpeak.FileSystems;

/// <summary>
///     The kind of change being made to the current restriction
/// </summary>
public enum StorageChangeType
{
    Created,
    Deleted,
    Renamed,
    Modified,
}
