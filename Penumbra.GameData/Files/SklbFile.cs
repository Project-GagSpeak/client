using Lumina.Extensions;

namespace Penumbra.GameData.Files;

public class SklbFile
{
    private const int SklbMagic = 0x736B6C62; // "sklb"

    public SklbVersion Version;
    public byte[]      RawHeader;
    public byte[]      Skeleton;

    public SklbFile(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        var magic = reader.ReadUInt32();
        if (magic != SklbMagic)
            throw new InvalidDataException("Invalid sklb magic");

        Version = (SklbVersion)reader.ReadUInt32();

        var oldHeader = Version switch
        {
            SklbVersion.V1100 => true,
            SklbVersion.V1110 => true,
            SklbVersion.V1200 => true,
            SklbVersion.V1300 => false,
            SklbVersion.V1301 => false,
            _                 => throw new InvalidDataException($"Unknown version 0x{Version:X}"),
        };

        // Skeleton offset directly follows the layer offset.
        // TODO: Flesh out further fields of the file.
        uint skeletonOffset;
        if (oldHeader)
        {
            reader.ReadInt16();
            skeletonOffset = reader.ReadUInt16();
        }
        else
        {
            reader.ReadUInt32();
            skeletonOffset = reader.ReadUInt32();
        }

        reader.Seek(0L);
        RawHeader = reader.ReadBytes((int)skeletonOffset);
        Skeleton  = reader.ReadBytes((int)(reader.BaseStream.Length - skeletonOffset));
    }

    /// <summary> Version of a .sklb file. This is representative of the version specified in the SE-specific header, not the contained havok data. </summary>
    public enum SklbVersion
    {
        V1100 = 0x31313030, // "1100"
        V1110 = 0x31313130, // "1110"
        V1200 = 0x31323030, // "1200"
        V1300 = 0x31333030, // "1300"
        V1301 = 0x31333031, // "1301"
    }
}
