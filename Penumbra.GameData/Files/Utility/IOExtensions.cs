namespace Penumbra.GameData.Files.Utility;

public static class IoExtensions
{
    public static void Write<T>(this Stream stream, in T value) where T : unmanaged
        => stream.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));

    public static void Write<T>(this BinaryWriter writer, in T value) where T : unmanaged
        => writer.Write(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in value)));
}
