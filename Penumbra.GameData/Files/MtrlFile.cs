using Lumina.Data.Parsing;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Files.StainMapStructs;
using Penumbra.GameData.Files.Utility;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Files;

public partial class MtrlFile : IWritable, ICloneable
{
    public readonly uint Version;

    public bool Valid
        => CheckTextures();

    public Texture[]         Textures;
    public AttributeSet[]    UvSets;
    public AttributeSet[]    ColorSets;
    public IColorTable?      Table;
    public IColorDyeTable?   DyeTable;
    public ShaderPackageData ShaderPackage;
    public byte[]            AdditionalData;

    public TableFlags TableFlags
    {
        get => AdditionalData.Length switch
        {
            0 => default,
            1 => new(AdditionalData[0]),
            2 => new(AdditionalData[0] | ((uint)AdditionalData[1] << 8)),
            3 => new(AdditionalData[0] | ((uint)AdditionalData[1] << 8) | ((uint)AdditionalData[1] << 16)),
            _ => GetTableFlagsRef(),
        };
        set
        {
            if (AdditionalData.Length < 4 && value == TableFlags)
                return;

            GetTableFlagsRef() = value;
        }
    }

    public bool IsDawntrail
        => TableFlags.IsDawntrail && Table is not LegacyColorTable && DyeTable is not LegacyColorDyeTable;

    private ref TableFlags GetTableFlagsRef()
    {
        if (AdditionalData.Length < 4)
            Array.Resize(ref AdditionalData, 4);

        return ref MemoryMarshal.Cast<byte, TableFlags>(AdditionalData)[0];
    }

    public bool MigrateToDawntrail()
    {
        if (IsDawntrail)
            return false;

        if (ShaderPackage.Name is "character.shpk")
            ShaderPackage.Name = "characterlegacy.shpk";

        if (Table is LegacyColorTable)
            Table = new ColorTable(Table);
        if (DyeTable is LegacyColorDyeTable)
            DyeTable = new ColorDyeTable(DyeTable);

        var normalSamplerIdx = FindSampler(ShpkFile.NormalSamplerId);
        if (normalSamplerIdx >= 0)
        {
            var normalSampler    = ShaderPackage.Samplers[normalSamplerIdx];
            var normalTexture    = Textures[normalSampler.TextureIndex];
            var indexPath        = normalTexture.Path.Replace("_norm", "_id").Replace("_n", "_id");
            ref var indexSampler = ref GetOrAddSampler(ShpkFile.IndexSamplerId, indexPath);
            ref var indexTexture = ref Textures[indexSampler.TextureIndex];
            indexSampler.Flags   = normalSampler.Flags;
            indexTexture.Path    = indexPath;
            indexTexture.Flags   = normalTexture.Flags;
        }

        UpdateFlags();

        return true;
    }

    public bool ApplyDye(StmFile<DyePack> stm, ReadOnlySpan<StainId> stainIds)
    {
        if (DyeTable == null || stainIds.Length == 0)
            return false;

        if (Table is ColorTable table && DyeTable is ColorDyeTable dyeTable)
            return table.ApplyDye(stm, stainIds, dyeTable);
        else
            return false;
    }

    public bool ApplyDye(StmFile<LegacyDyePack> stm, ReadOnlySpan<StainId> stainIds)
    {
        if (DyeTable == null || stainIds.Length == 0)
            return false;

        if (Table is ColorTable table && DyeTable is ColorDyeTable dyeTable)
            return table.ApplyDye(stm, stainIds, dyeTable);
        else if (Table is LegacyColorTable legacyTable && DyeTable is LegacyColorDyeTable legacyDyeTable)
            return legacyTable.ApplyDye(stm, stainIds, legacyDyeTable);
        else
            return false;
    }

    public bool ApplyDyeToRow(StmFile<DyePack> stm, ReadOnlySpan<StainId> stainIds, int rowIdx)
    {
        if (DyeTable == null || rowIdx < 0 || rowIdx >= DyeTable.Height || stainIds.Length == 0)
            return false;

        if (Table is ColorTable table && DyeTable is ColorDyeTable dyeTable)
            return table.ApplyDyeToRow(stm, stainIds, rowIdx, dyeTable[rowIdx]);
        else
            return false;
    }

