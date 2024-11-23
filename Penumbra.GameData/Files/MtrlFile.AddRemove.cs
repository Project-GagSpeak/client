using Lumina.Data.Parsing;
using OtterGui;
using Penumbra.GameData.Data;

namespace Penumbra.GameData.Files;

public partial class MtrlFile
{
    public const string DummyTexturePath    = GamePaths.Tex.DummyPath;
    public const uint   DefaultSamplerFlags = 0x000F8340u;

    public int FindConstant(uint id)
        => ShaderPackage.Constants.IndexOf(c => c.Id == id);

    public int FindOrAddConstant(uint id, ShpkFile shpk)
    {
        if (UtilityFunctions.FindIndex(ShaderPackage.Constants, c => c.Id == id, out var idx))
            return idx;

        var shpkParam = shpk.GetMaterialParamById(id);
        if (!shpkParam.HasValue)
            throw new ArgumentException("Material constant not found in shader package");

        var offset = ShaderPackage.ShaderValues.Length;
        if (offset >= 0x10000)
            throw new InvalidOperationException("Constant capacity exceeded");

        Array.Resize(ref ShaderPackage.ShaderValues, ShaderPackage.ShaderValues.Length + shpkParam.Value.ByteSize);

        var newConstant = new Constant
        {
            Id = id,
            ByteOffset = (ushort)offset,
            ByteSize = shpkParam.Value.ByteSize,
        };
        var newI = ShaderPackage.Constants.Length;
        ShaderPackage.Constants = ShaderPackage.Constants.AddItem(newConstant);

        shpk.GetMaterialParamDefault<byte>(shpkParam.Value).CopyTo(GetConstantValue<byte>(newConstant));

        return newI;
    }

    public ref Constant GetOrAddConstant(uint id, ShpkFile shpk, out int i)
    {
        i = FindOrAddConstant(id, shpk);
        return ref ShaderPackage.Constants[i];
    }

    public ref Constant GetOrAddConstant(uint id, ShpkFile shpk)
        => ref GetOrAddConstant(id, shpk, out _);

    public int FindSampler(uint id)
        => ShaderPackage.Samplers.IndexOf(c => c.SamplerId == id);

    public int FindOrAddSampler(uint id, string defaultTexture)
    {
        if (UtilityFunctions.FindIndex(ShaderPackage.Samplers, c => c.SamplerId == id, out var idx))
            return idx;

        var newTextureI = Textures.Length;
        if (newTextureI >= 0x100)
            throw new InvalidOperationException("Sampler capacity exceeded");

        Textures = Textures.AddItem(new Texture
        {
            Path  = defaultTexture.Length > 0 ? defaultTexture : DummyTexturePath,
            Flags = 0,
        });

        var newI = ShaderPackage.Samplers.Length;
        ShaderPackage.Samplers = ShaderPackage.Samplers.AddItem(new Sampler
        {
            Flags        = DefaultSamplerFlags,
            SamplerId    = id,
            TextureIndex = (byte)newTextureI,
        });

        return newI;
    }

    public ref Sampler GetOrAddSampler(uint id, string defaultTexture, out int i)
    {
        i = FindOrAddSampler(id, defaultTexture);
        return ref ShaderPackage.Samplers[i];
    }

    public ref Sampler GetOrAddSampler(uint id, string defaultTexture)
        => ref GetOrAddSampler(id, defaultTexture, out _);

    public int FindShaderKey(uint category)
        => ShaderPackage.ShaderKeys.IndexOf(c => c.Category == category);

    public ShaderKey? GetShaderKey(uint category, out int i)
    {
        i = FindShaderKey(category);
        return i >= 0 ? ShaderPackage.ShaderKeys[i] : null;
    }

    public ShaderKey? GetShaderKey(uint category)
        => GetShaderKey(category, out _);

    public int FindOrAddShaderKey(uint category, uint defaultValue)
    {
        if (UtilityFunctions.FindIndex(ShaderPackage.ShaderKeys, c => c.Category == category, out var idx))
            return idx;

        var newI = ShaderPackage.ShaderKeys.Length;
        ShaderPackage.ShaderKeys = ShaderPackage.ShaderKeys.AddItem(new ShaderKey
        {
            Category = category,
            Value    = defaultValue,
        });

        return newI;
    }

