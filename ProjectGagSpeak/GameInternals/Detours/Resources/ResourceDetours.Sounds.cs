using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using GagSpeak.GameInternals.Structs;
using Penumbra.GameData;
#nullable disable

namespace GagSpeak.GameInternals.Detours;

// Most of this is taken from penumbra as vfxeditor has been falling behind on updates.
public unsafe partial class ResourceDetours
{
    // our custom resolved crc64 datas.
    private readonly HashSet<ulong> CustomScdCrc = [];

    // A local thread pool for the scd's return data validity.
    private readonly ThreadLocal<bool> ScdReturnData = new(() => default);

    // Checks the state of a file, returning whether it is loaded or not.
    private delegate IntPtr CheckFileState(IntPtr ptr, ulong crc64);
    [Signature(Sigs.CheckFileState, DetourName = nameof(CheckFileStateDetour), ScanType = ScanType.StaticAddress)]
    private readonly Hook<CheckFileState> CheckFileStateHook = null!;

    // Loads a local SCD file.
    [Signature(Sigs.LoadScdFileLocal)] // will be initialized by InitializeFromAttributes(this) call.
    private readonly delegate* unmanaged<ResourceHandle*, SeFileDescriptor*, byte, byte> LoadScdFileLocal = null!;

    // Occurs whenever a sound file is loaded.
    private delegate byte SoundOnLoadDelegate(ResourceHandle* handle, SeFileDescriptor* descriptor, byte unk);
    [Signature(Sigs.SoundOnLoad, DetourName = nameof(OnScdLoadDetour))]
    private readonly Hook<SoundOnLoadDelegate> SoundOnLoadHook = null!;

    /// <summary>
    ///     Checks the state of a file, returning whether it is loaded or not.
    /// </summary>
    private nint CheckFileStateDetour(IntPtr ptr, ulong crc64)
    {
        // If the file is a custom scd, we return the pointer to the custom scd.
        if (CustomScdCrc.Contains(crc64))
            ScdReturnData.Value = true;
        return CheckFileStateHook.Original(ptr, crc64);
    }

    /// <summary>
    ///     Fired whenever a sound file is loaded.
    /// </summary>
    private byte OnScdLoadDetour(ResourceHandle* handle, SeFileDescriptor* descriptor, byte unk)
    {
        var ret = SoundOnLoadHook.Original(handle, descriptor, unk);
        // if the return data had no value, return the original.
        if (!ScdReturnData.Value)
            return ret;
        
        // Otherwise the function failed on a replaced scd, so call local.
        ScdReturnData.Value = false;
        return LoadScdFileLocal(handle, descriptor, unk);
    }
}
