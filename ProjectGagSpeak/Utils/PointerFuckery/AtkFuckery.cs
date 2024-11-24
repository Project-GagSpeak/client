using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GagSpeak.Utils;

/// <summary> A class for all of the UI helpers, including basic functions for drawing repetative yet unique design elements </summary>
public static unsafe class AtkFuckery
{
    public static AtkUnitBase* Base(this AddonArgs args) => (AtkUnitBase*)args.Addon;

    public static IntPtr GetAddonByName(string name)
    {
        var atkStage = AtkStage.Instance();
        if (atkStage == null)
            return IntPtr.Zero;

        var unitMgr = atkStage->RaptureAtkUnitManager;
        if (unitMgr == null)
            return IntPtr.Zero;

        var addon = unitMgr->GetAddonByName(name, 1);
        if (addon == null)
            return IntPtr.Zero;

        return (IntPtr)addon;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkUnitBase* Addon)
    {
        return Addon->IsVisible && Addon->UldManager.LoadedState == AtkLoadState.Loaded && Addon->IsFullyLoaded();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkComponentNode* Addon)
    {
        return Addon->AtkResNode.IsVisible() && Addon->Component->UldManager.LoadedState == AtkLoadState.Loaded;
    }

    public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon)
    {
        var btnRes = target.AtkComponentBase.OwnerNode->AtkResNode;
        var evt = btnRes.AtkEventManager.Event;

        addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, btnRes.AtkEventManager.Event);
    }

    public static void ClickAddonButton(this AtkCollisionNode target, AtkUnitBase* addon)
    {
        var btnRes = target.AtkResNode;
        var evt = btnRes.AtkEventManager.Event;

        while (evt->State.EventType != AtkEventType.MouseClick)
            evt = evt->NextEvent;

        addon->ReceiveEvent(evt->State.EventType, (int)evt->Param, btnRes.AtkEventManager.Event);
    }

    public static void GenerateCallback(AtkUnitBase* unitBase, params object[] values)
    {
        var atkValues = CreateAtkValueArray(values);
        if (atkValues == null) return;
        try
        {
            unitBase->FireCallback((uint)values.Length, atkValues);
        }
        finally
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (atkValues[i].Type == FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String)
                {
                    Marshal.FreeHGlobal(new IntPtr(atkValues[i].String));
                }
            }
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }

    public static AtkValue* CreateAtkValueArray(params object[] values)
    {
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(values.Length * sizeof(AtkValue));
        if (atkValues == null) return null;
        try
        {
            for (var i = 0; i < values.Length; i++)
            {
                var v = values[i];
                switch (v)
                {
                    case uint uintValue:
                        atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt;
                        atkValues[i].UInt = uintValue;
                        break;
                    case int intValue:
                        atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
                        atkValues[i].Int = intValue;
                        break;
                    case float floatValue:
                        atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Float;
                        atkValues[i].Float = floatValue;
                        break;
                    case bool boolValue:
                        atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Bool;
                        atkValues[i].Byte = (byte)(boolValue ? 1 : 0);
                        break;
                    case string stringValue:
                        {
                            atkValues[i].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String;
                            var stringBytes = Encoding.UTF8.GetBytes(stringValue);
                            var stringAlloc = Marshal.AllocHGlobal(stringBytes.Length + 1);
                            Marshal.Copy(stringBytes, 0, stringAlloc, stringBytes.Length);
                            Marshal.WriteByte(stringAlloc, stringBytes.Length, 0);
                            atkValues[i].String = (byte*)stringAlloc;
                            break;
                        }
                    default:
                        throw new ArgumentException($"Unable to convert type {v.GetType()} to AtkValue");
                }
            }
        }
        catch
        {
            return null;
        }
        return atkValues;
    }
}
