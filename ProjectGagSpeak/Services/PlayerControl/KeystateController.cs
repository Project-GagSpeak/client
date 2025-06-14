using Dalamud.Game.ClientState.Keys;
using Dalamud.Plugin.Services;
using GagSpeak.CkCommons;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using System.Reflection;
using System.Windows.Forms;


namespace GagSpeak.Services.Control;

/// <summary>
///     Controls how <see cref="IKeyState"/> handles interacted keys, overriding them with a reference
///     binder to forcefully set values one could not normally set through the service.
/// </summary>
public sealed class KeystateController : DisposableMediatorSubscriberBase
{
    private readonly MovementController _moveService;
    private readonly IKeyState _keyState;

    delegate ref int GetRefValue(int vkCode);
    private static GetRefValue? _getRefValue;

    // Dictates controlling the player's KeyState blocking.
    private PlayerControlSource _sources = PlayerControlSource.None;
    private bool _keysWereCancelled = false;

    public KeystateController(ILogger<KeystateController> logger, GagspeakMediator mediator, 
        MovementController moveService, IKeyState keyState) : base(logger, mediator)
    {
        _moveService = moveService;
        _keyState = keyState;

        Generic.ExecuteSafely(delegate
        {
            _getRefValue = (GetRefValue)Delegate.CreateDelegate(typeof(GetRefValue), keyState,
                keyState.GetType().GetMethod("GetRefValue", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(int)], null)!);
        });

        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    // If keys are currently being blocked.
    public bool BlockKeyInput => _sources != 0 || _moveService.IsMoveTaskRunning;

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
        foreach (var x in MoveKeys)
        {
            // the action to execute for each of our moved keys
            if (_keyState.GetRawValue(x) == 0)
            {
                // if the value is not set to execute, set it.
                _keyState.SetRawValue(x, 1);
                _keysWereCancelled = true;
            }
        }
    }

    /// <summary>
    ///     Resets any keys that were cancelled.
    /// </summary>
    private void ResetCancelledMoveKeys()
    {
        if (_keysWereCancelled)
        {
            _keysWereCancelled = false;
            // Restore the state of the virtual keys
            foreach (var x in MoveKeys)
            {
                if (KeyMonitor.IsKeyPressed((int)(Keys)x))
                    SetKeyState(x, 3);
            }
        }
    }

    /// <summary>
    ///     Sets the key state (if you start crashing when using this you probably have a fucked up getrefvalue)
    /// </summary>
    private static void SetKeyState(VirtualKey key, int state) => _getRefValue!((int)key) = state;

    // Allow Mouse VKeys, block the rest. (Likely dangerous to even invoke, since it could block everything, but yolo i guess)
    private readonly HashSet<VirtualKey> AllKeys = Enum.GetValues<VirtualKey>().Skip(4).ToHashSet();
    private readonly HashSet<VirtualKey> MoveKeys = [VirtualKey.W, VirtualKey.A, VirtualKey.S, VirtualKey.D, VirtualKey.SPACE];
      
}
