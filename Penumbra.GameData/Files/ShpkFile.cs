using Penumbra.GameData.Files.ShaderStructs;
using Penumbra.GameData.Files.Utility;
using DisassembledShader = Penumbra.GameData.Interop.DisassembledShader;
using ShaderKeyValueSet = Penumbra.GameData.Structs.SharedSet<uint, uint>;

namespace Penumbra.GameData.Files;

public partial class ShpkFile : IWritable
{
    private const uint ShPkMagic = 0x6B506853u; // bytes of ShPk
    private const uint Dx9Magic  = 0x00395844u; // bytes of DX9\0
    private const uint Dx11Magic = 0x31315844u; // bytes of DX11

    public const uint MaterialParamsConstantId = 0x64D12851u; // g_MaterialParameter is a cbuffer filled from the ad hoc section of the mtrl
    public const uint TableSamplerId           = 0x2005679Fu; // g_SamplerTable is a texture filled from the mtrl's color set
    public const uint NormalSamplerId          = 0x0C5EC1F1u; // g_SamplerNormal (suffix "_norm" or "_n") is the normal map
    public const uint IndexSamplerId           = 0x565F8FD8u; // g_SamplerIndex (suffix "_id") is the texture used to address g_SamplerTable

    public static readonly Name MaterialParamsConstantName = "g_MaterialParameter";
    public static readonly Name TableSamplerName           = "g_SamplerTable";
    public static readonly Name NormalSamplerName          = "g_SamplerNormal";
    public static readonly Name IndexSamplerName           = "g_SamplerIndex";

    public const uint SelectorMultiplier        = 31;
    public const uint SelectorInverseMultiplier = 3186588639; // 31 * 3186588639 = 23 << 32 + 1, iow they're modular inverses

    public const int MaxExhaustiveNodeAnalysisCombinations = 262144;

    public readonly bool Disassembled;

    public uint      Version;
    public bool      IsLegacy;
    public DxVersion DirectXVersion;
    public Shader[]  VertexShaders;
    public Shader[]  PixelShaders;
    public uint      MaterialParamsSize;

    /// <remarks>
    /// This is always null for legacy shaders.
    /// </remarks>
    public byte[]? MaterialParamsDefaults;

    public MaterialParam[] MaterialParams;
    public Resource[]      Constants;
    public Resource[]      Samplers;

    /// <remarks>
    /// When dealing with legacy shaders, this will always be empty, use <see cref="Samplers"/> instead.
    /// </remarks>
    public Resource[] Textures;

    public Resource[]                 Uavs;
    public Key[]                      SystemKeys;
    public Key[]                      SceneKeys;
    public Key[]                      MaterialKeys;
    public Key[]                      SubViewKeys;
    public ShaderKeyValueSet.Universe Passes;
    public Node[]                     Nodes;
    public Dictionary<uint, uint>     NodeSelectors;
    public byte[]                     AdditionalData;

    public  bool Valid { get; private set; }
    private bool _changed;

    public MaterialParam? GetMaterialParamById(uint id)
        => MaterialParams.FirstOrNull(m => m.Id == id);

    public Span<T> GetMaterialParamDefault<T>(MaterialParam param) where T : struct
    {
        if (MaterialParamsDefaults == null || param.ByteOffset >= MaterialParamsDefaults.Length)
            return [];

        var byteEnd = Math.Min(param.ByteOffset + param.ByteSize, MaterialParamsDefaults.Length);
        return MemoryMarshal.Cast<byte, T>(MaterialParamsDefaults.AsSpan(param.ByteOffset..byteEnd));
    }

    public Resource? GetConstantById(uint id)
        => Constants.FirstOrNull(c => c.Id == id);

    public Resource? GetConstantByName(string name)
        => Constants.FirstOrNull(c => c.Name == name);

    public Resource? GetSamplerById(uint id)
        => Samplers.FirstOrNull(s => s.Id == id);

    public Resource? GetSamplerByName(string name)
        => Samplers.FirstOrNull(s => s.Name == name);

    public Resource? GetTextureById(uint id)
        => Textures.FirstOrNull(s => s.Id == id);

    public Resource? GetTextureByName(string name)
        => Textures.FirstOrNull(s => s.Name == name);

    public Resource? GetUavById(uint id)
        => Uavs.FirstOrNull(u => u.Id == id);