    public bool ApplyDyeToRow(StmFile<LegacyDyePack> stm, ReadOnlySpan<StainId> stainIds, int rowIdx)
    {
        if (DyeTable == null || rowIdx < 0 || rowIdx >= DyeTable.Height || stainIds.Length == 0)
            return false;

        if (Table is ColorTable table && DyeTable is ColorDyeTable dyeTable)
            return table.ApplyDyeToRow(stm, stainIds, rowIdx, dyeTable[rowIdx]);
        else if (Table is LegacyColorTable legacyTable && DyeTable is LegacyColorDyeTable legacyDyeTable)
            return legacyTable.ApplyDyeToRow(stm, stainIds, rowIdx, legacyDyeTable[rowIdx]);
        else
            return false;
    }

    public Span<T> GetConstantValue<T>(Constant constant) where T : struct
    {
        if (constant.ByteOffset + constant.ByteSize > ShaderPackage.ShaderValues.Length)
            return null;

        return MemoryMarshal.Cast<byte, T>(ShaderPackage.ShaderValues.AsSpan().Slice(constant.ByteOffset, constant.ByteSize));
    }

    public (Sampler? MtrlSampler, ShpkFile.Resource? ShpkSampler)[] GetSamplersByTexture(ShpkFile? shpk)
    {
        var samplers = Array.ConvertAll<Texture, (Sampler?, ShpkFile.Resource?)>(Textures, _ => (null, null));
        foreach (var sampler in ShaderPackage.Samplers)
            samplers[sampler.TextureIndex] = (sampler, shpk?.GetSamplerById(sampler.SamplerId));

        return samplers;
    }

    public MtrlFile(byte[] data)
        : this((ReadOnlySpan<byte>)data)
    { }

    public MtrlFile(ReadOnlySpan<byte> data)
    {
        var r = new SpanBinaryReader(data);

        Version = r.ReadUInt32();
        r.ReadUInt16(); // file size
        var dataSetSize             = r.ReadUInt16();
        var stringTableSize         = r.ReadUInt16();
        var shaderPackageNameOffset = r.ReadUInt16();
        var textureCount            = r.ReadByte();
        var uvSetCount              = r.ReadByte();
        var colorSetCount           = r.ReadByte();
        var additionalDataSize      = r.ReadByte();

        Textures  = ReadTextureOffsets(ref r, textureCount, out var textureOffsets);
        UvSets    = ReadAttributeSetOffsets(ref r, uvSetCount,    out var uvOffsets);
        ColorSets = ReadAttributeSetOffsets(ref r, colorSetCount, out var colorOffsets);

        var strings = r.SliceFromHere(stringTableSize);
        for (var i = 0; i < textureCount; ++i)
            Textures[i].Path = strings.ReadString(textureOffsets[i]);

        for (var i = 0; i < uvSetCount; ++i)
            UvSets[i].Name = strings.ReadString(uvOffsets[i]);

        for (var i = 0; i < colorSetCount; ++i)
            ColorSets[i].Name = strings.ReadString(colorOffsets[i]);

        ShaderPackage.Name = strings.ReadString(shaderPackageNameOffset);

        AdditionalData = r.Read<byte>(additionalDataSize).ToArray();

        var dataSet    = r.SliceFromHere(dataSetSize);
        var tableFlags = TableFlags;
        if (tableFlags.HasTable)
        {
            Table = tableFlags.TableDimensionLogs switch
            {
                0 or 0x42 => LegacyColorTable.TryReadFrom(ref dataSet),
                0x53      => ColorTable.TryReadFrom(ref dataSet),
                var logs  => OpaqueColorTable.TryReadFrom(logs, ref dataSet),
            };
            if (tableFlags.HasDyeTable)
            {
                DyeTable = tableFlags.TableDimensionLogs switch
                {
                    0                   => LegacyColorDyeTable.TryReadFrom(ref dataSet),
                    >= 0x50 and <= 0x5F => ColorDyeTable.TryReadFrom(ref dataSet),
                    _                   => OpaqueColorDyeTable.TryReadFrom(tableFlags.TableHeightLog, ref dataSet),
                };
            }
        }

        var shaderValueListSize = r.ReadUInt16();
        var shaderKeyCount      = r.ReadUInt16();
        var constantCount       = r.ReadUInt16();
        var samplerCount        = r.ReadUInt16();
        ShaderPackage.Flags = r.ReadUInt32();

        ShaderPackage.ShaderKeys   = r.Read<ShaderKey>(shaderKeyCount).ToArray();
        ShaderPackage.Constants    = r.Read<Constant>(constantCount).ToArray();
        ShaderPackage.Samplers     = r.Read<Sampler>(samplerCount).ToArray();
        ShaderPackage.ShaderValues = r.Read<byte>(shaderValueListSize).ToArray();
    }

