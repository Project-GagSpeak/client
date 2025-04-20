using GagSpeak.Services.Configs;
using GagSpeak.Services.Textures;
using System.Globalization;

namespace GagSpeak.UI;

public class ThumbnailFolder : IDisposable
{
    private readonly CosmeticService _cosmetics;

    private List<ThumbnailFile> _files = new List<ThumbnailFile>();
    private Task? _scanTask = null;

    public ThumbnailFolder(CosmeticService cosmetics, ImageDataType folder)
    {
        _cosmetics = cosmetics;
        FolderName = folder;

        // Run initial Scan.
        ScanFiles();
    }

    public bool IsScanning => _scanTask is not null && !_scanTask.IsCompleted;
    public ImageDataType FolderName { get; init; }
    public IEnumerable<ThumbnailFile> AllFiles => _files;

    public void Add(ThumbnailFile file)
    {
        _files.Add(file);
    }

    public void ClearFiles()
    {
        // Be memory efficient, and dispose each texture-wrap prior to the clear.
        foreach (var file in _files)
            file.Dispose();

        _files.Clear();
    }

    public void ScanFiles()
    {
        if(IsScanning)
            return;

        _scanTask = ScanTask();
    }
    private async Task ScanTask()
    {
        // clear if we have any files.
        if (_files.Count > 0)
            ClearFiles();

        var directoryPath = Path.Combine(ConfigFileProvider.ThumbnailDirectory, FolderName.ToString());
        if (!Directory.Exists(directoryPath))
        {
            GagSpeak.StaticLog.Warning($"Thumbnail directory does not exist: {directoryPath}, Creating one!");
            Directory.CreateDirectory(directoryPath);
        }

        // obtain all files from the directory folder.
        var filePaths = Directory.GetFiles(directoryPath, "*.png");
        GagSpeak.StaticLog.Debug($"Scanning {filePaths.Length} files in {directoryPath}");

        var tasks = filePaths
            .Select(x =>
            {
                return Task.Run(async () =>
                {
                    // Check if the file is a valid thumbnail.
                    if (await _cosmetics.RentThumbnailFile(FolderName, x) is { } validImage)
                    {
                        var fileName = Path.GetFileName(x);
                        if (fileName is null)
                        {
                            GagSpeak.StaticLog.Warning($"Failed to get file name from path: {x}");
                            return;
                        }

                        Add(new ThumbnailFile(fileName, validImage));
                        GagSpeak.StaticLog.Verbose($"Added thumbnail file: {fileName} to folder: {FolderName}");
                    }
                });
            });
        // Run all tasks in parallel.
        await Task.WhenAll(tasks);
        // Order them properly.
        _files = _files.OrderBy(x => x.FileName).ToList();
    }

    public void Dispose()
    {
        // Halt scan task, if it is running.
        if (IsScanning)
        {
            _scanTask?.Wait();
            _scanTask = null;
        }

        ClearFiles();
    }
}