    public Resource? GetUavByName(string name)
        => Uavs.FirstOrNull(u => u.Name == name);

    public Key? GetSystemKeyById(uint id)
        => SystemKeys.FirstOrNull(k => k.Id == id);

    public Key? GetSceneKeyById(uint id)
        => SceneKeys.FirstOrNull(k => k.Id == id);

    public Key? GetMaterialKeyById(uint id)
        => MaterialKeys.FirstOrNull(k => k.Id == id);

    public Node? GetNodeBySelector(uint selector)
    {
        if (NodeSelectors.TryGetValue(selector, out var index) && index < Nodes.Length)
            return Nodes[index];

        return null;
    }

    public ShpkFile(byte[] data, bool disassemble = false)
        : this((ReadOnlySpan<byte>)data, disassemble)
    { }

    public ShpkFile(ReadOnlySpan<byte> data, bool disassemble = false)
    {
        Disassembled = disassemble;

        var r = new SpanBinaryReader(data);

        if (r.ReadUInt32() != ShPkMagic)
            throw new InvalidDataException();

        Version = r.ReadUInt32();
        DirectXVersion = r.ReadUInt32() switch
        {
            Dx9Magic  => DxVersion.DirectX9,
            Dx11Magic => DxVersion.DirectX11,
            _         => throw new InvalidDataException(),
        };
        if (r.ReadUInt32() != data.Length)
            throw new InvalidDataException();

        var blobsOffset       = r.ReadUInt32();
        var stringsOffset     = r.ReadUInt32();
        var vertexShaderCount = r.ReadUInt32();
        var pixelShaderCount  = r.ReadUInt32();
        MaterialParamsSize = r.ReadUInt32();
        var materialParamCount  = r.ReadUInt16();
        var hasMatParamDefaults = r.ReadUInt16() != 0;
        var constantCount       = r.ReadUInt32();
        var samplerCount        = r.ReadUInt16();
        var textureCount        = r.ReadUInt16();
        var uavCount            = r.ReadUInt32();
        var systemKeyCount      = r.ReadUInt32();
        var sceneKeyCount       = r.ReadUInt32();
        var materialKeyCount    = r.ReadUInt32();
        var nodeCount           = r.ReadUInt32();
        var nodeAliasCount      = r.ReadUInt32();

        IsLegacy = !hasMatParamDefaults && textureCount == 0;

        var blobs   = data[(int)blobsOffset..(int)stringsOffset];
        var strings = new SpanBinaryReader(data[(int)stringsOffset..]);

        VertexShaders = ReadShaderArray(ref r, (int)vertexShaderCount, DisassembledShader.ShaderStage.Vertex, DirectXVersion, disassemble,
            IsLegacy,                          blobs,                  ref strings);
        PixelShaders = ReadShaderArray(ref r, (int)pixelShaderCount, DisassembledShader.ShaderStage.Pixel, DirectXVersion, disassemble,
            IsLegacy,                         blobs,                 ref strings);

        MaterialParams = r.Read<MaterialParam>(materialParamCount).ToArray();

        MaterialParamsDefaults = hasMatParamDefaults ? r.Read<byte>((int)MaterialParamsSize).ToArray() : null;

        Constants = ReadResourceArray(ref r, (int)constantCount, ref strings);
        Samplers  = ReadResourceArray(ref r, samplerCount,       ref strings);
        Textures  = ReadResourceArray(ref r, textureCount,       ref strings);
        Uavs      = ReadResourceArray(ref r, (int)uavCount,      ref strings);

        SystemKeys   = ReadKeyArray(ref r, (int)systemKeyCount);
        SceneKeys    = ReadKeyArray(ref r, (int)sceneKeyCount);
        MaterialKeys = ReadKeyArray(ref r, (int)materialKeyCount);

        var subViewKey1Default = r.ReadUInt32();
        var subViewKey2Default = r.ReadUInt32();

        SubViewKeys = new Key[]
        {
            new()
            {
                Id           = 1,
                DefaultValue = subViewKey1Default,
                Values       = [subViewKey1Default],
            },
            new()
            {
                Id           = 2,
                DefaultValue = subViewKey2Default,
                Values       = [subViewKey2Default],
            },
        };

        Passes = [];

        Nodes = ReadNodeArray(ref r, (int)nodeCount, SystemKeys.Length, SceneKeys.Length, MaterialKeys.Length, SubViewKeys.Length);

        NodeSelectors = new Dictionary<uint, uint>(Nodes.Length + (int)nodeAliasCount);
        for (var i = 0; i < Nodes.Length; ++i)
            NodeSelectors.TryAdd(Nodes[i].Selector, (uint)i);
        foreach (var alias in r.Read<NodeAlias>((int)nodeAliasCount))
            NodeSelectors.TryAdd(alias.Selector, alias.Node);

        AdditionalData = r.Read<byte>((int)(blobsOffset - r.Position)).ToArray(); // This should be empty, but just in case.

        UpdateUsed(null);
        UpdateKeyValues();

        Valid    = true;
        _changed = false;
    }

