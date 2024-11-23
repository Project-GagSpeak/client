namespace OtterGui.Compression;

/// <summary> Available compression methods for file system compression. </summary>
public enum CompressionAlgorithm
{
    None      = -2,
    Lznt1     = -1,
    Xpress4K  = 0,
    Lzx       = 1,
    Xpress8K  = 2,
    XPress16K = 3,
}
