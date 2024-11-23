using Newtonsoft.Json;
using OtterGui.Log;

namespace OtterGui.Classes;

public static partial class Backup
{
    public const int MaxNumBackups = 10;

    /// <summary>
    /// Create a backup named by ISO 8601 of the current time.
    /// </summary>
    public static void CreatePermanentBackup(Logger logger, DirectoryInfo dir, IReadOnlyCollection<FileInfo> files, string name)
        => CreateBackupInternal(logger, dir, files, name);

    /// <summary>
    /// Create a backup named by ISO 8601 of the current time.
    /// </summary>
    /// <remarks>
    /// If the newest previously existing backup equals the current state of files, do not create a new backup.
    /// If the maximum number of backups is exceeded afterwards, delete the oldest backup.
    /// </remarks>
    public static void CreateAutomaticBackup(Logger logger, DirectoryInfo dir, IReadOnlyCollection<FileInfo> files)
        => CreateBackupInternal(logger, dir, files, null);

    /// <summary> Check all existing backups for a specific file and try to parse it. </summary>
    /// <typeparam name="T"> The type the file should be converted into. </typeparam>
    /// <param name="dir"> The plugins config directory. </param>
    /// <param name="fileName"> The full path of the file. </param>
    /// <param name="parsedFile"> On success, the parsed object. </param>
    /// <param name="message"> Several lines of status messages for failures or the success. </param>
    /// <param name="parse"> The converter function turning the read data into the object. </param>
    /// <returns> True on success. </returns>
    public static bool TryGetFile<T>(DirectoryInfo dir, string fileName, [NotNullWhen(true)] out T? parsedFile, out string message,
        Func<string, T?>? parse = null)
    {
        message =   $"The configuration file {fileName} was corrupted, trying to automatically restore from backup.\n";
        parse   ??= JsonConvert.DeserializeObject<T>;
        var directory = CreateBackupDirectory(dir);
        fileName = Path.GetRelativePath(dir.Parent!.FullName, fileName);
        // Skip one since the newest backup apparently failed.
        foreach (var existingBackup in EnumerateBackups(directory).OrderByDescending(f => f.CreationTimeUtc).Skip(1))
        {
            try
            {
                using var oldFileStream = File.Open(existingBackup.FullName, FileMode.Open);
                using var oldZip        = new ZipArchive(oldFileStream, ZipArchiveMode.Read);
                var       entry         = oldZip.GetEntry(fileName);
                if (entry == null)
                {
                    message += $"\nBackup from {existingBackup.CreationTime} did not contain the file {fileName}";
                    continue;
                }

                using var file = entry.Open();
                using var tr   = new StreamReader(file, Encoding.UTF8);
                var       text = tr.ReadToEnd();
                parsedFile = parse(text);
                if (parsedFile != null)
                {
                    message += $"\nBackup from {existingBackup.CreationTime} successfully loaded {fileName}.";
                    return true;
                }
            }
            catch (Exception ex)
            {
                message += $"\nBackup from {existingBackup.CreationTime} could not successfully load the file {fileName}: {ex.Message}";
            }
        }

        parsedFile = default;
        return false;
    }

    private static void CreateBackupInternal(Logger logger, DirectoryInfo dir, IReadOnlyCollection<FileInfo> files, string? name)
    {
        try
        {
            var configDirectory = dir.Parent!.FullName;
            var directory       = CreateBackupDirectory(dir);
            if (name == null)
            {
                var (newestFile, oldestFile, numFiles) = CheckExistingBackups(directory);
                var newBackupName = Path.Combine(directory.FullName, $"{DateTime.Now:yyyyMMddHHmmss}.zip");
                if (newestFile == null || CheckNewestBackup(logger, newestFile, configDirectory, files.Count))
                {
                    CreateBackupFile(files, newBackupName, configDirectory);
                    if (numFiles > MaxNumBackups)
                        oldestFile!.Delete();
                }
            }
            else
            {
                var fileName = $"{name}.zip";
                if (FormatRegex().IsMatch(fileName))
                    fileName = $"{name}-.zip";
                var newBackupName = Path.Combine(directory.FullName, fileName);
                CreateBackupFile(files, newBackupName, configDirectory);
            }
        }
        catch (Exception e)
        {
            logger.Error($"Could not create backups:\n{e}");
        }
    }