    /// <summary> Determines whether a ShPk file is a legacy one, while examining the least possible amount of data, for performance reasons. </summary>
    /// <param name="startOfData"> At least the 48 first bytes of the file. </param>
    /// <seealso cref="IsLegacy"/>
    public static bool FastIsLegacy(ReadOnlySpan<byte> startOfData)
    {
        const int hasMatParamDefaults = 19;
        const int textureCount        = 23;

        var asShorts = MemoryMarshal.Cast<byte, ushort>(startOfData);
        return asShorts[hasMatParamDefaults] == 0 && asShorts[textureCount] == 0;
    }

    /// <summary> Extract all resource names from a ShPk file, while examining the least possible amount of data, for performance reasons. </summary>
    /// <param name="data"> The bytes of the file. </param>
    /// <returns> The extracted resource names, indexed by their CRC32 hash (that's usually used as resource ID). </returns>
    public static IReadOnlyDictionary<uint, Name> FastExtractNames(ReadOnlySpan<byte> data)
    {
        var asInts        = MemoryMarshal.Cast<byte, uint>(data);
        var stringsOffset = asInts[5];
        var strings       = new SpanBinaryReader(data[(int)stringsOffset..]);
        var names         = new List<Name>();
        while (strings.Remaining > 0)
        {
            var str = strings.ReadByteString(strings.Position);
            if (str.Length > 0)
                names.Add(new Name(str));
            if (str.Length + 1 >= strings.Remaining)
                break;

            strings.Skip(str.Length + 1);
        }

        return names.Indexed();
    }

    public void UpdateResources()
    {
        if (!Disassembled)
            return;

        var constants = new Dictionary<uint, Resource>();
        var samplers  = new Dictionary<uint, Resource>();
        var textures  = new Dictionary<uint, Resource>();
        var uavs      = new Dictionary<uint, Resource>();

        static void CollectResources(Dictionary<uint, Resource> resources, Resource[] shaderResources, Func<uint, Resource?> getExistingById,
            DisassembledShader.ResourceType type)
        {
            foreach (var resource in shaderResources)
            {
                if (resources.TryGetValue(resource.Id, out var carry) && type != DisassembledShader.ResourceType.ConstantBuffer)
                    continue;

                var existing = getExistingById(resource.Id);
                resources[resource.Id] = new Resource
                {
                    Id = resource.Id,
                    Name = resource.Name,
                    IsTexture = resource.IsTexture,
                    Slot = existing?.Slot ?? (type == DisassembledShader.ResourceType.ConstantBuffer ? (ushort)65535 : (ushort)2),
                    Size = type == DisassembledShader.ResourceType.ConstantBuffer ? Math.Max(carry.Size, resource.Size) : existing?.Size ?? 0,
                    Used = null,
                    UsedDynamically = null,
                };
            }
        }

        foreach (var shader in VertexShaders)
        {
            CollectResources(constants, shader.Constants, GetConstantById, DisassembledShader.ResourceType.ConstantBuffer);
            CollectResources(samplers,  shader.Samplers,  GetSamplerById,  DisassembledShader.ResourceType.Sampler);
            CollectResources(textures,  shader.Textures,  GetTextureById,  DisassembledShader.ResourceType.Texture);
            CollectResources(uavs,      shader.Uavs,      GetUavById,      DisassembledShader.ResourceType.Uav);
        }

        foreach (var shader in PixelShaders)
        {
            CollectResources(constants, shader.Constants, GetConstantById, DisassembledShader.ResourceType.ConstantBuffer);
            CollectResources(samplers,  shader.Samplers,  GetSamplerById,  DisassembledShader.ResourceType.Sampler);
            CollectResources(textures,  shader.Textures,  GetTextureById,  DisassembledShader.ResourceType.Texture);
            CollectResources(uavs,      shader.Uavs,      GetUavById,      DisassembledShader.ResourceType.Uav);
        }

        Constants = constants.Values.ToArray();
        Samplers  = samplers.Values.ToArray();
        Textures  = textures.Values.ToArray();
        Uavs      = uavs.Values.ToArray();
        UpdateUsed(null);

        // Ceil required size to a multiple of 16 bytes.
        // Offsets can be skipped, MaterialParamsConstantId's size is the count.
        MaterialParamsSize = (GetConstantById(MaterialParamsConstantId)?.Size ?? 0u) << 4;
        foreach (var param in MaterialParams)
            MaterialParamsSize = Math.Max(MaterialParamsSize, (uint)param.ByteOffset + param.ByteSize);
        MaterialParamsSize = (MaterialParamsSize + 0xFu) & ~0xFu;

        // Automatically grow MaterialParamsDefaults if needed. Shrinking it will be handled at write time.
        if (MaterialParamsDefaults != null && MaterialParamsDefaults.Length < MaterialParamsSize)
        {
            var newDefaults = new byte[MaterialParamsSize];
            Array.Copy(MaterialParamsDefaults, newDefaults, MaterialParamsDefaults.Length);
            MaterialParamsDefaults = newDefaults;
        }
    }

