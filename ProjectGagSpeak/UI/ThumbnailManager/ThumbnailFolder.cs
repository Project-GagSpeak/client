using GagSpeak.Services.Configs;
using GagSpeak.Services.Textures;

namespace GagSpeak.Gui;

public class ThumbnailFolder : IDisposable
{
    private readonly ILogger _logger;
    private readonly CosmeticService _cosmetics;

    private List<ThumbnailFile> _files = new List<ThumbnailFile>();
    private Task? _scanTask = null;
    public ThumbnailFolder(ILogger log, CosmeticService cosmetics, ImageDataType folder)
    {
        _logger = log;
        _cosmetics = cosmetics;
        FolderName = folder;
        ScanFiles();
    }

    public ILogger Log => _logger;
    public bool IsScanning => _scanTask is not null && !_scanTask.IsCompleted;
    public ImageDataType FolderName { get; init; }
    public IEnumerable<ThumbnailFile> AllFiles => _files;

    public void Add(ThumbnailFile file)
    {
        _files.Add(file);
    }

    public void Remove(ThumbnailFile file)
    {
        if (!_files.Contains(file))
            return;

        var dir = Path.Combine(ConfigFileProvider.ThumbnailDirectory, FolderName.ToString());
        if (File.Exists(Path.Combine(dir, file.FileName)))
        {
            try
            {
                File.Delete(Path.Combine(dir, file.FileName));
                file.Dispose();
                _files.Remove(file);
                _logger.LogDebug($"Deleted thumbnail file: {file.FileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete thumbnail file: {file.FileName}, Error: {ex.Message}");
            }
        }
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
            _logger.LogWarning($"Thumbnail directory does not exist: {directoryPath}, Creating one!");
            Directory.CreateDirectory(directoryPath);
        }

        // obtain all files from the directory folder.
        var filePaths = Directory.GetFiles(directoryPath, "*.png");
        _logger.LogDebug($"Scanning {filePaths.Length} files in {directoryPath}");

        await Parallel.ForEachAsync(filePaths, async (filePath, ct) =>
        {
            if (await TextureManagerEx.RentMetadataPath(FolderName, filePath) is { } validImage)
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName is null)
                {
                    _logger.LogWarning($"Failed to get file name from path: {filePath}");
                    return;
                }

                Add(new ThumbnailFile(this, fileName, validImage));
                _logger.LogTrace($"Added thumbnail file: {fileName} to folder: {FolderName}");
            }
        });

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
