using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Services.Configs;
using GagSpeak.Utils;
using System.Runtime.InteropServices;
using ValType = FFXIVClientStructs.FFXIV.Component.GUI;
#nullable disable

namespace GagSpeak.GameInternals.Detours;
public unsafe partial class StaticDetours
{
    // Detours the fired callback to get the values from it. Useful for documenting new cases from addon interactions.
    private unsafe delegate void* FireCallbackDelegate(AtkUnitBase* atkUnitBase, int valueCount, AtkValue* atkValues, byte updateVisibility);
    [Signature(Signatures.FireCallback, DetourName = nameof(FireCallbackDetour), Fallibility = Fallibility.Auto)]
    private static Hook<FireCallbackDelegate> FireCallbackHook { get; set; } = null;

    // A delegate Function pointer that we can injection functions into and the game will react to it.
    internal delegate byte FireCallbackFuncDelegate(AtkUnitBase* Base, int valueCount, AtkValue* values, byte updateState);
    // Used to execute things to this callback
    private static FireCallbackFuncDelegate FireCallback = null!;

    /// <summary>
    ///     Detour for the FireCallback function, which is used to handle callbacks from ATK units.
    /// </summary>
    [return: MarshalAs(UnmanagedType.U1)]
    private unsafe void* FireCallbackDetour(AtkUnitBase* atkUnitBase, int valueCount, AtkValue* atkValues, byte updateVisibility)
    {
        // If the callback isnt something we care aboput then return the original
        if (atkUnitBase->NameString is not ("SelectString" or "SelectYesno"))
            return FireCallbackHook.Original(atkUnitBase, valueCount, atkValues, updateVisibility);

        try
        {
            var atkValueList = Enumerable.Range(0, valueCount)
                .Select<int, object>(i => atkValues[i].Type switch
                {
                    ValType.ValueType.Int => atkValues[i].Int,
                    ValType.ValueType.String => Marshal.PtrToStringUTF8(new IntPtr(atkValues[i].String)) ?? string.Empty,
                    ValType.ValueType.UInt => atkValues[i].UInt,
                    ValType.ValueType.Bool => atkValues[i].Byte != 0,
                    _ => $"Unknown Type: {atkValues[i].Type}"
                })
                .ToList();
            //_logger.LogDebug($"Callback triggered on {atkUnitBase->NameString} with values: {string.Join(", ", atkValueList.Select(value => value.ToString()))}");
            if(atkUnitBase->NameString == "SelectString")
            {
                MainConfigService.LastSeenListIndex = atkValues[0].Int;
                //_logger.LogDebug("Last Seen List Index: " + _mainConfig.LastSeenListIndex);
            }
            if(atkUnitBase->NameString == "SelectYesno")
            {
                var selection = atkValues[0].Int == 1 ? "No" : "Yes";
                MainConfigService.LastSeenListSelection = selection;
                //_logger.LogDebug("Last Seen List Selection: " + _mainConfig.LastSeenListSelection);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Exception in {nameof(FireCallbackDetour)}: {ex.Message}");
            return FireCallbackHook.Original(atkUnitBase, valueCount, atkValues, updateVisibility);
        }
        return FireCallbackHook.Original(atkUnitBase, valueCount, atkValues, updateVisibility);
    }

    /// <summary>
    ///     Sends off data to register as a callback to the callback function for the game to respond to.
    /// </summary>
    public static void CallbackFuncFire(AtkUnitBase* Base, bool updateState, params object[] values)
    {
        if (Base == null) throw new Exception("Null UnitBase");
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null) return;
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                switch (v)
                {
                    case uint uintValue:
                        atkValues[i].Type = ValType.ValueType.UInt;
                        atkValues[i].UInt = uintValue;
                        break;
                    case int intValue:
                        atkValues[i].Type = ValType.ValueType.Int;
                        atkValues[i].Int = intValue;
                        break;
                    case float floatValue:
                        atkValues[i].Type = ValType.ValueType.Float;
                        atkValues[i].Float = floatValue;
                        break;
                    case bool boolValue:
                        atkValues[i].Type = ValType.ValueType.Bool;
                        atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                        break;
                    case string stringValue:
                        {
                            atkValues[i].Type = ValType.ValueType.String;
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
            {
                CallbackValues.Add($"    Value {i}: [input: {values[i]}/{values[i]?.GetType().Name}] -> {DecodeValue(atkValues[i])})");
            }
            GagSpeak.StaticLog.Verbose($"Firing callback: " + Base->Name.Read() + ", valueCount = " + values.Length + ", " +
                "updateStatte = " + updateState + ", values:\n" + string.Join("\n", CallbackValues), LoggerType.HardcorePrompt);

            if (FireCallback is not null)
            {
                FireCallback(Base, values.Length, atkValues, (byte)(updateState ? 1 : 0));
            }
            else
            {
                GagSpeak.StaticLog.Error("FireCallback somehow not yet Initialized!");
            }
        }
        finally
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (atkValues[i].Type == ValType.ValueType.String)
                {
                    Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
                }
            }
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }

    public List<string> DecodeValues(int cnt, AtkValue* values)
    {
        var atkValueList = new List<string>();
        try
        {
            for (var i = 0; i < cnt; i++)
            {
                atkValueList.Add(DecodeValue(values[i]));
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error in DecodeValues");
        }
        return atkValueList;
    }

    public static string DecodeValue(AtkValue a)
    {
        var str = new StringBuilder(a.Type.ToString()).Append(": ");
        switch (a.Type)
        {
            case ValType.ValueType.Int:
                {
                    str.Append(a.Int);
                    break;
                }
            case ValType.ValueType.String8:
            case ValType.ValueType.WideString:
            case ValType.ValueType.ManagedString:
            case ValType.ValueType.String:
                {
                    str.Append(Marshal.PtrToStringUTF8(new IntPtr(a.String)));
                    break;
                }
            case ValType.ValueType.UInt:
                {
                    str.Append(a.UInt);
                    break;
                }
            case ValType.ValueType.Bool:
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
