using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using CkCommons;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.Utils;
using System.Reflection;
using System.Windows.Forms;
using CkCommons.Helpers;


namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls how <see cref="IKeyState"/> handles interacted keys, overriding them with a reference
///     binder to forcefully set values one could not normally set through the service.
/// </summary>
public sealed class KeystateController : DisposableMediatorSubscriberBase
{
    private readonly MovementController _moveService;

    delegate ref int GetRefValue(int vkCode);
    private static GetRefValue? _getRefValue;

    // Block everything but hardcore safeword keybind. (Maybe use keymonitor to handle this while logged out or something i dont know.
    public static readonly List<VirtualKey> AllKeys = Enum.GetValues<VirtualKey>().Skip(4).Except([VirtualKey.CONTROL, VirtualKey.MENU, VirtualKey.BACK]).ToList();
    public static readonly List<VirtualKey> MoveKeys = [VirtualKey.W, VirtualKey.A, VirtualKey.S, VirtualKey.D, VirtualKey.SPACE];

    // Dictates controlling the player's KeyState blocking.
    private PlayerControlSource _sources = PlayerControlSource.None;
    private bool _keysWereCancelled = false;

    public KeystateController(ILogger<KeystateController> logger, GagspeakMediator mediator, MovementController moveService) 
        : base(logger, mediator)
    {
        _moveService = moveService;

        Generic.Safe(delegate
        {
            _getRefValue = (GetRefValue)Delegate.CreateDelegate(typeof(GetRefValue), Svc.KeyState,
                Svc.KeyState.GetType().GetMethod("GetRefValue", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(int)], null)!);
        });

        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    private bool BlockKeyInput => _sources != 0 || _moveService.IsMoveTaskRunning;
    // this could potentially cause a race condition where the lifestream task is removed before it can reset the cancelled keys,
    // but that will only occur if the framework tick is not fast enough for the cancel.
    // an easy fix would be to just run the addition/removal of sources on the framework thread, but that would harm application time.
    private List<VirtualKey> BlockedKeys => _sources.HasAny(PlayerControlSource.LifestreamTask) ? AllKeys : MoveKeys;
    
    private unsafe void FrameworkUpdate()
    {
        if (BlockKeyInput)
            CancelMoveKeys();
        else
            ResetCancelledMoveKeys();
    }

    public void AddControlSources(PlayerControlSource sources)
        => _sources |= sources;

    public void RemoveControlSources(PlayerControlSource sources)
        => _sources &= ~sources;

    /// <summary>
    ///     The keys that are used for movement, which will be cancelled if the player is not allowed to move.
    /// </summary>
    private void CancelMoveKeys()
    {
        foreach (var x in BlockedKeys)
        {
            // the action to execute for each of our moved keys
            if (Svc.KeyState.GetRawValue(x) == 0)
            {
                // if the value is not set to execute, set it.
                Svc.KeyState.SetRawValue(x, 1);
                _keysWereCancelled = true;
            }
        }
    }

    /// <summary>
    ///     Resets any keys that were cancelled.
    /// </summary>
    private void ResetCancelledMoveKeys()
    {
        if (!_keysWereCancelled)
            return;

        // Make sure they become false.
        _keysWereCancelled = false;
        // Restore the state of the virtual keys
        foreach (var x in BlockedKeys)
        {
            if (KeyMonitor.IsKeyPressed((int)(Keys)x))
                SetKeyState(x, 3);
        }
    }

    /// <summary>
    ///     Sets the key state (if you start crashing when using this you probably have a fucked up getrefvalue)
    /// </summary>
    private static void SetKeyState(VirtualKey key, int state) => _getRefValue!((int)key) = state;      
}
