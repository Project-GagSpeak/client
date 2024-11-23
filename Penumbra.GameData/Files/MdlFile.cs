using Lumina.Data;
using Lumina.Extensions;
using static Lumina.Data.Parsing.MdlStructs;
using MeshStruct = Penumbra.GameData.Files.ModelStructs.MeshStruct;

// ReSharper disable FieldCanBeMadeReadOnly.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Penumbra.GameData.Files;

public partial class MdlFile : IWritable
{
    public const int  V5             = 0x01000005;
    public const int  V6             = 0x01000006;
    public const uint NumVertices    = 17;
    public const uint FileHeaderSize = 0x44;

    // Refers to string, thus not Lumina struct.
    public struct Shape
    {
        public string   ShapeName = string.Empty;
        public ushort[] ShapeMeshStartIndex;
        public ushort[] ShapeMeshCount;

        public Shape(ShapeStruct data, uint[] offsets, string[] strings)
        {
            var idx = offsets.AsSpan().IndexOf(data.StringOffset);
            ShapeName           = idx >= 0 ? strings[idx] : string.Empty;
            ShapeMeshStartIndex = data.ShapeMeshStartIndex;
            ShapeMeshCount      = data.ShapeMeshCount;
        }
    }

    // Raw data to write back.
    public uint   Version = 0x01000005;
    public float  Radius;
    public float  ModelClipOutDistance;
    public float  ShadowClipOutDistance;
    public byte   BgChangeMaterialIndex;
    public byte   BgCrestChangeMaterialIndex;
    public ushort CullingGridCount;
    public byte   Flags3;
    public byte   Unknown6;
    public ushort Unknown8;
    public ushort Unknown9;

    // Offsets are stored relative to RuntimeSize instead of file start.
    public uint[] VertexOffset = [0, 0, 0];
    public uint[] IndexOffset  = [0, 0, 0];

    public uint[] VertexBufferSize = [0, 0, 0];
    public uint[] IndexBufferSize  = [0, 0, 0];
    public byte   LodCount;
    public bool   EnableIndexBufferStreaming;
    public bool   EnableEdgeGeometry;

    public ModelFlags1 Flags1;
    public ModelFlags2 Flags2;

    public BoundingBoxStruct BoundingBoxes            = EmptyBoundingBox;
    public BoundingBoxStruct ModelBoundingBoxes       = EmptyBoundingBox;
    public BoundingBoxStruct WaterBoundingBoxes       = EmptyBoundingBox;
    public BoundingBoxStruct VerticalFogBoundingBoxes = EmptyBoundingBox;

    public VertexDeclarationStruct[]      VertexDeclarations     = [];
    public ElementIdStruct[]              ElementIds             = [];
    public MeshStruct[]                   Meshes                 = [];
    public ModelStructs.BoneTableStruct[] BoneTables             = [];
    public BoundingBoxStruct[]            BoneBoundingBoxes      = [];
    public SubmeshStruct[]                SubMeshes              = [];
    public ShapeMeshStruct[]              ShapeMeshes            = [];
    public ShapeValueStruct[]             ShapeValues            = [];
    public TerrainShadowMeshStruct[]      TerrainShadowMeshes    = [];
    public TerrainShadowSubmeshStruct[]   TerrainShadowSubMeshes = [];
    public LodStruct[]                    Lods                   = [];
    public ExtraLodStruct[]               ExtraLods              = [];
    public ushort[]                       SubMeshBoneMap         = [];

    // Strings are written in order
    public string[] Attributes = [];
    public string[] Bones      = [];
    public string[] Materials  = [];
    public Shape[]  Shapes     = [];

    // Raw, unparsed data.
    public byte[] RemainingData = [];

    public bool Valid { get; set; }

    public MdlFile()
    { }

