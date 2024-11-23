namespace OtterGui.Compression;

internal static partial class Interop
{
    private const uint InvalidFileSize = 4294967295;

    /// <summary> Obtain the compressed size of a file by its path. </summary>
    /// <param name="path"> The path of the file. </param>
    /// <returns> The compressed size of the file. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static long GetCompressedFileSize(string path)
    {
        var loSize = GetCompressedFileSizeW(path, out var hiSize);
        if (loSize == InvalidFileSize)
            return -1;

        return ((long)hiSize << 32) | loSize;
    }

    /// <summary> Round a given size up to a multiple of the cluster size. </summary>
    /// <param name="size"> The file size. </param>
    /// <param name="clusterSize"> The cluster size. </param>
    /// <returns> The size rounded up to a multiple of cluster size if necessary. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static long RoundToCluster(long size, long clusterSize)
        => (size + clusterSize - 1) / clusterSize * clusterSize;

    /// <summary> Check if a file is compacted with a given algorithm. </summary>
    /// <param name="filePath"> The path to the file. </param>
    /// <param name="algorithm"> The algorithm to check. </param>
    /// <returns> Whether the file is compacted with the given algorithm. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool IsCompactedFile(string filePath, CompressionAlgorithm algorithm = CompressionAlgorithm.Xpress8K)
    {
        var buf = 8u;
        _ = WofIsExternalFile(filePath, out var isExtFile, out _, out var info, ref buf);
        return isExtFile != 0 && info.Algorithm == algorithm;
    }

    /// <summary> Try to compact a file with a given algorithm. </summary>
    /// <param name="path"> The path to the file to be compacted. </param>
    /// <param name="algorithm"> The algorithm to be used to compact. </param>
    public static unsafe void CompactFile(string path, CompressionAlgorithm algorithm = CompressionAlgorithm.Xpress8K)
    {
        const ulong wofProviderFile          = 2UL;
        const int   compressionNotBeneficial = unchecked((int)0x80070158);

        var       efInfo = new WofFileCompressionInfoV1(algorithm, 0);
        var       length = (ulong)sizeof(WofFileCompressionInfoV1);
        using var fs     = File.Open(path, FileMode.Open);
        var       hFile  = fs.SafeFileHandle.DangerousGetHandle();
        var       ret    = WofSetFileDataLocation(hFile, wofProviderFile, (nint)(&efInfo), length);
        if (ret is not 0 and not compressionNotBeneficial)
            Marshal.ThrowExceptionForHR(ret);
    }

    /// <summary> Try to remove compacting from a file. </summary>
    /// <param name="path"> The file to remove compacting from. </param>
    public static void DecompactFile(string path)
    {
        const uint fsctlDeleteExternalBacking = 0x90314U;

        using var fs      = File.Open(path, FileMode.Open);
        var       hDevice = fs.SafeFileHandle.DangerousGetHandle();
        var       ret     = DeviceIoControl(hDevice, fsctlDeleteExternalBacking, nint.Zero, 0, nint.Zero, 0, out _, out _);
        Marshal.ThrowExceptionForHR(ret);
    }

    [LibraryImport("kernel32.dll")]
    public static partial int GetDiskFreeSpaceW([MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName, out uint lpSectorsPerCluster,
        out uint lpBytesPerSector, out uint lpNumberOfFreeClusters, out uint lpTotalNumberofClusters);

    private record struct WofFileCompressionInfoV1(CompressionAlgorithm Algorithm, ulong Flags);

    [LibraryImport("kernel32.dll")]
    private static partial int DeviceIoControl(nint hDevice, uint dwIoControlCode, nint lpInBuffer, uint nInBufferSize, nint lpOutBuffer,
        uint nOutBufferSize, out nint lpBytesReturned, out nint lpOverlapped);

    [LibraryImport("kernel32.dll")]
    private static partial uint GetCompressedFileSizeW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, out uint lpFileSizeHigh);


    [LibraryImport("WoFUtil.dll")]
    private static partial int WofIsExternalFile([MarshalAs(UnmanagedType.LPWStr)] string filepath, out int isExternalFile, out uint provider,
        out WofFileCompressionInfoV1 info, ref uint bufferLength);

    [LibraryImport("WofUtil.dll")]
    private static partial int WofSetFileDataLocation(nint fileHandle, ulong provider, nint externalFileInfo, ulong length);
}
