using System.Buffers;
using System.IO.MemoryMappedFiles;

namespace Penumbra.GameData.Files.Utility;

/// <summary> Allows wrapping a memory-mapped file in a <see cref="Memory{byte}"/>. </summary>
/// <param name="accessor"> A view accessor over the file's memory. </param>
/// <param name="file"> The file, if it shall be closed on disposal, otherwise <c>null</c>. </param>
/// <param name="leaveOpen"> Whether to leave <paramref name="accessor"/> open on disposal. </param>
public sealed unsafe class MmioMemoryManager(MemoryMappedViewAccessor accessor, MemoryMappedFile? file = null, bool leaveOpen = false) : MemoryManager<byte>
{
    private readonly MemoryMappedViewAccessor _accessor  = accessor;
    private readonly MemoryMappedFile?        _file      = file;
    private readonly bool                     _leaveOpen = leaveOpen;

    private byte* _pointer;

    public override Memory<byte> Memory
        => CreateMemory(Length);

    public bool CanRead
        => _accessor.CanRead;

    public bool CanWrite
        => _accessor.CanWrite;

    public int Length
        => (int)_accessor.Capacity;

    public MmioMemoryManager(MemoryMappedFile file, MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWrite, bool leaveOpen = false)
        : this(file.CreateViewAccessor(0, 0, access), leaveOpen ? null : file)
    { }

    /// <seealso cref="MemoryMappedFile.CreateFromFile(string, FileMode, string?, long, MemoryMappedFileAccess)"/>
    public static MmioMemoryManager CreateFromFile(string path, FileMode mode = FileMode.Open, string? mapName = null, long capacity = 0, MemoryMappedFileAccess access = MemoryMappedFileAccess.ReadWrite)
        => new(MemoryMappedFile.CreateFromFile(path, mode, mapName, capacity, access), access);

    public override Span<byte> GetSpan()
        => new(Pin(0).Pointer, Length);

    /// <seealso cref="MemoryMappedViewAccessor.Flush"/>
    public void Flush()
        => _accessor.Flush();

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if (_pointer == null)
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
        return new MemoryHandle(_pointer + _accessor.PointerOffset + elementIndex, pinnable: this);
    }

    public override void Unpin()
    { }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_pointer != null)
            {
                _pointer = null;
                _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            }
            if (!_leaveOpen)
                _accessor.Dispose();
            _file?.Dispose();
        }
    }
}
