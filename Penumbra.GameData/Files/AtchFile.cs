using OtterGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.AtchStructs;
using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files;

public class AtchFile : IWritable
{
    public const int BitFieldSize = 32;

    public readonly List<AtchPoint> Points;

    public unsafe AtchFile(ReadOnlySpan<byte> data)
    {
        var r          = new SpanBinaryReader(data);
        var numPoints  = r.ReadUInt16();
        var numEntries = r.ReadUInt16();
        Points = new List<AtchPoint>(numPoints);
        for (var i = 0; i < numPoints; ++i)
            Points.Add(new AtchPoint { Type = (AtchType)r.ReadUInt32() });

        var bitfield = stackalloc ulong[BitFieldSize / 8];
        for (var i = 0; i < BitFieldSize / 8; ++i)
            bitfield[i] = r.ReadUInt64();

        for (var i = 0; i < numPoints; ++i)
        {
            var bitIdx   = i & 0x3F;
            var ulongIdx = i >> 6;
            Points[i].Accessory = ((bitfield[ulongIdx] >> bitIdx) & 1) == 1;
        }

        foreach (var entry in Points)
            entry.ReadStates(ref r, numEntries);
    }

    public AtchPoint? GetPoint(AtchType type)
        => Points.FirstOrDefault(p => p.Type == type);

    public ref AtchEntry GetEntry(AtchType type, ushort entryIndex)
    {
        if (GetPoint(type) is not { } point)
            throw new IndexOutOfRangeException();
        if (point.Entries.Length <= entryIndex)
            throw new IndexOutOfRangeException();

        return ref point.Entries[entryIndex];
    }

    public AtchFile Clone()
        => new(this);

    private AtchFile(AtchFile clone)
        => Points = clone.Points.Select(p => new AtchPoint(p)).ToList();

    public bool Valid
        => Points.Count > 0 && Points.All(e => e.Type != 0 && e.Entries.Length == Points[0].Entries.Length);

    public unsafe MemoryStream Write()
    {
        var       ms = new MemoryStream();
        using var w  = new BinaryWriter(ms, Encoding.UTF8, true);
        w.Write((ushort)Points.Count);
        if (Points.Count == 0)
        {
            w.Write((ushort)0);
            return ms;
        }

        var firstEntry = Points[0];
        w.Write((ushort)firstEntry.Entries.Length);
        foreach (var entry in Points)
            w.Write((uint)entry.Type);

        Span<byte> bitfield = stackalloc byte[BitFieldSize];
        foreach (var (entry, i) in Points.WithIndex())
        {
            var bitIdx  = i & 0x7;
            var byteIdx = i >> 3;
            if (entry.Accessory)
                bitfield[byteIdx] |= (byte)(1 << bitIdx);
        }

        w.Write(bitfield);

        var stringStart = ms.Position + 32 * Points.Count * firstEntry.Entries.Length;
        var pool        = new StringPool();
        foreach (var entry in Points)
        {
            if (entry.Entries.Length != firstEntry.Entries.Length)
                throw new Exception(
                    $".atch file is invalid: different number of entries in point {entry.Type} compared to first point {firstEntry.Type}.");

            foreach (var state in entry.Entries)
            {
                var offset = pool.FindOrAddString(state.Bone);
                w.Write((uint)(offset + stringStart));
                w.Write(state.Scale);
                w.Write(state.Offset.X);
                w.Write(state.Offset.Y);
                w.Write(state.Offset.Z);
                w.Write(state.Rotation.X);
                w.Write(state.Rotation.Y);
                w.Write(state.Rotation.Z);
            }
        }

        pool.WriteTo(ms);
        var remainder = ms.Position % 64;
        if (remainder != 0)
        {
            var padding = 64 - remainder;
            for (var i = 0; i < padding; ++i)
                ms.Write((byte)0);
        }

        return ms;
    }

    byte[] IWritable.Write()
    {
        using var ms = Write();
        return ms.ToArray();
    }
}
