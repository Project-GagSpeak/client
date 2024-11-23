using Lumina.Data.Parsing;

namespace Penumbra.GameData.Files.ModelStructs;

public unsafe struct ModelHeader
{
    // MeshHeader
    public        float                  Radius;
    public        ushort                 MeshCount;
    public        ushort                 AttributeCount;
    public        ushort                 SubmeshCount;
    public        ushort                 MaterialCount;
    public        ushort                 BoneCount;
    public        ushort                 BoneTableCount;
    public        ushort                 ShapeCount;
    public        ushort                 ShapeMeshCount;
    public        ushort                 ShapeValueCount;
    public        byte                   LodCount;
    public        MdlStructs.ModelFlags1 Flags1;
    public        ushort                 ElementIdCount;
    public        byte                   TerrainShadowMeshCount;
    public        MdlStructs.ModelFlags2 Flags2;
    public        float                  ModelClipOutDistance;
    public        float                  ShadowClipOutDistance;
    public        ushort                 CullingGridCount;
    public        ushort                 TerrainShadowSubmeshCount;
    public        byte                   Flags3;
    public        byte                   BGChangeMaterialIndex;
    public        byte                   BGCrestChangeMaterialIndex;
    public        byte                   Unknown6;
    public        ushort                 BoneTableArrayCountTotal;
    public        ushort                 Unknown8;
    public        ushort                 Unknown9;
    private fixed byte                   _padding[6];
}