    public MdlFile(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var r      = new LuminaBinaryReader(stream);

        var header = LoadModelFileHeader(r);
        LodCount         = header.LodCount;
        VertexBufferSize = header.VertexBufferSize;
        IndexBufferSize  = header.IndexBufferSize;
        VertexOffset     = header.VertexOffset;
        IndexOffset      = header.IndexOffset;

        var dataOffset = FileHeaderSize + header.RuntimeSize + header.StackSize;
        for (var i = 0; i < LodCount; ++i)
        {
            VertexOffset[i] -= dataOffset;
            IndexOffset[i]  -= dataOffset;
        }

        VertexDeclarations = new VertexDeclarationStruct[header.VertexDeclarationCount];
        for (var i = 0; i < header.VertexDeclarationCount; ++i)
            VertexDeclarations[i] = VertexDeclarationStruct.Read(r);

        var (offsets, strings) = LoadStrings(r);

        var modelHeader = LoadModelHeader(r);
        ElementIds = new ElementIdStruct[modelHeader.ElementIdCount];
        for (var i = 0; i < modelHeader.ElementIdCount; i++)
            ElementIds[i] = ElementIdStruct.Read(r);

        Lods = new LodStruct[3];
        for (var i = 0; i < 3; i++)
        {
            var lod = r.ReadStructure<LodStruct>();
            if (i < LodCount)
            {
                lod.VertexDataOffset -= dataOffset;
                lod.IndexDataOffset  -= dataOffset;
            }

            Lods[i] = lod;
        }

        ExtraLods = (modelHeader.Flags2 & ModelFlags2.ExtraLodEnabled) != 0
            ? r.ReadStructuresAsArray<ExtraLodStruct>(3)
            : [];

        Meshes = new MeshStruct[modelHeader.MeshCount];
        for (var i = 0; i < modelHeader.MeshCount; i++)
            Meshes[i] = MeshStruct.Read(r);

        Attributes = new string[modelHeader.AttributeCount];
        for (var i = 0; i < modelHeader.AttributeCount; ++i)
        {
            var offset    = r.ReadUInt32();
            var stringIdx = offsets.AsSpan().IndexOf(offset);
            Attributes[i] = stringIdx >= 0 ? strings[stringIdx] : string.Empty;
        }

        TerrainShadowMeshes    = r.ReadStructuresAsArray<TerrainShadowMeshStruct>(modelHeader.TerrainShadowMeshCount);
        SubMeshes              = r.ReadStructuresAsArray<SubmeshStruct>(modelHeader.SubmeshCount);
        TerrainShadowSubMeshes = r.ReadStructuresAsArray<TerrainShadowSubmeshStruct>(modelHeader.TerrainShadowSubmeshCount);

        Materials = new string[modelHeader.MaterialCount];
        for (var i = 0; i < modelHeader.MaterialCount; ++i)
        {
            var offset    = r.ReadUInt32();
            var stringIdx = offsets.AsSpan().IndexOf(offset);
            Materials[i] = stringIdx >= 0 ? strings[stringIdx] : string.Empty;
        }

        Bones = new string[modelHeader.BoneCount];
        for (var i = 0; i < modelHeader.BoneCount; ++i)
        {
            var offset    = r.ReadUInt32();
            var stringIdx = offsets.AsSpan().IndexOf(offset);
            Bones[i] = stringIdx >= 0 ? strings[stringIdx] : string.Empty;
        }

        BoneTables = new ModelStructs.BoneTableStruct[modelHeader.BoneTableCount];
        if (Version is V5)
        {
            for (var i = 0; i < modelHeader.BoneTableCount; i++)
                BoneTables[i] = ModelStructs.BoneTableStruct.ReadV5(r);
        }
        else if (Version is V6)
        {
            for (var i = 0; i < modelHeader.BoneTableCount; i++)
                BoneTables[i] = ModelStructs.BoneTableStruct.ReadV6(r);
            r.Position += modelHeader.BoneTableArrayCountTotal * 2;
        }


        Shapes = new Shape[modelHeader.ShapeCount];
        for (var i = 0; i < modelHeader.ShapeCount; i++)
            Shapes[i] = new Shape(ShapeStruct.Read(r), offsets, strings);

        ShapeMeshes = r.ReadStructuresAsArray<ShapeMeshStruct>(modelHeader.ShapeMeshCount);
        ShapeValues = r.ReadStructuresAsArray<ShapeValueStruct>(modelHeader.ShapeValueCount);

        var submeshBoneMapSize = r.ReadUInt32();
        SubMeshBoneMap = r.ReadStructures<ushort>((int)submeshBoneMapSize / 2).ToArray();

        var paddingAmount = r.ReadByte();
        r.Seek(r.BaseStream.Position + paddingAmount);

        // Dunno what this first one is for?
        BoundingBoxes            = BoundingBoxStruct.Read(r);
        ModelBoundingBoxes       = BoundingBoxStruct.Read(r);
        WaterBoundingBoxes       = BoundingBoxStruct.Read(r);
        VerticalFogBoundingBoxes = BoundingBoxStruct.Read(r);
        BoneBoundingBoxes        = new BoundingBoxStruct[modelHeader.BoneCount];
        for (var i = 0; i < modelHeader.BoneCount; i++)
            BoneBoundingBoxes[i] = BoundingBoxStruct.Read(r);

        var runtimePadding = header.RuntimeSize + FileHeaderSize + header.StackSize - r.BaseStream.Position;
        if (runtimePadding > 0)
            r.ReadBytes((int)runtimePadding);
        RemainingData = r.ReadBytes((int)(r.BaseStream.Length - r.BaseStream.Position));
        Valid         = true;
    }

