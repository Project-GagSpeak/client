namespace Penumbra.GameData.Structs;

/// <summary> A wrapper for customization values for type-safety. </summary>
public record struct CustomizeValue(byte Value) :
    IComparisonOperators<CustomizeValue, CustomizeValue, bool>,
    IComparisonOperators<CustomizeValue, int, bool>,
    IAdditionOperators<CustomizeValue, int, CustomizeValue>
{
    public static readonly CustomizeValue Zero = new(0);
    public static readonly CustomizeValue Max  = new(0xFF);

    public static CustomizeValue Bool(bool b)
        => b ? Max : Zero;

    public static explicit operator CustomizeValue(byte value)
        => new(value);

    public static CustomizeValue operator ++(CustomizeValue v)
        => new(++v.Value);

    public static CustomizeValue operator --(CustomizeValue v)
        => new(--v.Value);

    public static bool operator >=(CustomizeValue left, int right)
        => left.Value >= right;

    public static bool operator <(CustomizeValue v, int count)
        => v.Value < count;

    public static bool operator <=(CustomizeValue left, int right)
        => left.Value <= right;

    public static bool operator ==(CustomizeValue left, int right)
        => left.Value == right;

    public static bool operator !=(CustomizeValue left, int right)
        => left.Value != right;

    public static bool operator >(CustomizeValue v, int count)
        => v.Value > count;

    public static CustomizeValue operator +(CustomizeValue v, int rhs)
        => new((byte)(v.Value + rhs));

    public static CustomizeValue operator -(CustomizeValue v, int rhs)
        => new((byte)(v.Value - rhs));

    public override readonly string ToString()
        => Value.ToString();

    public static bool operator >(CustomizeValue left, CustomizeValue right)
        => left.Value > right.Value;

    public static bool operator >=(CustomizeValue left, CustomizeValue right)
        => left.Value >= right.Value;

    public static bool operator <(CustomizeValue left, CustomizeValue right)
        => left.Value < right.Value;

    public static bool operator <=(CustomizeValue left, CustomizeValue right)
        => left.Value <= right.Value;
}
