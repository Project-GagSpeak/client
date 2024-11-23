namespace Penumbra.GameData.Files.MaterialStructs;

[StructLayout(LayoutKind.Sequential)]
public struct SamplerFlags(uint flags) : IEquatable<SamplerFlags>
{
    public uint Flags = flags;

    public TextureAddressMode UAddressMode
    {
        readonly get => (TextureAddressMode)(Flags & 0x3u);
        set => Flags = (Flags & ~0x3u) | ((uint)value & 0x3u);
    }

    public TextureAddressMode VAddressMode
    {
        readonly get => (TextureAddressMode)((Flags >> 2) & 0x3u);
        set => Flags = (Flags & ~0xCu) | (((uint)value & 0x3u) << 2);
    }

    public float LodBias
    {
        readonly get => unchecked((int)(Flags << 12) >> 22) / 64.0f;
        set => Flags = (Flags & ~0x000FFC00u)
              | ((uint)((int)Math.Round(Math.Clamp(value, -8.0f, 7.984375f) * 64.0f) & 0x3FF) << 10);
    }

    public int MinLod
    {
        readonly get => (int)((Flags >> 20) & 0xFu);
        set => Flags = (Flags & ~0x00F00000u) | ((uint)Math.Clamp(value, 0, 15) << 20);
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is SamplerFlags other && Flags == other.Flags;

    public readonly bool Equals(SamplerFlags other)
        => Flags == other.Flags;

    public override readonly int GetHashCode()
        => Flags.GetHashCode();

    public static ref SamplerFlags Wrap(ref uint flags)
        => ref UtilityFunctions.Cast<uint, SamplerFlags>(ref flags);

    public static bool operator ==(SamplerFlags left, SamplerFlags right)
        => left.Flags == right.Flags;

    public static bool operator !=(SamplerFlags left, SamplerFlags right)
        => left.Flags != right.Flags;

    public enum TextureAddressMode : uint
    {
        Wrap   = 0,
        Mirror = 1,
        Clamp  = 2,
        Border = 3,
    }
}
