namespace Penumbra.GameData.Files.Utility;

public class StringPool : IDisposable
{
    public readonly MemoryStream Data;
    public readonly List<int>    StartingOffsets;

    public int Length
        => (int)Data.Length;

    public StringPool()
    {
        Data            = new MemoryStream();
        StartingOffsets = new List<int>();
    }

    public StringPool(ReadOnlySpan<byte> initialData)
    {
        Data = new MemoryStream();
        Data.Write(initialData);
        StartingOffsets = new List<int>
        {
            0,
        };
        for (var i = 0; i < initialData.Length; ++i)
        {
            if (initialData[i] == 0)
                StartingOffsets.Add(i + 1);
        }

        if (StartingOffsets[^1] == initialData.Length)
            StartingOffsets.RemoveAt(StartingOffsets.Count - 1);
        else
            Data.WriteByte(0);
    }

    public void Dispose()
        => Data.Dispose();

    public ReadOnlySpan<byte> AsSpan()
        => Data.GetBuffer().AsSpan()[..(int)Data.Length];

    public void WriteTo(Stream stream)
        => Data.WriteTo(stream);

    public string GetString(int offset, int length)
        => Encoding.UTF8.GetString(AsSpan().Slice(offset, length));

    public string GetNullTerminatedString(int offset)
    {
        var str  = AsSpan()[offset..];
        var size = str.IndexOf((byte)0);
        if (size >= 0)
            str = str[..size];
        return Encoding.UTF8.GetString(str);
    }

    public (int Offset, int Length) FindOrAddString(string str)
    {
        var dataSpan = AsSpan();
        var bytes    = Encoding.UTF8.GetBytes(str);
        foreach (var offset in StartingOffsets)
        {
            if (offset + bytes.Length > Data.Length)
                break;

            var strSpan = dataSpan[offset..];
            var match   = true;
            for (var i = 0; i < bytes.Length; ++i)
            {
                if (strSpan[i] != bytes[i])
                {
                    match = false;
                    break;
                }
            }

            if (match && strSpan[bytes.Length] == 0)
                return (offset, bytes.Length);
        }

        Data.Seek(0L, SeekOrigin.End);
        var newOffset = (int)Data.Position;
        StartingOffsets.Add(newOffset);
        Data.Write(bytes);
        Data.WriteByte(0);
        return (newOffset, bytes.Length);
    }

    public int FindOrAddString(ReadOnlySpan<byte> str)
    {
        var dataSpan = AsSpan();
        foreach (var offset in StartingOffsets)
        {
            if (offset + str.Length > Data.Length)
                break;

            var strSpan = dataSpan[offset..];
            var match   = true;
            for (var i = 0; i < str.Length; ++i)
            {
                if (strSpan[i] != str[i])
                {
                    match = false;
                    break;
                }
            }

            if (match && strSpan[str.Length] == 0)
                return offset;
        }

        Data.Seek(0L, SeekOrigin.End);
        var newOffset = (int)Data.Position;
        StartingOffsets.Add(newOffset);
        Data.Write(str);
        Data.WriteByte(0);
        return newOffset;
    }
}
