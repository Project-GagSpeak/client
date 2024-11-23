using Dalamud.Hooking;
using GagSpeak.Services.Mediator;

namespace GagSpeak.UpdateMonitoring.SpatialAudio.Loaders;
public unsafe partial class ResourceLoader
{
    // ======== ACTOR =============
    public delegate IntPtr ActorVfxCreateDelegate(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7);

    public ActorVfxCreateDelegate ActorVfxCreate;

    public delegate IntPtr ActorVfxRemoveDelegate(IntPtr vfx, char a2);

    public ActorVfxRemoveDelegate ActorVfxRemove;

    // ======== ACTOR HOOKS =============
    public Hook<ActorVfxCreateDelegate> ActorVfxCreateHook { get; private set; }

    public Hook<ActorVfxRemoveDelegate> ActorVfxRemoveHook { get; private set; }

    // ======= TRIGGERS =============
    public delegate IntPtr VfxUseTriggerDelete(IntPtr vfx, uint triggerId);

    public Hook<VfxUseTriggerDelete> VfxUseTriggerHook { get; private set; }

    // ==============================

    private IntPtr ActorVfxNewDetour(string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7)
    {
        var vfx = ActorVfxCreateHook.Original(path, a2, a3, a4, a5, a6, a7);

        _logger.LogTrace($"New Actor: {path} {vfx:X8}", LoggerType.SpatialAudioLogger);
        return vfx;
    }

    private IntPtr ActorVfxRemoveDetour(IntPtr vfx, char a2)
    {
        // remove from vfxSpawns
        _mediator.Publish(new VfxActorRemoved(vfx));

        _logger.LogTrace($"Removed Actor: {vfx:X8}", LoggerType.SpatialAudioLogger);
        return ActorVfxRemoveHook.Original(vfx, a2);
    }

    private IntPtr VfxUseTriggerDetour(IntPtr vfx, uint triggerId)
    {
        // use dat trigger in dat timeline.
        var timeline = VfxUseTriggerHook.Original(vfx, triggerId);

        _logger.LogTrace($"Trigger {triggerId} on {vfx:X8}, timeline: {timeline:X8}", LoggerType.SpatialAudioLogger);
        return timeline;
    }
}
