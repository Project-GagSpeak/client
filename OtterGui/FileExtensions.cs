namespace OtterGui;

public static class FileExtensions
{
    /// <summary> Recursively enumerate all non-hidden files. </summary>
    public static List<FileInfo> EnumerateNonHiddenFiles(this DirectoryInfo topDir)
        => EnumerateNonHiddenFilesFiltered(topDir, null);

    /// <summary> Recursively enumerate all non-hidden files that fulfill the given filter. </summary>
    public static List<FileInfo> EnumerateNonHiddenFilesFiltered(this DirectoryInfo topDir, Func<FileInfo, bool>? filter)
    {
        var ret = new List<FileInfo>();
        foreach (var info in topDir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
            EnumerateNonHiddenFilesRecurse(ret, info, filter);
        return ret;
    }

    /// <summary> Return whether a file or directory is hidden. </summary>
    public static bool IsHidden(this FileSystemInfo file)
        => file.Attributes.HasFlag(FileAttributes.Hidden);


    private static void EnumerateNonHiddenFilesRecurse(List<FileInfo> files, FileSystemInfo info, Func<FileInfo, bool>? filter)
    {
        if (info.IsHidden())
            return;

        switch (info)
        {
            case FileInfo file when (filter?.Invoke(file) ?? true): 
                files.Add(file);
                return;
            case DirectoryInfo dir:
            {
                foreach(var info2 in dir.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
                    EnumerateNonHiddenFilesRecurse(files, info2, filter);
                return;
            }
        }
    }
}
