using Lumina.Data;

namespace Penumbra.GameData.Files.ModelStructs;

#pragma warning disable 0169
// ReSharper disable UnassignedField.Local
public unsafe struct MeshStruct
{
    public        ushort VertexCount;
    private       ushort _padding;
    public        uint   IndexCount;
    public        ushort MaterialIndex;
    public        ushort SubMeshIndex;
    public        ushort SubMeshCount;
    public        ushort BoneTableIndex;
    public        uint   StartIndex;
    private fixed uint   _vertexBufferOffset[3];
    private fixed byte   _vertexBufferStride[3];
    private       byte   _vertexStreamCountByte;

    public uint VertexBufferOffset1
    {
        get => _vertexBufferOffset[0];
        set => _vertexBufferOffset[0] = value;
    }

    public uint VertexBufferOffset2
    {
        get => _vertexBufferOffset[1];
        set => _vertexBufferOffset[1] = value;
    }

    public uint VertexBufferOffset3
    {
        get => _vertexBufferOffset[2];
        set => _vertexBufferOffset[2] = value;
    }

    public byte VertexBufferStride1
    {
        get => _vertexBufferStride[0];
        set => _vertexBufferStride[0] = value;
    }

    public byte VertexBufferStride2
    {
        get => _vertexBufferStride[1];
        set => _vertexBufferStride[1] = value;
    }

    public byte VertexBufferStride3
    {
        get => _vertexBufferStride[2];
        set => _vertexBufferStride[2] = value;
    }

    public uint VertexBufferOffset(int idx)
        => _vertexBufferOffset[idx];

    public byte VertexBufferStride(int idx)
        => _vertexBufferStride[idx];

    public byte VertexStreamCount
    {
        get => (byte)(_vertexStreamCountByte & 3);
        set => _vertexStreamCountByte = (byte)((value & 3) | VertexStreamCountRemainder);
    }

    public byte VertexStreamCountRemainder
        => (byte)(_vertexStreamCountByte & ~3);

    public static MeshStruct Read(LuminaBinaryReader br)
        => br.ReadStructure<MeshStruct>();

    public void Write(BinaryWriter w)
    {
        w.Write(VertexCount);
        w.Write((ushort)0); // padding
        w.Write(IndexCount);
        w.Write(MaterialIndex);
        w.Write(SubMeshIndex);
        w.Write(SubMeshCount);
        w.Write(BoneTableIndex);
        w.Write(StartIndex);
        w.Write(_vertexBufferOffset[0]);
        w.Write(_vertexBufferOffset[1]);
        w.Write(_vertexBufferOffset[2]);
        w.Write(_vertexBufferStride[0]);
        w.Write(_vertexBufferStride[1]);
        w.Write(_vertexBufferStride[2]);
        w.Write(_vertexStreamCountByte);
    }
}

#pragma warning restore 0169
#pragma warning restore 0649
