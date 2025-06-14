using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GagSpeak.Interop.Ipc;
using GagSpeak.UpdateMonitoring;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using Penumbra.GameData.Enums;

namespace GagSpeak.State.Listeners;

// Must be partial to avoid circular dependancy. The time it would take to make this
// not have circular dependancy is not worth looking into how to split it.
public class GlamourListener : IDisposable
{
    private readonly ILogger<GlamourHandler> _logger;
    private readonly IpcCallerGlamourer _ipc;
    private readonly GlamourCache _cache;
    private readonly GlamourHandler _handler;
    private readonly PlayerData _player;

    public GlamourListener(
        ILogger<GlamourHandler> logger,
        IpcCallerGlamourer ipc,
        PlayerData clientMonitor,
        IDalamudPluginInterface pi,
        IGameInteropProvider gip)
    {
        _logger = logger;
        _ipc = ipc;
        _player = clientMonitor;

        _ipc.StateWasChanged = StateChangedWithType.Subscriber(pi, (addr, type) => _ = OnStateChanged(addr, type));
        _ipc.StateWasFinalized = StateFinalized.Subscriber(pi, (addr, type) => _ = OnStateFinalized(addr, type));
        _ipc.StateWasChanged.Enable();
        _ipc.StateWasFinalized.Enable();
    }

    public void Dispose()
    {
        _ipc.StateWasChanged.Disable();
        _ipc.StateWasChanged?.Dispose();
        _ipc.StateWasFinalized.Disable();
        _ipc.StateWasFinalized?.Dispose();
    }

    /// <summary>
    ///     When ANY Glamourer state change occurs for ANY given actor, this is fired.
    /// </summary>
    /// <param name="address">The address of the actor that was changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <remarks> This is primarily used to cache the state of the Client. Discarded for other players. </remarks>
    private async Task OnStateChanged(nint address, StateChangeType changeType)
    {
        if (address != _player.Address)
            return;

        if (changeType is not (StateChangeType.Equip or StateChangeType.Stains or StateChangeType.Other))
            return;

        if (_handler.BlockIpcCalls is not IpcBlockReason.None)
        {
            _logger.LogWarning($"[OnStateChanged] ChangeType: [{changeType}] blocked! Still processing! ({_handler.BlockIpcCalls})", LoggerType.IpcGlamourer);
            return;
        }

        // Handle the only state changes we want to care about, as they interfere
        // with OnStateFinalized, or rather are special exceptions.
        if (changeType is StateChangeType.Equip or StateChangeType.Stains)
        {
            if(_cache.FinalGlamour.Keys.Count() <= 0)
                return;

            _logger.LogDebug($"[OnStateChanged] ChangeType: [{changeType}] accepted, Processing ApplySemaphore!", LoggerType.IpcGlamourer);
            await _handler.ApplySemaphore(true, false, false);
        }
        else if (changeType is StateChangeType.Other)
        {
            if (!_cache.FinalMeta.AnySet())
                return;

            _logger.LogDebug($"[OnStateChanged] ChangeType: [{changeType}] accepted, Processing ApplyMetaCache!", LoggerType.IpcGlamourer);
            await _handler.ApplySemaphore(false, true, false);
        }
    }

    /// <summary> 
    ///     Any any primary Glamourer Operation has completed, StateFinalized will fire. 
    ///     (This IPC Call is a Godsend).
    /// </summary>
    /// <param name="address"> The address of the actor that was finalized. </param>
    /// <param name="finalizationType"> The type of finalization that occurred. </param>
    /// <remarks> This is primarily used to cache the state of the player after a glamour operation has completed. </remarks>
    private async Task OnStateFinalized(nint address, StateFinalizationType finalizationType)
    {
        if (address != _player.Address)
            return;

        _logger.LogDebug($"[OnStateFinalized] FinalizationType: [{finalizationType}] accepted, Caching & Applying!", LoggerType.IpcGlamourer);
        await _handler.ApplySemaphore(true, true, true);
    }
}