    public bool ConvertV5ToV6()
    {
        if (Version != V5 || !Valid)
            return false;

        Version = V6;
        return true;
    }

    private ModelFileHeader LoadModelFileHeader(LuminaBinaryReader r)
    {
        var header = ModelFileHeader.Read(r);
        Version                    = header.Version;
        EnableIndexBufferStreaming = header.EnableIndexBufferStreaming;
        EnableEdgeGeometry         = header.EnableEdgeGeometry;
        return header;
    }

    private ModelStructs.ModelHeader LoadModelHeader(BinaryReader r)
    {
        var modelHeader = r.ReadStructure<ModelStructs.ModelHeader>();
        Radius                     = modelHeader.Radius;
        Flags1                     = modelHeader.Flags1;
        Flags2                     = modelHeader.Flags2;
        ModelClipOutDistance       = modelHeader.ModelClipOutDistance;
        ShadowClipOutDistance      = modelHeader.ShadowClipOutDistance;
        CullingGridCount           = modelHeader.CullingGridCount;
        Flags3                     = modelHeader.Flags3;
        Unknown6                   = modelHeader.Unknown6;
        Unknown8                   = modelHeader.Unknown8;
        Unknown9                   = modelHeader.Unknown9;
        BgChangeMaterialIndex      = modelHeader.BGChangeMaterialIndex;
        BgCrestChangeMaterialIndex = modelHeader.BGCrestChangeMaterialIndex;

        return modelHeader;
    }

    private static (uint[], string[]) LoadStrings(BinaryReader r)
    {
        var stringCount = r.ReadUInt16();
        r.ReadUInt16();
        var stringSize = (int)r.ReadUInt32();
        var stringData = r.ReadBytes(stringSize);
        var start      = 0;
        var strings    = new string[stringCount];
        var offsets    = new uint[stringCount];
        for (var i = 0; i < stringCount; ++i)
        {
            var span = stringData.AsSpan(start);
            var idx  = span.IndexOf((byte)'\0');
            strings[i] = Encoding.UTF8.GetString(span[..idx]);
            offsets[i] = (uint)start;
            start      = start + idx + 1;
        }

        return (offsets, strings);
    }

    public unsafe uint StackSize
        => (uint)(VertexDeclarations.Length * NumVertices * sizeof(VertexElement));

    public enum VertexType
    {
        Single1 = 0,
        Single2 = 1,
        Single3 = 2,
        Single4 = 3,

        // Unk4  = 4,
        UByte4  = 5,
        Short2  = 6,
        Short4  = 7,
        NByte4  = 8,
        NShort2 = 9,
        NShort4 = 10,

        // Unk11 = 11,
        // Unk12 = 12
        Half2 = 13,
        Half4 = 14,

        // Unk15 = 15
        UShort2 = 16,
        UShort4 = 17,
    }

    public enum VertexUsage
    {
        Position     = 0,
        BlendWeights = 1,
        BlendIndices = 2,
        Normal       = 3,
        UV           = 4,
        Tangent2     = 5,
        Tangent1     = 6,
        Color        = 7,
    }

    public static BoundingBoxStruct EmptyBoundingBox = new()
    {
        Min = [0f, 0f, 0f, 0f],
        Max = [0f, 0f, 0f, 0f],
    };
}
