
namespace GagSpeak.GameInternals.Detours;
#nullable enable
#pragma warning disable CS0649 // Missing XML comment for publicly visible type or member
public partial class MovementDetours
{
    //public unsafe delegate IntPtr UNK_sub_14171A220(IntPtr a1, byte a2, bool a3, byte a4, byte a5, byte a6);
    //[Signature("48 8B C4 53 55 56 57 48 83 EC 48", DetourName = nameof(UNK_sub_14171A220Detour), Fallibility = Fallibility.Auto)]
    //private Hook<UNK_sub_14171A220> UNK_sub_14171A220Hook = null!;
    //public unsafe IntPtr UNK_sub_14171A220Detour(IntPtr a1, byte a2, bool a3, byte a4, byte a5, byte a6)
    //{
    //    bool shouldTurn = a3;
    //    Svc.Logger.Information($"Detouring: [A1: ({a1.ToString("X")}), A2: {a2}, A3: {a3}, A4: {a4}, A5: {a5}, A6: {a6}]");
    //    // Read *(byte*)(a1 + 61)
    //    byte flag61 = *(byte*)((byte*)a1 + 61);
    //    // Log each piece of the conditional
    //    Svc.Logger.Information($"[Check @a1+61] Raw: {flag61}, Interpreted: {flag61 != 0}, A3: {a3}");

    //    if (flag61 == 0 && a3)
    //    {
    //        //*(byte*)(a1 + 61) = 0;
    //        //shouldTurn = false;
    //        Svc.Logger.Information($"[Condition Triggered] (!*(_BYTE*)(a1+61) && a3) → would set (a1+61) = 1");

    //    }

    //    return UNK_sub_14171A220Hook.Original(a1, a2, shouldTurn, a4, a5, a6);
    //}

    //// Possibly related to movement.
    //public unsafe delegate byte* UNK_sub_141719E40(IntPtr a1, float a2, float* a3, byte a4, sbyte a5, byte a6, sbyte a7, float a8, byte* a9);
    //[Signature("48 8B C4 48 89 58 10 48 89 68 18 48 89 70 20 57 48 81 EC ?? ?? ?? ?? 48 8B 59 20", DetourName = nameof(UNK_sub_141719E40Detour), Fallibility = Fallibility.Auto)]
    //private Hook<UNK_sub_141719E40> UNK_sub_141719E40Hook = null!;
    //public unsafe byte* UNK_sub_141719E40Detour(IntPtr a1, float a2, float* a3, byte a4, sbyte a5, byte a6, sbyte a7, float a8, byte* a9)
    //{
    //    var ret = UNK_sub_141719E40Hook.Original(a1, a2, a3, a4, a5, a6, a7, a8, a9);
    //    try
    //    {
    //        Svc.Logger.Information($"UNK_sub_141719E40 Detouring: [A1: ({a1.ToString("X")}), A2: {a2}, A3: {*a3}, A4: {a4}, A5: {a5}, A6: {a6}, A7: {a7}, A8: {a8}, A9: {*a9}]");
    //    }
    //    catch (Exception ex)
    //    {
    //        Svc.Logger.Error($"Error in UNK_sub_14171A220Detour: {ex}");
    //    }
    //    // log offset of 
    //    return ret;
    //}
}