    // Obtain the backup directory. Create it if it does not exist.
    private static DirectoryInfo CreateBackupDirectory(DirectoryInfo dir)
    {
        var path   = Path.Combine(dir.Parent!.Parent!.FullName, "backups", dir.Name);
        var newDir = new DirectoryInfo(path);
        if (!newDir.Exists)
            newDir = Directory.CreateDirectory(newDir.FullName);

        return newDir;
    }

    // Check the already existing backups.
    // Only keep MaxNumBackups at once, and delete the oldest if the number would be exceeded.
    // Return the newest backup.
    private static (FileInfo? Newest, FileInfo? Oldest, int Count) CheckExistingBackups(DirectoryInfo backupDirectory)
    {
        var       count  = 0;
        FileInfo? newest = null;
        FileInfo? oldest = null;

        foreach (var file in EnumerateBackups(backupDirectory))
        {
            ++count;
            var time = file.CreationTimeUtc;
            if ((oldest?.CreationTimeUtc ?? DateTime.MaxValue) > time)
                oldest = file;

            if ((newest?.CreationTimeUtc ?? DateTime.MinValue) < time)
                newest = file;
        }

        return (newest, oldest, count);
    }

    [GeneratedRegex(@"^\d{14}\.zip$", RegexOptions.ExplicitCapture | RegexOptions.NonBacktracking)]
    private static partial Regex FormatRegex();

    /// <summary> Enumerate existing standard backups. </summary>
    private static IEnumerable<FileInfo> EnumerateBackups(DirectoryInfo backupDirectory)
        => backupDirectory.EnumerateFiles("*.zip").Where(f => FormatRegex().IsMatch(f.Name));

    // Compare the newest backup against the currently existing files.
    // If there are any differences, return true, and if they are completely identical, return false.
    private static bool CheckNewestBackup(Logger logger, FileInfo newestFile, string configDirectory, int fileCount)
    {
        try
        {
            using var oldFileStream = File.Open(newestFile.FullName, FileMode.Open);
            using var oldZip        = new ZipArchive(oldFileStream, ZipArchiveMode.Read);
            // Number of stored files is different.
            if (fileCount != oldZip.Entries.Count)
                return true;

            // Since number of files is identical,
            // the backups are identical if every file in the old backup
            // still exists and is identical.
            foreach (var entry in oldZip.Entries)
            {
                var file = Path.Combine(configDirectory, entry.FullName);
                if (!File.Exists(file))
                    return true;

                using var currentData = File.OpenRead(file);
                using var oldData     = entry.Open();

                if (!Equals(currentData, oldData))
                    return true;
            }
        }
        catch (Exception e)
        {
            logger.Warning($"Could not read the newest backup file {newestFile.FullName}:\n{e}");
            return true;
        }

        return false;
    }

    // Create the actual backup, storing all the files relative to the given configDirectory in the zip.
    private static void CreateBackupFile(IEnumerable<FileInfo> files, string fileName, string configDirectory)
    {
        using var fileStream = File.Open(fileName, FileMode.Create);
        using var zip        = new ZipArchive(fileStream, ZipArchiveMode.Create);
        foreach (var file in files.Where(f => File.Exists(f.FullName)))
            zip.CreateEntryFromFile(file.FullName, Path.GetRelativePath(configDirectory, file.FullName), CompressionLevel.Optimal);
    }

    // Compare two streams per byte and return if they are equal.
    [SkipLocalsInit]
    private static bool Equals(Stream lhs, Stream rhs)
    {
        const int  bufferSize = 1024;
        Span<byte> bufferLhs  = stackalloc byte[bufferSize];
        Span<byte> bufferRhs  = stackalloc byte[bufferSize];
        while (true)
        {
            var bytesLhs = lhs.ReadAtLeast(bufferLhs, bufferSize, false);
            var bytesRhs = rhs.ReadAtLeast(bufferRhs, bufferSize, false);
            if (bytesLhs != bytesRhs || !bufferLhs.SequenceEqual(bufferRhs))
                return false;
            if (bytesLhs < bufferSize)
                return true;
        }
    }
}
