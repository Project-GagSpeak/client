namespace Penumbra.GameData.Files.MaterialStructs;

[StructLayout(LayoutKind.Sequential)]
public struct TableFlags(uint flags) : IEquatable<TableFlags>
{
    public uint Flags = flags;

    public bool HasTable
    {
        readonly get => (Flags & 0x4u) != 0;
        set => Flags = value ? (Flags | 0x4u) : (Flags & ~0x4u);
    }

    public bool HasDyeTable
    {
        readonly get => (Flags & 0x8u) != 0;
        set => Flags = value ? (Flags | 0x8u) : (Flags & ~0x8u);
    }

    // We have no reasonably simple way to determine for sure whether a material without a color table is a legacy or DT one.
    public readonly bool IsDawntrail
        => !HasTable || TableWidthLog != 0 && TableHeightLog != 0;

    public byte TableWidthLog
    {
        readonly get => (byte)((Flags >> 4) & 0xFu);
        set => Flags = (Flags & ~0xF0u) | ((value & 0xFu) << 4);
    }

    public byte TableHeightLog
    {
        readonly get => (byte)((Flags >> 8) & 0xFu);
        set => Flags = (Flags & ~0xF00u) | ((value & 0xFu) << 8);
    }

    public byte TableDimensionLogs
    {
        readonly get => unchecked((byte)(Flags >> 4));
        set => Flags = (Flags & ~0xFF0u) | ((uint)value << 4);
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj)
        => obj is TableFlags other && Flags == other.Flags;

    public readonly bool Equals(TableFlags other)
        => Flags == other.Flags;

    public override readonly int GetHashCode()
        => Flags.GetHashCode();

    public static ref TableFlags Wrap(ref uint flags)
        => ref UtilityFunctions.Cast<uint, TableFlags>(ref flags);

    public static bool operator ==(TableFlags left, TableFlags right)
        => left.Flags == right.Flags;

    public static bool operator !=(TableFlags left, TableFlags right)
        => left.Flags != right.Flags;
}
