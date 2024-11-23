using Penumbra.GameData.Files;
using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Data;

/// <remarks> This type is, notably, part of the PBD file format. </remarks>
public sealed class RacialDeformer : ICloneable, IWritable
{
    public Dictionary<string, TransformMatrix> DeformMatrices { get; private set; } = [];

    public bool IsEmpty
        => DeformMatrices.Count == 0;

    private RacialDeformer(Dictionary<string, TransformMatrix> matrices)
        => DeformMatrices = matrices;

    public RacialDeformer()
    { }

    public RacialDeformer(ReadOnlySpan<byte> data)
    {
        var reader = new SpanBinaryReader(data);
        var bones  = new string[reader.ReadInt32()];

        // Read strings by offset.
        for (var i = 0; i < bones.Length; i++)
            bones[i] = reader.ReadString(reader.ReadUInt16());

        // Align padding if necessary.
        if ((bones.Length & 1) != 0)
            reader.ReadUInt16();

        // Read matrices.
        foreach (var bone in bones)
            DeformMatrices.Add(bone, reader.Read<TransformMatrix>());
    }

    public RacialDeformer Clone()
        => new(DeformMatrices.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

    object ICloneable.Clone()
        => Clone();

    public void IntersectWith(RacialDeformer deformer)
    {
        DeformMatrices = DeformMatrices.Where(kvp => deformer.DeformMatrices.ContainsKey(kvp.Key))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void AddDefaults(RacialDeformer deformer)
    {
        foreach (var (bone, matrix) in deformer.DeformMatrices)
            DeformMatrices.TryAdd(bone, matrix);
    }

    public void Append(RacialDeformer deformer, bool addMissing = true)
    {
        foreach (var (bone, matrix) in deformer.DeformMatrices)
        {
            if (DeformMatrices.TryGetValue(bone, out var thisMatrix))
                DeformMatrices[bone] = thisMatrix.Append(in matrix);
            else if (addMissing)
                DeformMatrices.Add(bone, matrix);
        }
    }

    public void Prepend(RacialDeformer deformer, bool addMissing = true)
    {
        foreach (var (bone, matrix) in deformer.DeformMatrices)
        {
            if (DeformMatrices.TryGetValue(bone, out var thisMatrix))
                DeformMatrices[bone] = thisMatrix.Prepend(in matrix);
            else if (addMissing)
                DeformMatrices.Add(bone, matrix);
        }
    }

    public void Invert()
        => DeformMatrices = DeformMatrices.Select(kvp => (kvp.Key, kvp.Value.Invert())).ToDictionary(kvp => kvp.Key, kvp => kvp.Item2);

    public bool Valid
        => true;

    public void Write(Stream stream)
    {
        var names = new StringPool();

        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(DeformMatrices.Count);
            var needsPadding = (DeformMatrices.Count & 1) != 0;
            var offset       = (needsPadding ? 6 : 4) + DeformMatrices.Count * (2 + 12 * 4);
            foreach (var bone in DeformMatrices.Keys)
                writer.Write((ushort)(names.FindOrAddString(bone).Offset + offset));
            // Add padding if necessary.
            if (needsPadding)
                writer.Write((ushort)0);

            foreach (var matrix in DeformMatrices.Values)
                writer.Write(matrix);
        }

        names.WriteTo(stream);

        // Add padding if necessary.
        if ((stream.Length & 3) != 0)
            stream.Write(new byte[4 - (stream.Length & 3)]);
    }

    public byte[] Write()
    {
        using var mem = new MemoryStream();
        Write(mem);
        return mem.ToArray();
    }
}
