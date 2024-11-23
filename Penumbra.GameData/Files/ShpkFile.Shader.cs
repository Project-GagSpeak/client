using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Lumina.Misc;
using DisassembledShader = Penumbra.GameData.Interop.DisassembledShader;
using ShaderKeyValueSet = Penumbra.GameData.Structs.SharedSet<uint, uint>;

namespace Penumbra.GameData.Files;

public partial class ShpkFile
{
    public struct Shader
    {
        public  bool                           IsLegacy;
        public  DisassembledShader.ShaderStage Stage;
        public  DxVersion                      DirectXVersion;
        public  Resource[]                     Constants;
        public  Resource[]                     Samplers;
        public  Resource[]                     Uavs;
        /// <remarks>
        /// When dealing with legacy shaders, this will always be empty, use <see cref="Samplers"/> instead.
        /// </remarks>
        public  Resource[]                     Textures;
        public  byte[]                         AdditionalHeader;
        private byte[]                         _byteData;
        private DisassembledShader?            _disassembly;

        public ShaderKeyValueSet[]? SystemValues;
        public ShaderKeyValueSet[]? SceneValues;
        public ShaderKeyValueSet[]? MaterialValues;
        public ShaderKeyValueSet[]? SubViewValues;
        public ShaderKeyValueSet    Passes;

        public byte[] Blob
        {
            get => _byteData;
            set
            {
                if (_byteData == value)
                    return;

                if (Stage != DisassembledShader.ShaderStage.Unspecified)
                {
                    // Reject the blob entirely if we can't disassemble it or if we find inconsistencies.
                    var disasm = DisassembledShader.Disassemble(value);
                    if (disasm.Stage != Stage || (disasm.ShaderModel >> 8) + 6 != (uint)DirectXVersion)
                        throw new ArgumentException(
                            $"The supplied blob is a DirectX {(disasm.ShaderModel >> 8) + 6} {disasm.Stage} shader ; expected a DirectX {(uint)DirectXVersion} {Stage} shader.",
                            nameof(value));

                    if (IsLegacy && disasm.ShaderModel >= 0x0500)
                    {
                        var samplers = new Dictionary<uint, string>();
                        var textures = new Dictionary<uint, string>();
                        foreach (var binding in disasm.ResourceBindings)
                        {
                            switch (binding.Type)
                            {
                                case DisassembledShader.ResourceType.Texture:
                                    textures[binding.Slot] = NormalizeResourceName(binding.Name);
                                    break;
                                case DisassembledShader.ResourceType.Sampler:
                                    samplers[binding.Slot] = NormalizeResourceName(binding.Name);
                                    break;
                            }
                        }

                        if (samplers.Count != textures.Count
                         || !samplers.All(pair => textures.TryGetValue(pair.Key, out var texName) && pair.Value == texName))
                            throw new ArgumentException($"The supplied blob has inconsistent sampler and texture allocation.");
                    }

                    _byteData    = value;
                    _disassembly = disasm;
                }
                else
                {
                    _byteData    = value;
                    _disassembly = null;
                }

                UpdateUsed();
            }
        }

        /// <remarks>
        /// This is only stored for vertex shaders.
        /// </remarks>
        public readonly VertexShader.Input DeclaredInputs
            => (VertexShader.Input)(AdditionalHeader.Length >= 4 ? BitConverter.ToUInt32(AdditionalHeader, 0) : 0u);

        /// <remarks>
        /// This is only stored for Shader Model 5 (DirectX 11) vertex shaders.
        /// </remarks>
        public readonly VertexShader.Input UsedInputs
            => (VertexShader.Input)(AdditionalHeader.Length >= 8 ? BitConverter.ToUInt32(AdditionalHeader, 4) : 0u);

        public readonly DisassembledShader? Disassembly
            => _disassembly;

        public readonly Resource? GetConstantById(uint id)
            => Constants.FirstOrNull(res => res.Id == id);

        public readonly Resource? GetConstantByName(string name)
            => Constants.FirstOrNull(res => res.Name == name);

        public readonly Resource? GetSamplerById(uint id)
            => Samplers.FirstOrNull(s => s.Id == id);

        public readonly Resource? GetSamplerByName(string name)
            => Samplers.FirstOrNull(s => s.Name == name);

        public readonly Resource? GetUavById(uint id)
            => Uavs.FirstOrNull(u => u.Id == id);

        public readonly Resource? GetUavByName(string name)
            => Uavs.FirstOrNull(u => u.Name == name);

        public readonly Resource? GetTextureById(uint id)
            => Textures.FirstOrNull(s => s.Id == id);

        public readonly Resource? GetTextureByName(string name)
            => Textures.FirstOrNull(s => s.Name == name);

