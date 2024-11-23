using OtterGui.Log;
using OtterGui.Services;

namespace OtterGui.Compression;

public class FileCompactor(Logger logger) : IDisposable, IService
{
    public readonly bool CanCompact = !Dalamud.Utility.Util.IsWine();

    /// <summary> Whether to use file system compression at all. </summary>
    public bool Enabled
    {
        get => _enabled;
        set
        {
            logger.Information(
                $"File System Compression was {(value ? "enabled" : "disabled")}{(CanCompact ? string.Empty : " but was not available")}.");
            _enabled = CanCompact && value;
        }
    }

    private          bool                    _enabled;
    private readonly Dictionary<string, int> _clusterSizes = new(8, StringComparer.Ordinal);

    private Task?                    _massCompact;
    private CancellationTokenSource? _cancellation;

    /// <summary> Check if a mass compact operation is currently running. </summary>
    public bool MassCompactRunning
        => _massCompact is { IsCompleted: false };

    /// <summary> The file currently being compacted, if any. </summary>
    public FileInfo? CurrentFile { get; private set; }

    /// <summary> The index of the file currently being compacted for Progress. </summary>
    public int CurrentIndex { get; private set; }

    /// <summary> The total number of files in the current mass compact operation. </summary>
    public int TotalFiles { get; private set; }

    public void Dispose()
        => CancelMassCompact();

    /// <summary> Cancel the current mass compact operation if one is running. </summary>
    public void CancelMassCompact()
        => _cancellation?.Cancel();

    /// <summary> Start a new mass compact operation on a set of files. </summary>
    /// <param name="files"> The set of files we want to compact. </param>
    /// <param name="algorithm"> The compression algorithm to use. Use None to decompress files instead. </param>
    /// <returns> If the task could successfully be started. </returns>
    public bool StartMassCompact(IEnumerable<FileInfo> files, CompressionAlgorithm algorithm)
    {
        if (!CanCompact)
            return false;

        if (MassCompactRunning)
        {
            logger.Error("Triggered Mass Compact of files while it was already running.");
            return false;
        }

        _cancellation = new CancellationTokenSource();
        var token = _cancellation.Token;
        _massCompact = Task.Run(() =>
        {
            TotalFiles   = 1;
            CurrentIndex = 0;
            CurrentFile  = null;
            var list = files.ToList();
            TotalFiles = list.Count;
            logger.Information(
                $"Starting Mass {(algorithm is CompressionAlgorithm.None ? "Decompact" : $"Compact with {algorithm}")} for {TotalFiles} files.");
            for (; CurrentIndex < list.Count; ++CurrentIndex)
            {
                if (token.IsCancellationRequested)
                    return;

                CurrentFile = list[CurrentIndex];
                if (algorithm is CompressionAlgorithm.None)
                    Interop.DecompactFile(CurrentFile.FullName);
                else
                    CompactFile(CurrentFile.FullName, algorithm);
            }
        }, token);
        return true;
    }

    /// <summary> Get the actual file size of a file on disk. </summary>
    /// <param name="filePath"> The path to the file. </param>
    /// <returns> The actual file size considering compression and clustering. </returns>
    public long GetFileSizeOnDisk(string filePath)
    {
        if (!CanCompact)
            return new FileInfo(filePath).Length;

        var size        = Interop.GetCompressedFileSize(filePath);
        if (size < 0)
            return new FileInfo(filePath).Length;

        var clusterSize = GetClusterSize(filePath);
        if (clusterSize == -1)
            return size;

        return Interop.RoundToCluster(size, clusterSize);
    }

    /// <summary> Write bytes and conditionally compact the file afterwards. </summary>
    /// <param name="filePath"> The file to write to. </param>
    /// <param name="decompressedFile"> The data to write. </param>
    public void WriteAllBytes(string filePath, byte[] decompressedFile)
    {
        File.WriteAllBytes(filePath, decompressedFile);

        if (Enabled)
            CompactFile(filePath);
    }

    /// <summary> Asynchronously write bytes and conditionally compact the file afterwards. </summary>
    /// <param name="filePath"> The file to write to. </param>
    /// <param name="decompressedFile"> The data to write. </param>
    /// <param name="token"> A cancellation token. </param>
    public async Task WriteAllBytesAsync(string filePath, byte[] decompressedFile, CancellationToken token = default)
    {
        await File.WriteAllBytesAsync(filePath, decompressedFile, token);

        if (Enabled && !token.IsCancellationRequested)
            CompactFile(filePath);
    }

    /// <summary> Get the cluster size for a drive root. </summary>
    private int GetClusterSize(string filePath)
    {
        if (!File.Exists(filePath))
            return -1;

        var root = Path.GetPathRoot(filePath) ?? string.Empty;
        if (root.Length == 0)
            return -1;

        if (!_clusterSizes.TryGetValue(root, out var size))
        {
            var result = Interop.GetDiskFreeSpaceW(root, out var sectorsPerCluster, out var bytesPerSector, out _, out _);
            if (result == 0)
                return -1;

            size = (int)(sectorsPerCluster * bytesPerSector);
            logger.Verbose($"Cluster size for root {root} is {size}.");
            _clusterSizes.Add(root, size);
        }

        return size;
    }

    /// <summary> Try to compact a file with a given algorithm. </summary>
    private bool CompactFile(string filePath, CompressionAlgorithm algorithm = CompressionAlgorithm.Xpress8K)
    {
        try
        {
            var oldSize     = new FileInfo(filePath).Length;
            var clusterSize = GetClusterSize(filePath);

            var minFileSize = algorithm switch
            {
                CompressionAlgorithm.None      => clusterSize,
                CompressionAlgorithm.Lznt1     => clusterSize,
                CompressionAlgorithm.Xpress4K  => Math.Max(clusterSize, 4 * 1024),
                CompressionAlgorithm.Lzx       => clusterSize,
                CompressionAlgorithm.Xpress8K  => Math.Max(clusterSize, 8 * 1024),
                CompressionAlgorithm.XPress16K => Math.Max(clusterSize, 16 * 1024),
                _                              => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null),
            };

            if (oldSize < minFileSize)
            {
                logger.Verbose($"File {filePath} is smaller than cluster size ({minFileSize}), it will not be compacted.");
                return false;
            }


            if (Interop.IsCompactedFile(filePath, algorithm))
            {
                logger.Verbose($"File {filePath} is already compacted with {algorithm}.");
                return true;
            }


            Interop.CompactFile(filePath, algorithm);
            logger.Verbose($"Compacted {filePath} from {oldSize} bytes to {new LazyString(() => GetFileSizeOnDisk(filePath).ToString())} bytes.");
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Unexpected problem when compacting file {filePath}:\n{ex}");
            return false;
        }
    }
}