    private MtrlFile(MtrlFile original)
    {
        Version = original.Version;

        Textures       = (Texture[])original.Textures.Clone();
        UvSets         = (AttributeSet[])original.UvSets.Clone();
        ColorSets      = (AttributeSet[])original.ColorSets.Clone();
        Table          = original.Table;
        DyeTable       = original.DyeTable;
        ShaderPackage  = original.ShaderPackage.Clone();
        AdditionalData = (byte[])original.AdditionalData.Clone();
    }

    public MtrlFile Clone()
        => new(this);

    object ICloneable.Clone()
        => new MtrlFile(this);

    public void UpdateFlags()
    {
        TableFlags = TableFlags with
        {
            HasTable           = Table != null,
            HasDyeTable        = Table != null && DyeTable != null,
            TableDimensionLogs = Table?.DimensionLogs ?? 0,
        };
    }

    private static Texture[] ReadTextureOffsets(ref SpanBinaryReader r, int count, out ushort[] offsets)
    {
        var ret = new Texture[count];
        offsets = new ushort[count];
        for (var i = 0; i < count; ++i)
        {
            offsets[i]   = r.ReadUInt16();
            ret[i].Flags = r.ReadUInt16();
        }

        return ret;
    }

    private static AttributeSet[] ReadAttributeSetOffsets(ref SpanBinaryReader r, int count, out ushort[] offsets)
    {
        var ret = new AttributeSet[count];
        offsets = new ushort[count];
        for (var i = 0; i < count; ++i)
        {
            offsets[i]   = r.ReadUInt16();
            ret[i].Index = r.ReadUInt16();
        }

        return ret;
    }

    private bool CheckTextures()
        => Textures.All(texture => texture.Path.Contains('/'));

    public struct AttributeSet
    {
        public string Name;
        public ushort Index;
    }

    public struct Texture
    {
        public const ushort DX11Flag = 0x8000;

        public string Path;
        public ushort Flags;

        public bool DX11
        {
            get => (Flags & DX11Flag) != 0;
            set => Flags = value ? (ushort)(Flags | DX11Flag) : (ushort)(Flags & ~DX11Flag);
        }
    }

    public struct Constant
    {
        public uint   Id;
        public ushort ByteOffset;
        public ushort ByteSize;
    }

    public struct ShaderPackageData : ICloneable
    {
        public string      Name;
        public ShaderKey[] ShaderKeys;
        public Constant[]  Constants;
        public Sampler[]   Samplers;
        public byte[]      ShaderValues;
        public uint        Flags;

        public ShaderPackageData Clone()
            => new()
            {
                Name         = Name,
                ShaderKeys   = (ShaderKey[])ShaderKeys.Clone(),
                Constants    = (Constant[])Constants.Clone(),
                Samplers     = (Sampler[])Samplers.Clone(),
                ShaderValues = (byte[])ShaderValues.Clone(),
                Flags        = Flags,
            };

        object ICloneable.Clone()
            => Clone();
    }
}