    public ref ShaderKey GetOrAddShaderKey(uint category, uint defaultValue, out int i)
    {
        i = FindOrAddShaderKey(category, defaultValue);
        return ref ShaderPackage.ShaderKeys[i];
    }

    public ref ShaderKey GetOrAddShaderKey(uint category, uint defaultValue)
        => ref GetOrAddShaderKey(category, defaultValue, out _);

    public void GarbageCollect(ShpkFile? shpk, IReadOnlySet<uint> keepSamplers)
    {
        static bool ShallKeepConstant(MtrlFile mtrl, ShpkFile shpk, Constant constant)
        {
            if ((constant.ByteOffset & 0x3) != 0 || (constant.ByteSize & 0x3) != 0)
                return true;

            var shpkParam = shpk.GetMaterialParamById(constant.Id);
            if (!shpkParam.HasValue)
                return false;

            var value        = mtrl.GetConstantValue<byte>(constant);
            var defaultValue = shpk.GetMaterialParamDefault<byte>(shpkParam.Value);

            return defaultValue.Length > 0 ? !value.SequenceEqual(defaultValue) : value.ContainsAnyExcept((byte)0);
        }

        if (!keepSamplers.Contains(ShpkFile.TableSamplerId))
            Table = null;

        if (Table == null)
            DyeTable = null;

        for (var i = ShaderPackage.Samplers.Length; i-- > 0;)
        {
            if (!keepSamplers.Contains(ShaderPackage.Samplers[i].SamplerId))
                ShaderPackage.Samplers = ShaderPackage.Samplers.RemoveItems(i);
        }

        var samplersByTexture = GetSamplersByTexture(null);
        for (var i = samplersByTexture.Length; i-- > 0;)
        {
            if (samplersByTexture[i].MtrlSampler != null)
                continue;

            Textures = Textures.RemoveItems(i);
            for (var j = 0; j < ShaderPackage.Samplers.Length; ++j)
            {
                if (ShaderPackage.Samplers[j].TextureIndex > i)
                    --ShaderPackage.Samplers[j].TextureIndex;
            }
        }

        if (shpk == null)
            return;

        for (var i = ShaderPackage.ShaderKeys.Length; i-- > 0;)
        {
            var key     = ShaderPackage.ShaderKeys[i];
            var shpkKey = shpk.GetMaterialKeyById(key.Category);
            if (!shpkKey.HasValue || key.Value == shpkKey.Value.DefaultValue)
                ShaderPackage.ShaderKeys = ShaderPackage.ShaderKeys.RemoveItems(i);
        }

        var usedBytes = new BitArray(ShaderPackage.ShaderValues.Length, false);
        for (var i = ShaderPackage.Constants.Length; i-- > 0;)
        {
            var constant = ShaderPackage.Constants[i];
            if (ShallKeepConstant(this, shpk, constant))
            {
                var start = constant.ByteOffset;
                var end   = Math.Min(constant.ByteOffset + constant.ByteSize, ShaderPackage.ShaderValues.Length);
                for (var j = start; j < end; j++)
                    usedBytes[j] = true;
            }
            else
            {
                ShaderPackage.Constants = ShaderPackage.Constants.RemoveItems(i);
            }
        }

        for (var i = ShaderPackage.ShaderValues.Length; i-- > 0;)
        {
            if (usedBytes[i])
                continue;

            var end = i + 1;
            while (i >= 0 && !usedBytes[i])
                --i;
            ++i;
            ShaderPackage.ShaderValues = ShaderPackage.ShaderValues.RemoveItems(i, end - i);
            var byteStart = (ushort)i;
            var byteShift = (ushort)(end - i);
            for (var j = 0; j < ShaderPackage.Constants.Length; ++j)
            {
                if (ShaderPackage.Constants[j].ByteOffset > byteStart)
                    ShaderPackage.Constants[j].ByteOffset -= byteShift;
            }
        }
    }
}
