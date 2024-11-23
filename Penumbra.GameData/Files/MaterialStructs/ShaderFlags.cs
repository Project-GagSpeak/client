namespace Penumbra.GameData.Files.MaterialStructs;

[StructLayout(LayoutKind.Sequential)]
public struct ShaderFlags(uint flags) : IEquatable<ShaderFlags>
{
    public uint Flags = flags;

    public bool HideBackfaces
    {
        readonly get => (Flags & 0x1u) != 0;
        set => Flags = value ? (Flags | 0x1u) : (Flags & ~0x1u);
    }

    public bool EnableTransparency
    {
        readonly get => (Flags & 0x10u) != 0;
        set => Flags = value ? (Flags | 0x10u) : (Flags & ~0x10u);
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is ShaderFlags other && Flags == other.Flags;

    public readonly bool Equals(ShaderFlags other)
        => Flags == other.Flags;

    public override readonly int GetHashCode()
        => Flags.GetHashCode();

    public static ref ShaderFlags Wrap(ref uint flags)
        => ref UtilityFunctions.Cast<uint, ShaderFlags>(ref flags);

    public static bool operator ==(ShaderFlags left, ShaderFlags right)
        => left.Flags == right.Flags;

    public static bool operator !=(ShaderFlags left, ShaderFlags right)
        => left.Flags != right.Flags;
}
