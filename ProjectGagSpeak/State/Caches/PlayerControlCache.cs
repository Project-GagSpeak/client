using Dalamud.Game.ClientState.Keys;
using GagSpeak.GameInternals;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.State.Caches;

/// <summary>
///     Cache holding what control over the player GagSpeak has. <para />
///     
///     The HcTaskManager can also impact final result of the cache. 
///     <b>Results should only change in controllers when a refresh is requested.</b>
/// </summary>
public sealed class PlayerControlCache
{
    // Block everything but hardcore safeword keybind. (Maybe use keymonitor to handle this while logged out or something i dont know.
    public static readonly IEnumerable<VirtualKey> AllKeys = Enum.GetValues<VirtualKey>().Skip(4).Except([VirtualKey.CONTROL, VirtualKey.MENU, VirtualKey.BACK]).ToList();
    public static readonly IEnumerable<VirtualKey> MoveKeys = [VirtualKey.W, VirtualKey.A, VirtualKey.S, VirtualKey.D, VirtualKey.SPACE];

    private readonly GagspeakMediator _mediator;
    private readonly TraitsCache _traits;
    private readonly OverlayCache _overlays;
    public PlayerControlCache(GagspeakMediator mediator, TraitsCache traits, OverlayCache overlays)
    {
        _mediator = mediator;
        _traits = traits;
        _overlays = overlays;
    }

    private HcTaskControl _activeTaskControl = HcTaskControl.None;

    // Accessors that retrieve under what conditions the respective hardcore attributes should be enabled with.
    public bool BlockChatInput
        => ClientData.Hardcore.IsEnabled(HcAttribute.BlockedChatInput) 
        || _activeTaskControl.HasAny(HcTaskControl.NoChatInputAccess);

    public bool HideChatInput
        => ClientData.Hardcore.IsEnabled(HcAttribute.HiddenChatInput) 
        || _activeTaskControl.HasAny(HcTaskControl.NoChatInputView);

    public bool HideChatBoxes
        => ClientData.Hardcore.IsEnabled(HcAttribute.HiddenChatBox) 
        || _activeTaskControl.HasAny(HcTaskControl.NoChatBoxView);

    public bool PreventUnfollowing
        => ClientData.Hardcore.IsEnabled(HcAttribute.Follow) 
        || _activeTaskControl.HasAny(HcTaskControl.MustFollow);

    // dont want to enforce this during lifestream tasks.
    public bool BlockRunning
        => !_activeTaskControl.HasAny(HcTaskControl.InLifestreamTask)
        && ClientData.Hardcore.IsEnabled(HcAttribute.Follow)
        || _activeTaskControl.HasAny(HcTaskControl.Weighted)
        || _traits.FinalTraits.HasAny(Traits.Weighty);

    // handled by higher, public accessor. (do not enfore during lifestream tasks)
    public bool ShouldLockFirstPerson
        => !_activeTaskControl.HasAny(HcTaskControl.InLifestreamTask)
        && _overlays.ShouldBeFirstPerson || _activeTaskControl.HasAny(HcTaskControl.LockFirstPerson);

    // handled by higher, public accessor. (enforce during lifestream tasks so radar scans can pick things up properly)
    public bool ShouldLockThirdPerson
        => _activeTaskControl.HasAny(HcTaskControl.InLifestreamTask)
        || _activeTaskControl.HasAny(HcTaskControl.LockThirdPerson);

    // keystate blocking.
    public bool BlockAllKeys
        => _activeTaskControl.HasAny(HcTaskControl.InLifestreamTask);

    public bool BlockMovementKeys
        => ClientData.Hardcore.IsEnabled(HcAttribute.Follow)
        || ClientData.Hardcore.IsEnabled(HcAttribute.EmoteState)
        || _activeTaskControl.HasAny(HcTaskControl.BlockMovementKeys)
        || _traits.FinalTraits.HasAny(Traits.Immobile);
    
    // if the player should be entirely in lock.
    public bool BlockAnyMovement
        => ClientData.Hardcore.IsEnabled(HcAttribute.EmoteState)
        || _activeTaskControl.HasAny(HcTaskControl.FreezePlayer);

    public bool DoAutoPrompts 
        => ClientData.Hardcore.IsEnabled(HcAttribute.Confinement) 
        || ClientData.Hardcore.IsEnabled(HcAttribute.Imprisonment) 
        || _activeTaskControl.HasAny(HcTaskControl.DoConfinementPrompts);

    public CameraControlMode GetPerspectiveToLock()
        => ShouldLockFirstPerson ? CameraControlMode.FirstPerson :
            ShouldLockThirdPerson ? CameraControlMode.ThirdPerson : CameraControlMode.Unknown;
    
    public IEnumerable<VirtualKey> GetBlockedKeys()
        => BlockAllKeys ? AllKeys : BlockMovementKeys ? MoveKeys : Enumerable.Empty<VirtualKey>();


    // Update the hardcore task manager control state and refresh the controllers with the latest cache information.
    public void SetActiveTaskControl(HcTaskControl control)
    {
        _activeTaskControl = control;
        _mediator.Publish(new HcStateCacheChanged());
    }
}
