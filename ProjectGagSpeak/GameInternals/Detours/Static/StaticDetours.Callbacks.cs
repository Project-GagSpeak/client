using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;
using System.Runtime.InteropServices;

#nullable disable

namespace GagSpeak.GameInternals.Detours;
public unsafe partial class StaticDetours
{
    // Delegate for manually invoking a callback fire.
    public delegate bool AtkUnitBase_FireCallbackDelegate(AtkUnitBase* Base, int valueCount, AtkValue* values, byte updateState);
    [Signature(Signatures.Callback, DetourName = nameof(AtkUnitBase_FireCallbackDetour), Fallibility = Fallibility.Auto)]
    private static Hook<AtkUnitBase_FireCallbackDelegate> FireCallbackHook = null!;

    // Used to execute things to this callback
    internal static AtkUnitBase_FireCallbackDelegate FireCallbackFunc = null!;

    /// <summary> Detour the callback for a unit base. This is called frequently, so minimize logic checks. </summary>
    private static bool AtkUnitBase_FireCallbackDetour(AtkUnitBase* atkBase, int valueCount, AtkValue*  atkValues, byte updateVis)
    {
        var ret = FireCallbackHook?.Original(atkBase, valueCount, atkValues, updateVis);
        // attempt to log it, if we want to. Recommeneded to disable it though.
        try
        {
            Svc.Logger.Verbose($"Callback on {atkBase->Name.Read()}, valueCount={valueCount}, updateState={updateVis}\n" +
                $"{string.Join("\n", DecodeValues(valueCount, atkValues).Select(x => $"    {x}"))}");
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Error($"Error in {nameof(AtkUnitBase_FireCallbackDetour)}: {ex.Message}");
        }
        return ret ?? false;
    }

    public static void FireCallbackRaw(AtkUnitBase* atkUnitBase, int valueCount, AtkValue* atkValues, byte updateVisibility)
    {
        // if for whatever godforsaken reason this is not set, set it here.
        if (FireCallbackFunc is null)
            FireCallbackFunc = Marshal.GetDelegateForFunctionPointer<AtkUnitBase_FireCallbackDelegate>(Svc.SigScanner.ScanText(Signatures.Callback));
        // Manually invoke the callback fire.
        FireCallbackFunc(atkUnitBase, valueCount, atkValues, updateVisibility);
    }

    public static void FireCallback(AtkUnitBase* atkBase, bool updateState, params object[] values)
    {
        if (atkBase == null)
            throw new Exception("Null UnitBase");
        // obtain the atkValues.
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null)
            return;

        // AtkValues are valid, so assign them.
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                switch (v)
                {
                    case uint uintValue:
                        atkValues[i].Type = AtkValueType.UInt;
                        atkValues[i].UInt = uintValue;
                        break;
                    case int intValue:
                        atkValues[i].Type = AtkValueType.Int;
                        atkValues[i].Int = intValue;
                        break;
                    case float floatValue:
                        atkValues[i].Type = AtkValueType.Float;
                        atkValues[i].Float = floatValue;
                        break;
                    case bool boolValue:
                        atkValues[i].Type = AtkValueType.Bool;
                        atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                        break;
                    case string stringValue:
                        {
                            atkValues[i].Type = AtkValueType.String;
                            var stringBytes = Encoding.UTF8.GetBytes(stringValue);
                            var stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                            Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                            Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                            atkValues[i].String = (byte*)stringAlloc;
                            break;
                        }
                    case AtkValue rawValue:
                        {
                            atkValues[i] = rawValue;
                            break;
                        }
                    default:
                        throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                }
            }
            List<string> CallbackValues = [];
            for (var i = 0; i < values.Length; i++)
                CallbackValues.Add($"    Value {i}: [input: {values[i]}/{values[i]?.GetType().Name}] -> {DecodeValue(atkValues[i])})");

            Svc.Logger.Verbose($"Firing callback: {atkBase->Name.Read()}, valueCount = {values.Length}, updateStatte = {updateState}, values:\n");
            FireCallbackRaw(atkBase, values.Length, atkValues, (byte)(updateState ? 1 : 0));
        }
        finally
        {
            // free up the allocated memory for strings.
            for (var i = 0; i < values.Length; i++)
                if (atkValues[i].Type == AtkValueType.String)
                    Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
            // free the allocated memory for the atkValues.
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }

    public static List<string> DecodeValues(int cnt, AtkValue* values)
    {
        var atkValueList = new List<string>();
        try
        {
            for (var i = 0; i < cnt; i++)
                atkValueList.Add(DecodeValue(values[i]));
        }
        catch (Bagagwa e)
        {
            Svc.Logger.Error(e, "Error in DecodeValues");
        }
        return atkValueList;
    }

    public static string DecodeValue(AtkValue a)
    {
        var str = new StringBuilder(a.Type.ToString()).Append(": ");
        switch (a.Type)
        {
            case AtkValueType.Int:
                {
                    str.Append(a.Int);
                    break;
                }
            case AtkValueType.String8:
            case AtkValueType.WideString:
            case AtkValueType.ManagedString:
            case AtkValueType.String:
                {
                    str.Append(Marshal.PtrToStringUTF8(new IntPtr(a.String)));
                    break;
                }
            case AtkValueType.UInt:
                {
                    str.Append(a.UInt);
                    break;
                }
            case AtkValueType.Bool:
                {
                    str.Append(a.Byte != 0);
                    break;
                }
            default:
                {
                    str.Append($"Unknown Type: {a.Int}");
                    break;
                }
        }
        return str.ToString();
    }
}
