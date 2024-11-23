using Penumbra.GameData.Files.Utility;

namespace Penumbra.GameData.Files;

public partial class MtrlFile
{
    public byte[] Write()
    {
        UpdateFlags();

        using var stream  = new MemoryStream();
        using var strings = new StringPool();
        using (var w = new BinaryWriter(stream))
        {
            const int materialHeaderSize = 4 + 2 + 2 + 2 + 2 + 1 + 1 + 1 + 1;

            w.BaseStream.Seek(materialHeaderSize, SeekOrigin.Begin);
            foreach (var texture in Textures)
            {
                w.Write((ushort)strings.FindOrAddString(texture.Path).Offset);
                w.Write(texture.Flags);
            }

            foreach (var set in UvSets)
            {
                w.Write((ushort)strings.FindOrAddString(set.Name).Offset);
                w.Write(set.Index);
            }

            foreach (var set in ColorSets)
            {
                w.Write((ushort)strings.FindOrAddString(set.Name).Offset);
                w.Write(set.Index);
            }

            var shaderPackageNameOffset = (ushort)strings.FindOrAddString(ShaderPackage.Name).Offset;

            strings.WriteTo(stream);

            w.Write(AdditionalData);
            var dataSetSize = 0;
            if (Table != null)
            {
                var span = Table.AsBytes();
                w.Write(span);
                dataSetSize += span.Length;
            }

            if (Table != null && DyeTable != null)
            {
                var span = DyeTable.AsBytes();
                w.Write(span);
                dataSetSize += span.Length;
            }

            w.Write((ushort)ShaderPackage.ShaderValues.Length);
            w.Write((ushort)ShaderPackage.ShaderKeys.Length);
            w.Write((ushort)ShaderPackage.Constants.Length);
            w.Write((ushort)ShaderPackage.Samplers.Length);
            w.Write(ShaderPackage.Flags);

            foreach (var key in ShaderPackage.ShaderKeys)
            {
                w.Write(key.Category);
                w.Write(key.Value);
            }

            foreach (var constant in ShaderPackage.Constants)
            {
                w.Write(constant.Id);
                w.Write(constant.ByteOffset);
                w.Write(constant.ByteSize);
            }

            foreach (var sampler in ShaderPackage.Samplers)
            {
                w.Write(sampler.SamplerId);
                w.Write(sampler.Flags);
                w.Write(sampler.TextureIndex);
                w.Write((ushort)0);
                w.Write((byte)0);
            }

            w.Write(ShaderPackage.ShaderValues);

            WriteHeader(w, (ushort)w.BaseStream.Position, dataSetSize, (ushort)strings.Length, shaderPackageNameOffset);
        }

        return stream.ToArray();
    }

    private void WriteHeader(BinaryWriter w, ushort fileSize, int dataSetSize, ushort stringPoolLength, ushort shaderPackageNameOffset)
    {
        w.BaseStream.Seek(0, SeekOrigin.Begin);
        w.Write(Version);
        w.Write(fileSize);
        w.Write((ushort)dataSetSize);
        w.Write(stringPoolLength);
        w.Write(shaderPackageNameOffset);
        w.Write((byte)Textures.Length);
        w.Write((byte)UvSets.Length);
        w.Write((byte)ColorSets.Length);
        w.Write((byte)AdditionalData.Length);
    }
}
