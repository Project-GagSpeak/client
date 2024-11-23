using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files;

public partial class PbdFile
{
    public bool Valid
        => CheckValidity();

    public bool CheckValidity()
    {
        var entryCount = Deformers.Length;

        if (RacialTree.Length != entryCount)
            return false;

        for (var i = 0; i < entryCount; ++i)
        {
            var deformer = Deformers[i];
            if (deformer.TreeEntryIndex < 0 || deformer.TreeEntryIndex >= entryCount)
                return false;
            if (RacialTree[deformer.TreeEntryIndex].DeformerIndex != i)
                return false;
        }

        for (var i = 0; i < entryCount; ++i)
        {
            var treeEntry = RacialTree[i];
            if (treeEntry.ParentIndex < -1 || treeEntry.ParentIndex >= entryCount)
                return false;
            if (treeEntry.FirstChildIndex < -1 || treeEntry.FirstChildIndex >= entryCount)
                return false;
            if (treeEntry.NextSiblingIndex < -1 || treeEntry.NextSiblingIndex >= entryCount)
                return false;
        }

        var root = 0;
        while (RacialTree[root].ParentIndex >= 0)
            root = RacialTree[root].ParentIndex;

        var visited = 0;
        if (!CheckTreeIntegrity(root, -1, ref visited))
            return false;
        if (visited != entryCount)
            return false;

        return true;
    }

    private bool CheckTreeIntegrity(int index, int parentIndex, ref int visited)
    {
        var treeEntry = RacialTree[index];
        if (treeEntry.ParentIndex != parentIndex)
            return false;

        if (treeEntry.FirstChildIndex >= 0)
            if (!CheckTreeIntegrity(treeEntry.FirstChildIndex, index, ref visited))
                return false;

        if (treeEntry.NextSiblingIndex >= 0)
            if (!CheckTreeIntegrity(treeEntry.NextSiblingIndex, parentIndex, ref visited))
                return false;

        ++visited;
        return true;
    }

    public byte[] Write()
    {
        var deformerBlobs      = new byte[Deformers.Length][];
        var deformerOffsets    = new int[Deformers.Length];
        var nextDeformerOffset = 4 + 20 * Deformers.Length;
        for (var i = 0; i < Deformers.Length; ++i)
        {
            var deformer = Deformers[i].RacialDeformer;
            if (deformer.IsEmpty)
            {
                deformerBlobs[i]   = [];
                deformerOffsets[i] = 0;
            }
            else
            {
                deformerBlobs[i]   =  deformer.Write();
                deformerOffsets[i] =  nextDeformerOffset;
                nextDeformerOffset += deformerBlobs[i].Length;
            }
        }

        using var stream = new MemoryStream();

        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Deformers.Length);
            for (var i = 0; i < Deformers.Length; ++i)
            {
                var deformer = Deformers[i];
                writer.Write((ushort)deformer.GenderRace);
                writer.Write(deformer.TreeEntryIndex);
                writer.Write(deformerOffsets[i]);
                writer.Write(deformer.UnkScale);
            }

            foreach (var entry in RacialTree)
                writer.Write(entry);
        }

        foreach (var blob in deformerBlobs)
            stream.Write(blob);

        if ((stream.Length & 16) != 0)
            stream.Write(new byte[16 - (stream.Length & 15)]);

        return stream.ToArray();
    }
}