        public void UpdateResources(ShpkFile file)
        {
            if (_disassembly == null)
                throw new InvalidOperationException();

            if (Stage == DisassembledShader.ShaderStage.Vertex && _disassembly.ShaderModel >= 0x0500)
            {
                // Shader Model 3 (DirectX 9) shaders are not handled there because we need to examine the bytecode (high effort), and we assume no one will be likely to ever want to change input signatures of SM3 vertex shaders (low value).
                VertexShader.Input declaredInputs = 0;
                VertexShader.Input usedInputs = 0;
                foreach (var input in _disassembly.InputSignature)
                {
                    var inputFlag = input.VertexInputFlag;
                    declaredInputs |= inputFlag;
                    if (input.Used != 0)
                        usedInputs |= inputFlag;
                }
                var header = MemoryMarshal.Cast<byte, VertexShader.Input>(AdditionalHeader.AsSpan());
                if (header.Length >= 1)
                    header[0] = declaredInputs;
                if (header.Length >= 2)
                    header[1] = usedInputs;
            }

            var constants = new List<Resource>();
            var samplers  = new List<Resource>();
            var uavs      = new List<Resource>();
            var textures  = new List<Resource>();
            foreach (var binding in _disassembly.ResourceBindings)
            {
                switch (binding.Type)
                {
                    case DisassembledShader.ResourceType.ConstantBuffer:
                        var name = NormalizeResourceName(binding.Name);
                        // We want to preserve IDs as much as possible, and to deterministically generate new ones in a way that's most compliant with the native ones, to maximize compatibility.
                        var id = GetConstantByName(name)?.Id ?? file.GetConstantByName(name)?.Id ?? Crc32.Get(name, 0xFFFFFFFFu);
                        constants.Add(new Resource
                        {
                            Id              = id,
                            Name            = name,
                            IsTexture       = 0,
                            Slot            = (ushort)binding.Slot,
                            Size            = (ushort)binding.RegisterCount,
                            Used            = binding.Used,
                            UsedDynamically = binding.UsedDynamically,
                        });
                        break;
                    case DisassembledShader.ResourceType.Sampler:
                        name = NormalizeResourceName(binding.Name);
                        id   = GetSamplerByName(name)?.Id ?? file.GetSamplerByName(name)?.Id ?? Crc32.Get(name, 0xFFFFFFFFu);
                        samplers.Add(new Resource
                        {
                            Id              = id,
                            Name            = name,
                            IsTexture       = 0,
                            Slot            = (ushort)binding.Slot,
                            Size            = (ushort)binding.Slot,
                            Used            = binding.Used,
                            UsedDynamically = binding.UsedDynamically,
                        });
                        break;
                    case DisassembledShader.ResourceType.Texture:
                        if (IsLegacy)
                            break;
                        name = NormalizeResourceName(binding.Name);
                        id   = GetTextureByName(name)?.Id ?? file.GetTextureByName(name)?.Id ?? Crc32.Get(name, 0xFFFFFFFFu);
                        textures.Add(new Resource
                        {
                            Id              = id,
                            Name            = name,
                            IsTexture       = 1,
                            Slot            = (ushort)binding.Slot,
                            Size            = (ushort)binding.Slot,
                            Used            = binding.Used,
                            UsedDynamically = binding.UsedDynamically,
                        });
                        break;
                    case DisassembledShader.ResourceType.Uav:
                        name = NormalizeResourceName(binding.Name);
                        id   = GetUavByName(name)?.Id ?? file.GetUavByName(name)?.Id ?? Crc32.Get(name, 0xFFFFFFFFu);
                        uavs.Add(new Resource
                        {
                            Id              = id,
                            Name            = name,
                            IsTexture       = 0, // Unsure.
                            Slot            = (ushort)binding.Slot,
                            Size            = (ushort)binding.Slot,
                            Used            = binding.Used,
                            UsedDynamically = binding.UsedDynamically,
                        });
                        break;
                }
            }

            Constants = constants.ToArray();
            Samplers  = samplers.ToArray();
            Uavs      = uavs.ToArray();
            Textures  = textures.ToArray();

            UpdateUsed();
        }

        private void UpdateUsed()
        {
            if (_disassembly != null)
            {
                var cbUsage = new Dictionary<string, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)>();
                var tUsage  = new Dictionary<string, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)>();
                var uUsage  = new Dictionary<string, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)>();
                foreach (var binding in _disassembly.ResourceBindings)
                {
                    switch (binding.Type)
                    {
                        case DisassembledShader.ResourceType.ConstantBuffer:
                            cbUsage[NormalizeResourceName(binding.Name)] = (binding.Used, binding.UsedDynamically);
                            break;
                        case DisassembledShader.ResourceType.Texture:
                            tUsage[NormalizeResourceName(binding.Name)] = (binding.Used, binding.UsedDynamically);
                            break;
                        case DisassembledShader.ResourceType.Uav:
                            uUsage[NormalizeResourceName(binding.Name)] = (binding.Used, binding.UsedDynamically);
                            break;
                    }
                }

                static void CopyUsed(Resource[] resources,
                    Dictionary<string, (DisassembledShader.VectorComponents[], DisassembledShader.VectorComponents)> used)
                {
                    for (var i = 0; i < resources.Length; ++i)
                    {
                        if (used.TryGetValue(resources[i].Name, out var usage))
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

                CopyUsed(Constants,                      cbUsage);
                CopyUsed(IsLegacy ? Samplers : Textures, tUsage);
                CopyUsed(Uavs,                           uUsage);
            }
            else
            {
                ClearUsed(Constants);
                ClearUsed(IsLegacy ? Samplers : Textures);
                ClearUsed(Uavs);
            }
        }

        internal void UpgradeFromLegacy(ShpkFile file)
        {
            if (!IsLegacy)
                return;

            IsLegacy = false;
            UpdateResources(file);
        }

        private static string NormalizeResourceName(string resourceName)
        {
            var dot = resourceName.IndexOf('.');
            if (dot >= 0)
                return resourceName[..dot];
            if (resourceName.Length > 1 && resourceName[^2] is '_' && resourceName[^1] is 'S' or 'T')
                return resourceName[..^2];

            return resourceName;
        }
    }
}
