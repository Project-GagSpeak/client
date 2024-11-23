using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files;

public partial class PbdFile : IWritable
{
    public struct Deformer
    {
        public GenderRace     GenderRace;
        public short          TreeEntryIndex;
        public float          UnkScale;
        public RacialDeformer RacialDeformer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TreeEntry
    {
        public short ParentIndex;
        public short FirstChildIndex;
        public short NextSiblingIndex;
        public short DeformerIndex;
    }

    public Deformer[]  Deformers;
    public TreeEntry[] RacialTree;

    public Deformer this[GenderRace genderRace]
    {
        get
        {
            foreach (var deformer in Deformers)
            {
                if (deformer.GenderRace == genderRace)
                    return deformer;
            }

            throw new IndexOutOfRangeException();
        }
    }

    public PbdFile(ReadOnlySpan<byte> data)
    {
        var reader     = new SpanBinaryReader(data);
        var entryCount = reader.ReadInt32();

        Deformers = new Deformer[entryCount];
        for (var i = 0; i < entryCount; ++i)
        {
            Deformers[i].GenderRace     = (GenderRace)reader.ReadUInt16();
            Deformers[i].TreeEntryIndex = reader.ReadInt16();
            var offset = reader.ReadInt32();
            Deformers[i].UnkScale       = reader.Read<float>();
            Deformers[i].RacialDeformer = offset == 0 ? new RacialDeformer() : new RacialDeformer(data[offset..]);
        }

        RacialTree = new TreeEntry[entryCount];
        for (var i = 0; i < entryCount; ++i)
            RacialTree[i] = reader.Read<TreeEntry>();
    }

    /// <summary> Gets the parent gender/race according to this PBD file. </summary>
    /// <remarks> In most cases, this will be equivalent to <see cref="RaceEnumExtensions.Fallback(GenderRace)"/>. </remarks>
    public GenderRace GetParent(GenderRace genderRace)
        => GetParent(this[genderRace]).GenderRace;

    public Deformer GetParent(Deformer deformer)
    {
        var treeEntry = RacialTree[deformer.TreeEntryIndex];

        if (treeEntry.ParentIndex < 0)
            throw new ArgumentException($"{deformer.GenderRace} is the root gender/race according to this PBD file");

        var parent = RacialTree[treeEntry.ParentIndex];
        return Deformers[parent.DeformerIndex];
    }

    public RacialDeformer GetRacialDeformer(GenderRace skeletonGenderRace, GenderRace modelGenderRace)
    {
        if (skeletonGenderRace == modelGenderRace)
            return new RacialDeformer();

        var skeletonDeformer = this[skeletonGenderRace];
        var result           = skeletonDeformer.RacialDeformer.Clone();

        for (var deformer = GetParent(skeletonDeformer); deformer.GenderRace != modelGenderRace; deformer = GetParent(deformer))
            result.Append(deformer.RacialDeformer, false);

        return result;
    }
}
