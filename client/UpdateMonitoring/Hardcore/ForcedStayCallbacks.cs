using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Utils;
using System.Runtime.InteropServices;
using ValType = FFXIVClientStructs.FFXIV.Component.GUI;
#nullable disable

namespace GagSpeak.UpdateMonitoring;
public unsafe class ForcedStayCallback : IDisposable
{
    private readonly ILogger<ForcedStayCallback> _logger;
    private readonly ClientConfigurationManager _clientConfigs;

    // Ref:
    // https://github.com/Caraxi/SimpleTweaksPlugin/blob/02abb1c3e4a140cbccded03af1e0637c3c5665ff/Debugging/AddonDebug.cs#L127
    // Detours the fired callback to get the values from it. Useful for documenting new cases from addon interactions.
    private unsafe delegate void* FireCallbackDelegate(AtkUnitBase* atkUnitBase, int valueCount, AtkValue* atkValues, byte updateVisibility);
    [Signature("E8 ?? ?? ?? ?? 0F B6 E8 8B 44 24 20", DetourName = nameof(FireCallbackDetour), Fallibility = Fallibility.Auto)]
    private static Hook<FireCallbackDelegate> FireCallbackHook { get; set; } = null;
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
                _clientConfigs.LastSeenListIndex = atkValues[0].Int;
                //_logger.LogDebug("Last Seen List Index: " + _clientConfigs.LastSeenListIndex);
            }
            if(atkUnitBase->NameString == "SelectYesno")
            {
                var selection = atkValues[0].Int == 1 ? "No" : "Yes";
                _clientConfigs.LastSeenListSelection = selection;
                //_logger.LogDebug("Last Seen List Selection: " + _clientConfigs.LastSeenListSelection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception in {nameof(FireCallbackDetour)}: {ex.Message}");
            return FireCallbackHook.Original(atkUnitBase, valueCount, atkValues, updateVisibility);
        }
        return FireCallbackHook.Original(atkUnitBase, valueCount, atkValues, updateVisibility);
    }

    // Ref:
    // https://github.com/NightmareXIV/ECommons/blob/6ea40a9eea2e805f2f566fe0493749c7c0639ea3/ECommons/Automation/Callback.cs#L64
    private static class Signatures
    {
        public const string CallbackSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 44 24 ?? 0F B7 81";
    }

    internal delegate byte ForcedStayCallbackDelegate(AtkUnitBase* Base, int valueCount, AtkValue* values, byte updateState);
    private static ForcedStayCallbackDelegate FireCallback = null!; // Used to execute things to this callback
    public ForcedStayCallback(ILogger<ForcedStayCallback> logger, ClientConfigurationManager config,
        ISigScanner sigScanner, IGameInteropProvider interopProvider)
    {
        _logger = logger;
        _clientConfigs = config;
        interopProvider.InitializeFromAttributes(this);

        var callbackAddr = sigScanner.ScanText(Signatures.CallbackSignature);
        FireCallback = Marshal.GetDelegateForFunctionPointer<ForcedStayCallbackDelegate>(callbackAddr);
        EnableCallbackHook();
        _logger.LogInformation("ForcedStayCallback Initialized", LoggerType.HardcorePrompt);
    }

    public void EnableCallbackHook()
    {
        if (FireCallbackHook?.IsEnabled ?? false)
            return;

        _logger.LogInformation("Enabling ForcedStayCallback", LoggerType.HardcorePrompt);
        FireCallbackHook?.Enable();
    }

    public void DisableCallbackHook()
    {
        if (FireCallbackHook?.IsEnabled ?? false)
            return;

        _logger.LogInformation("Disabling ForcedStayCallback", LoggerType.HardcorePrompt);
        FireCallbackHook?.Disable();
    }

    public void Dispose()
    {
        _logger.LogInformation("Stopping ForcedStayCallback", LoggerType.HardcorePrompt);
        try
        {
            DisableCallbackHook();
            FireCallbackHook?.Dispose();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error disposing of ForcedStayCallback");
        }
        _logger.LogInformation("Stopped ForcedStayCallback", LoggerType.HardcorePrompt);
    }

    public static void Fire(AtkUnitBase* Base, bool updateState, params object[] values)
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
            StaticLogger.Logger.LogTrace($"Firing callback: " + Base->Name.Read() + ", valueCount = " + values.Length + ", updateStatte = " + updateState + ", values:\n" + string.Join("\n", CallbackValues), LoggerType.HardcorePrompt);

            if (FireCallback is not null)
            {
                FireCallback(Base, values.Length, atkValues, (byte)(updateState ? 1 : 0));
            }
            else
            {
                StaticLogger.Logger.LogError("FireCallback somehow not yet Initialized!");
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
            _logger.LogError(e, "Error in DecodeValues");
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
