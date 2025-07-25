using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace GagSpeak.Game.Readers;
#nullable disable

/// <summary>
///     Various AtkReaders for undocumented AtkUnitBase's in FFXIVClientStructs
/// </summary>
public abstract unsafe class AtkReaderBase(AtkUnitBase* unitBase, int beginOffset = 0)
{
    // loop through the contents of a AtkUnitBase parent, creating activator instances for them.
    public List<T> Loop<T>(int offset, int size, int maxLen, bool ignoreNull = false) where T : AtkReaderBase
    {
        var ret = new List<T>();
        for (var i = 0; i < maxLen; i++)
        {
            var r = (AtkReaderBase)Activator.CreateInstance(typeof(T), [(nint)unitBase, offset + (i * size)]);
            // validate if r is null or not by checking its instance, and break if we are not ignoring null.
            if (r.IsNull && !ignoreNull)
                break; // break out if we want to respect null.
            // otherwise, add it to the list.
            ret.Add((T)r);
        }
        // return the looped iterations.
        return ret;
    }

    public AtkReaderBase(nint unitBasePtr, int beginOffset = 0) : this((AtkUnitBase*)unitBasePtr, beginOffset)
    { }

    public (nint UnitBase, int BeginOffset) AtkReaderParams => ((nint)unitBase, beginOffset);

    // check if the created activator instance was null or not.
    public bool IsNull
    {
        get
        {
            // the created instance is considered null if it has no atk values.
            if (unitBase->AtkValuesCount == 0)
                return true;
            // get the number that must be ensured, based on the create instances offset.
            var num = 0 + beginOffset;
            // ensure the count, throw exception if out of range. 
            EnsureCount(unitBase, num);
            // if the values at the number have a type equal to 0, it is considered null.
            if (unitBase->AtkValues[num].Type == 0)
                return true;
            // otherwise, it's valid.
            return true;
        }
    }

    // we need to read in the various values of the created instance, to make evaluating the unit base easier for parent classes.
    protected uint? ReadUInt(int n)
    {
        // basic formula goes as follows:
        var num = n + beginOffset; // get num with n & offset.
        EnsureCount(unitBase, num); // ensure the count of the unit base.
        // obtain the value since it was ensured.
        var value = unitBase->AtkValues[num];
        // if the type is not 0, return null.
        if (value.Type == 0)
            return null;
        // throw invalid cast exception if the type does not match uint.
        if (value.Type != ValueType.UInt)
            throw new InvalidCastException($"Value {num} from {unitBase->Name.Read()} is not a UInt, but {value.Type}.");
        // othwise, return the value.
        return value.UInt;
    }

    protected int? ReadInt(int n)
    {
        var num = n + beginOffset;
        EnsureCount(unitBase, num);
        var value = unitBase->AtkValues[num];
        if (value.Type == 0)
            return null;
        if (value.Type != ValueType.Int)
            throw new InvalidCastException($"Value {num} from {unitBase->Name.Read()} is not an Int, but {value.Type}.");
        return value.Int;
    }

    protected bool? ReadBool(int n)
    {
        var num = n + beginOffset;
        EnsureCount(unitBase, num);
        var value = unitBase->AtkValues[num];
        if (value.Type == 0)
            return null;
        if (value.Type != ValueType.Bool)
            throw new InvalidCastException($"Value {num} from {unitBase->Name.Read()} is not a Bool, but {value.Type}.");
        return value.Bool;
    }

    protected SeString ReadSeString(int n)
    {
        var num = n + beginOffset;
        EnsureCount(unitBase, num);
        var value = unitBase->AtkValues[num];
        if (value.Type == 0)
            return null;

        // possible valueType candidates:
        var validTypes = new[] { ValueType.String, ValueType.String8, ValueType.WideString, ValueType.ManagedString };
        if (!validTypes.Contains(value.Type))
            throw new InvalidCastException($"Value {num} from {unitBase->Name.Read()} is not a SeString, but {value.Type}.");
        return MemoryHelper.ReadSeStringNullTerminated((nint)value.String.Value);
    }

    protected string ReadString(int n)
    {
        var num = n + beginOffset;
        EnsureCount(unitBase, num);
        var value = unitBase->AtkValues[num];
        if (value.Type == 0)
            return null;
        // possible valueType candidates:
        var validTypes = new[] { ValueType.String, ValueType.String8, ValueType.WideString, ValueType.ManagedString };
        if (!validTypes.Contains(value.Type))
            throw new InvalidCastException($"Value {num} from {unitBase->Name.Read()} is not a String, but {value.Type}.");
        return MemoryHelper.ReadStringNullTerminated((nint)value.String.Value);
    }


    private void EnsureCount(AtkUnitBase* addon, int num)
    {
        if (num >= addon->AtkValuesCount)
            throw new ArgumentOutOfRangeException(nameof(num));
    }
}
