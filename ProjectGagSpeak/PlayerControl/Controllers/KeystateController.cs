using CkCommons;
using CkCommons.Helpers;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using System.Reflection;
using System.Windows.Forms;


namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls how <see cref="IKeyState"/> handles interacted keys, overriding them with a reference
///     binder to forcefully set values one could not normally set through the service.
/// </summary>
public sealed class KeystateController : DisposableMediatorSubscriberBase
{
    private readonly PlayerControlCache _cache;

    delegate ref int GetRefValue(int vkCode);
    private static GetRefValue? _getRefValue;

    private List<VirtualKey> _keysToBlock = new();

    public KeystateController(ILogger<KeystateController> logger, GagspeakMediator mediator, PlayerControlCache cache) 
        : base(logger, mediator)
    {
        _cache = cache;

        // cursed bs bagagwa summoning to obtain the various ref values of our keystates via reflection
        Generic.Safe(delegate
        {
            _getRefValue = (GetRefValue)Delegate.CreateDelegate(typeof(GetRefValue), Svc.KeyState,
                Svc.KeyState.GetType().GetMethod("GetRefValue", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(int)], null)!);
        });

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreState());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // reset all cancelled keys.
        ResetCancelledMoveKeys(PlayerControlCache.AllKeys);
        _keysToBlock.Clear();
    }

    private void UpdateHardcoreState()
    {
        var cacheBlockedKeys = _cache.GetBlockedKeys().ToList();

        // if we should be blocking keys, but we not currently, then block keys.
        var blockedKeyCount = _keysToBlock.Count();
        var cacheBlockedKeyCount = cacheBlockedKeys.Count();

        // Update the keys to block if there is a difference, but unblock all keys firstly.
        if (cacheBlockedKeyCount != blockedKeyCount)
        {
            // reset all cancelled keys.
            ResetCancelledMoveKeys(_keysToBlock);
            // update the list.
            _keysToBlock = cacheBlockedKeys;
        }
    }

    private unsafe void FrameworkUpdate()
    {
        if (_keysToBlock.Count <= 0)
            return;
        CancelMoveKeys(); 
    }

    /// <summary>
    ///     The keys that are used for movement, which will be cancelled if the player is not allowed to move.
    /// </summary>
    private void CancelMoveKeys()
    {
        foreach (var x in _keysToBlock)
            if (Svc.KeyState.GetRawValue(x) != 0)
                Svc.KeyState.SetRawValue(x, 0);
    }

    /// <summary>
    ///     Resets any keys that were cancelled.
    /// </summary>
    private void ResetCancelledMoveKeys(IEnumerable<VirtualKey> keysToRestore)
    {
        // Restore the state of the virtual keys if they are pressed.
        foreach (var x in keysToRestore)
            if (KeyMonitor.IsKeyPressed((int)(Keys)x))
                SetKeyState(x, 3);
    }

    /// <summary>
    ///     Sets the key state (if you start crashing when using this you probably have a fucked up getrefvalue)
    /// </summary>
    private static void SetKeyState(VirtualKey key, int state) => _getRefValue!((int)key) = state;      
}
