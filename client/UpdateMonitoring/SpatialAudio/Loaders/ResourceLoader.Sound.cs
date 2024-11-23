using Dalamud.Hooking;
using System.Runtime.InteropServices;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Loaders;
public unsafe partial class ResourceLoader
{
    private IntPtr OverriddenSound = IntPtr.Zero;
    private int OverriddenSoundIdx = -1;

    // ====== PLAY SOUND =======

    // Function Pointer Delegate
    public delegate IntPtr PlaySoundDelegate(IntPtr path, byte play);
    private readonly PlaySoundDelegate PlaySoundPath;

    public void PlaySound(string path, int idx)
    {
        if (string.IsNullOrEmpty(path) || idx < 0 || !_scdManager.FileExists(path)) return;

        var bytes = Encoding.ASCII.GetBytes(path);
        var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        Marshal.WriteByte(ptr + bytes.Length, 0);

        OverriddenSound = ptr;
        OverriddenSoundIdx = idx;

        // execute the function pointer with the provided fields.
        PlaySoundPath(ptr, 1);

        OverriddenSound = IntPtr.Zero;
        OverriddenSoundIdx = -1;

        Marshal.FreeHGlobal(ptr);
    }

    // ====== INIT SOUND =========

    public delegate IntPtr InitSoundPrototype(IntPtr a1, IntPtr path, float volume, int idx, int a5, uint a6, uint a7);
    public Hook<InitSoundPrototype> InitSoundHook { get; private set; }
    private IntPtr InitSoundDetour(IntPtr a1, IntPtr path, float volume, int idx, int a5, uint a6, uint a7)
    {
        if (path != IntPtr.Zero && path == OverriddenSound)
        {
            return InitSoundHook.Original(a1, path, volume, OverriddenSoundIdx, a5, a6, a7);
        }

        return InitSoundHook.Original(a1, path, volume, idx, a5, a6, a7);
    }
}