    public void UpdateFilteredUsed(Predicate<Shader> filter)
        => UpdateUsed(filter);

    private void UpdateUsed(Predicate<Shader>? filter)
    {
        if (!Disassembled)
            return;

        var cUsage = new Dictionary<uint, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)>();
        var tUsage = new Dictionary<uint, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)>();
        var uUsage = new Dictionary<uint, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)>();

        static void CollectUsed(Dictionary<uint, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)> usage,
            Resource[] resources)
        {
            foreach (var resource in resources)
            {
                if (resource.Used == null)
                    continue;

                usage.TryGetValue(resource.Id, out var carry);
                carry.Item1 ??= Array.Empty<DisassembledShader.VectorComponents>();
                var combined = new DisassembledShader.VectorComponents[Math.Max(carry.Item1.Length, resource.Used.Length)];
                for (var i = 0; i < combined.Length; ++i)
                    combined[i] = (i < carry.Item1.Length ? carry.Item1[i] : 0) | (i < resource.Used.Length ? resource.Used[i] : 0);
                usage[resource.Id] = (combined, carry.Item2 | (resource.UsedDynamically ?? 0));
            }
        }

        static void CopyUsed(Resource[] resources,
            Dictionary<uint, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)> used)
        {
            for (var i = 0; i < resources.Length; ++i)
            {
                if (used.TryGetValue(resources[i].Id, out var usage))
                {
                    resources[i].Used            = usage.Item1;
                    resources[i].UsedDynamically = usage.Item2;
                }
                else
                {
                    resources[i].Used            = null;
                    resources[i].UsedDynamically = null;
                }
            }
        }

        static void CopyFilteredUsed(Resource[] resources,
            Dictionary<uint, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)> used)
        {
            for (var i = 0; i < resources.Length; ++i)
            {
                if (used.TryGetValue(resources[i].Id, out var usage))
                {
                    resources[i].FilteredUsed            = usage.Item1;
                    resources[i].FilteredUsedDynamically = usage.Item2;
                }
                else
                {
                    resources[i].FilteredUsed            = null;
                    resources[i].FilteredUsedDynamically = null;
                }
            }
        }

        foreach (var shader in VertexShaders)
        {
            if (filter != null && !filter(shader))
                continue;

            CollectUsed(cUsage, shader.Constants);
            CollectUsed(tUsage, shader.IsLegacy ? shader.Samplers : shader.Textures);
            CollectUsed(uUsage, shader.Uavs);
        }

        foreach (var shader in PixelShaders)
        {
            if (filter != null && !filter(shader))
                continue;

            CollectUsed(cUsage, shader.Constants);
            CollectUsed(tUsage, shader.IsLegacy ? shader.Samplers : shader.Textures);
            CollectUsed(uUsage, shader.Uavs);
        }

        if (filter == null)
        {
            CopyUsed(Constants,                      cUsage);
            CopyUsed(IsLegacy ? Samplers : Textures, tUsage);
            CopyUsed(Uavs,                           uUsage);
        }
        else
        {
            CopyFilteredUsed(Constants,                      cUsage);
            CopyFilteredUsed(IsLegacy ? Samplers : Textures, tUsage);
            CopyFilteredUsed(Uavs,                           uUsage);
        }
    }

    public void UpdateKeyValues()
    {
        static void InitializeValues(Key[] keys)
        {
            for (var i = 0; i < keys.Length; ++i)
                keys[i].Values = [keys[i].DefaultValue];
        }

        static void CollectValues(Key[] keys, uint[] values)
        {
            for (var i = 0; i < keys.Length; ++i)
                keys[i].Values.Add(values[i]);
        }

        static void SortValues(Key[] keys)
        {
            for (var i = 0; i < keys.Length; ++i)
                keys[i].Values = new ShaderKeyValueSet.SortedUniverse(keys[i].Values);
        }

        static void AddValues(ShaderKeyValueSet[] sets, uint[] values)
        {
            for (var i = 0; i < sets.Length; ++i)
                sets[i].Add(values[i]);
        }

        void InitializeShaderValues(Shader[] shaders)
        {
            for (var i = 0; i < shaders.Length; ++i)
            {
                shaders[i].SystemValues   = Array.ConvertAll(SystemKeys,   key => new ShaderKeyValueSet(key.Values));
                shaders[i].SceneValues    = Array.ConvertAll(SceneKeys,    key => new ShaderKeyValueSet(key.Values));
                shaders[i].MaterialValues = Array.ConvertAll(MaterialKeys, key => new ShaderKeyValueSet(key.Values));
                shaders[i].SubViewValues  = Array.ConvertAll(SubViewKeys,  key => new ShaderKeyValueSet(key.Values));
                shaders[i].Passes         = new ShaderKeyValueSet(Passes);
            }
        }

        void CollectShaderValues(ref Shader shader, Node node, uint passId)
        {
            for (var i = 0; i < shader.SystemValues!.Length; ++i)
                shader.SystemValues[i] |= node.SystemValues![i];
            for (var i = 0; i < shader.SceneValues!.Length; ++i)
                shader.SceneValues[i] |= node.SceneValues![i];
            for (var i = 0; i < shader.MaterialValues!.Length; ++i)
                shader.MaterialValues[i] |= node.MaterialValues![i];
            for (var i = 0; i < shader.SubViewValues!.Length; ++i)
                shader.SubViewValues[i] |= node.SubViewValues![i];
            shader.Passes.Add(passId);
        }

        InitializeValues(SystemKeys);
        InitializeValues(SceneKeys);
        InitializeValues(MaterialKeys);
        InitializeValues(SubViewKeys);
        var passes = new ShaderKeyValueSet.Universe();
        foreach (var node in Nodes)
        {
            CollectValues(SystemKeys,   node.SystemKeys);
            CollectValues(SceneKeys,    node.SceneKeys);
            CollectValues(MaterialKeys, node.MaterialKeys);
            CollectValues(SubViewKeys,  node.SubViewKeys);
            foreach (var pass in node.Passes)
                passes.Add(pass.Id);
        }

        SortValues(SystemKeys);
        SortValues(SceneKeys);
        SortValues(MaterialKeys);
        SortValues(SubViewKeys);
        Passes = new ShaderKeyValueSet.SortedUniverse(passes);

        for (var i = 0; i < Nodes.Length; ++i)
        {
            ref var node = ref Nodes[i];
            node.SystemValues   = Array.ConvertAll(SystemKeys,   key => new ShaderKeyValueSet(key.Values));
            node.SceneValues    = Array.ConvertAll(SceneKeys,    key => new ShaderKeyValueSet(key.Values));
            node.MaterialValues = Array.ConvertAll(MaterialKeys, key => new ShaderKeyValueSet(key.Values));
            node.SubViewValues  = Array.ConvertAll(SubViewKeys,  key => new ShaderKeyValueSet(key.Values));
            AddValues(node.SystemValues,   node.SystemKeys);
            AddValues(node.SceneValues,    node.SceneKeys);
            AddValues(node.MaterialValues, node.MaterialKeys);
            AddValues(node.SubViewValues,  node.SubViewKeys);
        }

        if (IsExhaustiveNodeAnalysisFeasible())
            foreach (var selector in AllSelectors(out var systemValues, out var sceneValues, out var materialValues, out var subViewValues))
            {
                if (!NodeSelectors.TryGetValue(selector, out var index) || index >= Nodes.Length)
                    continue;

                ref var node = ref Nodes[index];
                AddValues(node.SystemValues!,   systemValues);
                AddValues(node.SceneValues!,    sceneValues);
                AddValues(node.MaterialValues!, materialValues);
                AddValues(node.SubViewValues!,  subViewValues);
            }

        InitializeShaderValues(VertexShaders);
        InitializeShaderValues(PixelShaders);
        foreach (var node in Nodes)
        {
            foreach (var pass in node.Passes)
            {
                CollectShaderValues(ref VertexShaders[pass.VertexShader], node, pass.Id);
                CollectShaderValues(ref PixelShaders[pass.PixelShader],   node, pass.Id);
            }
        }
    }

    /// <summary> Determines whether this shader package's nodes can be exhaustively analyzed within a reasonable resource budget. </summary>
    /// <remarks> Some shader packages have billions of key combinations if not even more. </remarks>
    public bool IsExhaustiveNodeAnalysisFeasible()
    {
        var combinations = 1;
        foreach (var key in SystemKeys)
        {
            combinations *= key.Values.Count;
            if (combinations > MaxExhaustiveNodeAnalysisCombinations)
                return false;
        }

        foreach (var key in SceneKeys)
        {
            combinations *= key.Values.Count;
            if (combinations > MaxExhaustiveNodeAnalysisCombinations)
                return false;
        }

        foreach (var key in MaterialKeys)
        {
            combinations *= key.Values.Count;
            if (combinations > MaxExhaustiveNodeAnalysisCombinations)
                return false;
        }

        foreach (var key in SubViewKeys)
        {
            combinations *= key.Values.Count;
            if (combinations > MaxExhaustiveNodeAnalysisCombinations)
                return false;
        }

        return true;
    }

    public IEnumerable<uint> AllSelectors(out uint[] systemValues, out uint[] sceneValues, out uint[] materialValues, out uint[] subViewValues)
    {
        systemValues   = new uint[SystemKeys.Length];
        sceneValues    = new uint[SceneKeys.Length];
        materialValues = new uint[MaterialKeys.Length];
        subViewValues  = new uint[SubViewKeys.Length];
        return AllSelectors(systemValues, sceneValues, materialValues, subViewValues);
    }

    private IEnumerable<uint> AllSelectors(uint[] systemValues, uint[] sceneValues, uint[] materialValues, uint[] subViewValues)
    {
        foreach (var systemKeySelector in AllSelectors(SystemKeys, systemValues))
        {
            foreach (var sceneKeySelector in AllSelectors(SceneKeys, sceneValues))
            {
                foreach (var materialKeySelector in AllSelectors(MaterialKeys, materialValues))
                {
                    foreach (var subViewKeySelector in AllSelectors(SubViewKeys, subViewValues))
                        yield return BuildSelector(systemKeySelector, sceneKeySelector, materialKeySelector, subViewKeySelector);
                }
            }
        }
    }

    /// <remarks>
    /// Using this is discouraged, as it will generate a ShPk container in the new format, but still keep shaders designed for the old engine.
    /// The resulting ShPk can be further processed by other tools, but attempts to load it into the game as-is should not be made.
    /// </remarks>
    public void UpgradeFromLegacy()
    {
        if (!IsLegacy)
            return;

        IsLegacy = false;

        for (var i = 0; i < VertexShaders.Length; ++i)
            VertexShaders[i].UpgradeFromLegacy(this);

        for (var i = 0; i < PixelShaders.Length; ++i)
            PixelShaders[i].UpgradeFromLegacy(this);

        UpdateResources();
    }

    public static uint BuildSelector(ReadOnlySpan<uint> systemKeys, ReadOnlySpan<uint> sceneKeys, ReadOnlySpan<uint> materialKeys,
        ReadOnlySpan<uint> subViewKeys)
        => BuildSelector(BuildSelector(systemKeys), BuildSelector(sceneKeys), BuildSelector(materialKeys), BuildSelector(subViewKeys));

    [SkipLocalsInit]
    public static uint BuildSelector(uint systemKeySelector, uint sceneKeySelector, uint materialKeySelector, uint subViewKeySelector)
    {
        ReadOnlySpan<uint> parts =
        [
            systemKeySelector,
            sceneKeySelector,
            materialKeySelector,
            subViewKeySelector,
        ];
        return BuildSelector(parts);
    }

    public static uint BuildSelector(ReadOnlySpan<uint> keys)
    {
        unchecked
        {
            var selector   = 0u;
            var multiplier = 1u;
            foreach (var key in keys)
            {
                selector   += key * multiplier;
                multiplier *= SelectorMultiplier;
            }

            return selector;
        }
    }

    public static uint BuildSelector(IEnumerable<uint> keys)
    {
        unchecked
        {
            var selector   = 0u;
            var multiplier = 1u;
            foreach (var key in keys)
            {
                selector   += key * multiplier;
                multiplier *= SelectorMultiplier;
            }

            return selector;
        }
    }

    public static IEnumerable<uint> AllSelectors(ReadOnlyMemory<Key> keys)
    {
        if (keys.Length == 0)
        {
            yield return 0;

            yield break;
        }
        else if (keys.Length == 1)
        {
            foreach (var value in (IEnumerable<uint>)keys.Span[0].Values)
                yield return value;
        }
        else
        {
            var keyValues = keys.Span[0].Values;
            foreach (var selector in AllSelectors(keys[1..]))
            {
                var multiplied = unchecked(selector * SelectorMultiplier);
                foreach (var value in (IEnumerable<uint>)keyValues)
                    yield return unchecked(value + multiplied);
            }
        }
    }

    public static IEnumerable<uint> AllSelectors(ReadOnlyMemory<Key> keys, Memory<uint> values)
    {
        if (keys.Length == 0)
        {
            yield return 0;

            yield break;
        }
        else if (keys.Length == 1)
        {
            foreach (var value in (IEnumerable<uint>)keys.Span[0].Values)
            {
                values.Span[0] = value;
                yield return value;
            }
        }
        else
        {
            var keyValues = keys.Span[0].Values;
            foreach (var selector in AllSelectors(keys[1..], values[1..]))
            {
                var multiplied = unchecked(selector * SelectorMultiplier);
                foreach (var value in (IEnumerable<uint>)keyValues)
                {
                    values.Span[0] = value;
                    yield return unchecked(value + multiplied);
                }
            }
        }
    }

    public void SetInvalid()
        => Valid = false;

    public void SetChanged()
        => _changed = true;

    public bool IsChanged()
    {
        var changed = _changed;
        _changed = false;
        return changed;
    }

    private static void ClearUsed(Resource[] resources)
    {
        for (var i = 0; i < resources.Length; ++i)
        {
            resources[i].Used            = null;
            resources[i].UsedDynamically = null;
        }
    }

    private static Resource[] ReadResourceArray(ref SpanBinaryReader r, int count, ref SpanBinaryReader strings)
    {
        if (count == 0)
            return Array.Empty<Resource>();

        var ret = new Resource[count];
        for (var i = 0; i < count; ++i)
        {
            var id        = r.ReadUInt32();
            var strOffset = r.ReadUInt32();
            var strSize   = r.ReadUInt16();
            ret[i] = new Resource
            {
                Id        = id,
                Name      = strings.ReadString((int)strOffset, strSize),
                IsTexture = r.ReadUInt16(),
                Slot      = r.ReadUInt16(),
                Size      = r.ReadUInt16(),
            };
        }

        return ret;
    }

    private static Shader[] ReadShaderArray(ref SpanBinaryReader r, int count, DisassembledShader.ShaderStage stage, DxVersion directX,
        bool disassemble, bool isLegacy, ReadOnlySpan<byte> blobs, ref SpanBinaryReader strings)
    {
        if (count == 0)
            return Array.Empty<Shader>();

        var extraHeaderSize = stage switch
        {
            DisassembledShader.ShaderStage.Vertex => directX switch
            {
                DxVersion.DirectX9  => 4,
                DxVersion.DirectX11 => 8,
                _                   => throw new NotImplementedException(),
            },
            _ => 0,
        };

        var ret = new Shader[count];
        for (var i = 0; i < count; ++i)
        {
            var blobOffset    = r.ReadUInt32();
            var blobSize      = r.ReadUInt32();
            var constantCount = r.ReadUInt16();
            var samplerCount  = r.ReadUInt16();
            var uavCount      = r.ReadUInt16();
            var textureCount  = r.ReadUInt16();

            var rawBlob = blobs.Slice((int)blobOffset, (int)blobSize);

            ret[i] = new Shader
            {
                IsLegacy         = isLegacy,
                Stage            = disassemble ? stage : DisassembledShader.ShaderStage.Unspecified,
                DirectXVersion   = directX,
                Constants        = ReadResourceArray(ref r, constantCount, ref strings),
                Samplers         = ReadResourceArray(ref r, samplerCount,  ref strings),
                Uavs             = ReadResourceArray(ref r, uavCount,      ref strings),
                Textures         = ReadResourceArray(ref r, textureCount,  ref strings),
                AdditionalHeader = rawBlob[..extraHeaderSize].ToArray(),
                Blob             = rawBlob[extraHeaderSize..].ToArray(),
            };
        }

        return ret;
    }

    private static Key[] ReadKeyArray(ref SpanBinaryReader r, int count)
    {
        if (count == 0)
            return Array.Empty<Key>();

        var ret = new Key[count];
        for (var i = 0; i < count; ++i)
        {
            ret[i] = new Key
            {
                Id           = r.ReadUInt32(),
                DefaultValue = r.ReadUInt32(),
                Values       = [],
            };
            ret[i].Values.Add(ret[i].DefaultValue);
        }

        return ret;
    }

    private static Node[] ReadNodeArray(ref SpanBinaryReader r, int count, int systemKeyCount, int sceneKeyCount, int materialKeyCount,
        int subViewKeyCount)
    {
        if (count == 0)
            return Array.Empty<Node>();

        var ret = new Node[count];
        for (var i = 0; i < count; ++i)
        {
            var selector  = r.ReadUInt32();
            var passCount = r.ReadUInt32();
            ret[i] = new Node
            {
                Selector     = selector,
                PassIndices  = r.Read<byte>(16).ToArray(),
                SystemKeys   = r.Read<uint>(systemKeyCount).ToArray(),
                SceneKeys    = r.Read<uint>(sceneKeyCount).ToArray(),
                MaterialKeys = r.Read<uint>(materialKeyCount).ToArray(),
                SubViewKeys  = r.Read<uint>(subViewKeyCount).ToArray(),
                Passes       = r.Read<Pass>((int)passCount).ToArray(),
            };
        }

        return ret;
    }

    public enum DxVersion : uint
    {
        DirectX9  = 9,
        DirectX11 = 11,
    }

    public struct Resource
    {
        public uint   Id;
        public string Name;

        // We aren't sure what this is exactly yet, but it's been observed to always be 0 for constants and samplers, and 1 for textures.
        public ushort IsTexture;

        public ushort                                 Slot;
        public ushort                                 Size;
        public DisassembledShader.VectorComponents[]? Used;
        public DisassembledShader.VectorComponents?   UsedDynamically;
        public DisassembledShader.VectorComponents[]? FilteredUsed;
        public DisassembledShader.VectorComponents?   FilteredUsedDynamically;
    }

    public struct MaterialParam
    {
        public uint   Id;
        public ushort ByteOffset;
        public ushort ByteSize;
    }

    public struct Pass
    {
        public uint Id;
        public uint VertexShader;
        public uint PixelShader;
    }

    public struct Key
    {
        public uint                       Id;
        public uint                       DefaultValue;
        public ShaderKeyValueSet.Universe Values;
    }

    public struct Node
    {
        public uint   Selector;
        public byte[] PassIndices;
        public uint[] SystemKeys;
        public uint[] SceneKeys;
        public uint[] MaterialKeys;
        public uint[] SubViewKeys;
        public Pass[] Passes;

        public ShaderKeyValueSet[]? SystemValues;
        public ShaderKeyValueSet[]? SceneValues;
        public ShaderKeyValueSet[]? MaterialValues;
        public ShaderKeyValueSet[]? SubViewValues;
    }

    public struct NodeAlias // aka Item
    {
        public uint Selector;
        public uint Node;
    }
}
