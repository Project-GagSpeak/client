using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI.Utils;
using GagSpeak.UpdateMonitoring;
using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.Utils;

namespace GagSpeak.PlayerData.Handlers;

/// <summary>
/// Handles the state of the visible game objects. Can refer to player character or another visible pair.
/// 
/// Helps with detecting when they are valid in the object table or not, and what to do with them.
/// </summary>
public sealed class GameObjectHandler : DisposableMediatorSubscriberBase
{
    private readonly OnFrameworkService _frameworkUtil; // for method helpers handled on the game's framework thread.
    private readonly Func<IntPtr> _getAddress;          // for getting the address of the object.
    private Task? _delayedZoningTask = null;                   // task to delay checking and updating owned object between zones.
    private CancellationTokenSource _zoningCts = new(); // CTS for the zoning task
    private CancellationTokenSource? _clearCts = new(); // CTS for the cache creation service
    private bool _haltProcessing = false;               // if we should halt the processing of our managed character.

    public GameObjectHandler(ILogger<GameObjectHandler> logger, GagspeakMediator mediator,
        OnFrameworkService frameworkUtil, Func<IntPtr> getAddress, bool ownedObject = true) : base(logger, mediator)
    {
        _frameworkUtil = frameworkUtil;
        _getAddress = () =>
        {
            _frameworkUtil.EnsureIsOnFramework();
            return getAddress.Invoke();
        };
        IsOwnedObject = ownedObject;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => FrameworkUpdate());

        // cutscene mediators would help with halting processing.
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, (_) => ZoneSwitchEnd());
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (_) => ZoneSwitchStart());

        // push a notifier to our IPC that we have created a new game object handler from our pairs.
        Mediator.Publish(new GameObjectHandlerCreatedMessage(this, IsOwnedObject));

        // run the object checks on framework thread.
        _frameworkUtil.RunOnFrameworkThread(CheckAndUpdateObject).GetAwaiter().GetResult();

    }


    // Determines if this object is the Player Character or not.
    public readonly bool IsOwnedObject;
    public string NameWithWorld { get; private set; } // the name of the character

    public IPlayerCharacter? PlayerCharacterObjRef { get; private set; }
    public IntPtr Address { get; private set; } // addr of character
    private IntPtr DrawObjectAddress { get; set; } // the address of the characters draw object.

    public override string ToString()
    {
        return "Name@World: " + NameWithWorld + " || Address: " + Address.ToString("X");
    }

    public void Invalidate()
    {
        Logger.LogDebug("Object for ["+NameWithWorld+"] is now invalid, clearing Address & NameWithWorld", LoggerType.GameObjects);
        Address = IntPtr.Zero;
        NameWithWorld = string.Empty;
        DrawObjectAddress = IntPtr.Zero;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Mediator.Publish(new GameObjectHandlerDestroyedMessage(this, IsOwnedObject));
    }

    private void FrameworkUpdate()
    {
        if (!_delayedZoningTask?.IsCompleted ?? false) return;

        try
        {
            CheckAndUpdateObject();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error during FrameworkUpdate of {this}", this);
        }
    }

    public void UpdatePlayerCharacterRef()
    {
        if(Address == IntPtr.Zero) return;
        PlayerCharacterObjRef = _frameworkUtil.GetIPlayerCharacterFromObjectTableAsync(Address).GetAwaiter().GetResult();
        NameWithWorld = PlayerCharacterObjRef?.GetNameWithWorld() ?? string.Empty;
    }

    /* Performs an operation on each framework update to check and update the owned objects we have. 
     * It's critical that this doesn't take too much processing power.                              */
    private unsafe void CheckAndUpdateObject()
    {
        // store the previous address and draw object.
        var prevAddr = Address;
        var prevDrawObj = DrawObjectAddress;

        // update the address of this game object.
        Address = _getAddress();
        // if the address still exists, update the draw object address.
        if (Address != IntPtr.Zero)
        {
            var drawObjAddr = (IntPtr)((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)Address)->DrawObject;
            DrawObjectAddress = drawObjAddr;
        }
        // otherwise, it doesnt exist, so set the draw object address to 0.
        else
        {
            DrawObjectAddress = IntPtr.Zero;
        }

        // if we are halting processing due to changing zones or something else like a cutscene or whatever, return.
        if (_haltProcessing) return;

        // otherwise, check if the address or draw object address has changed.
        bool drawObjDiff = DrawObjectAddress != prevDrawObj;
        bool addrDiff = Address != prevAddr;

        // check if the updated Address and Draw object are both not 0
        if (Address != IntPtr.Zero && DrawObjectAddress != IntPtr.Zero)
        {
            // if they are both not 0, and the clear cts is not null (aka we want to cancel), cancel the clear cts.
            if (_clearCts != null)
            {
                Logger.LogDebug("Cancelling Clear Task", LoggerType.GameObjects);
                _clearCts.CancelDispose();
                _clearCts = null;
            }

            if (addrDiff || drawObjDiff)
            {
                UpdatePlayerCharacterRef();
                Logger.LogDebug("Object Address Changed, updating with name & world "+NameWithWorld, LoggerType.GameObjects);
            }

            // If the game object is not a player character and a visible pair. update the name with world if the address is different, and then return.
            if (!IsOwnedObject) return;

            // Update the player characters cache in the cache creation service.
            if ((addrDiff || drawObjDiff) && IsOwnedObject)
            {
                Logger.LogDebug("Changed, Sending CreateCacheObjectMessage", LoggerType.GameObjects);
                Mediator.Publish(new CreateCacheForObjectMessage(this)); // will update the player character cache from its previous data.
            }
        }
        // reaching this case means that one of the addresses because IntPtr.Zero, so we need to clear the cache.
        else if (addrDiff || drawObjDiff)
        {
            Logger.LogTrace("[{this}] Changed", this);
            // only fires when the change is from us.
            if (IsOwnedObject)
            {
                Logger.LogDebug("["+this+"] Calling upon ClearAsync due to an owned object vanishing.", LoggerType.GameObjects);
                _clearCts?.CancelDispose();
                _clearCts = new();
                var token = _clearCts.Token;
                _ = Task.Run(() => ClearAsync(token), token);
            }
        }
    }

    /// <summary> The bridge to our cache creation service for the object handler. Should be called upon disposal of the gameobjecthandler
    /// <returns> Clears the cache for the object handler in the cache creation service via mediator publish. </returns>
    private async Task ClearAsync(CancellationToken token)
    {
        Logger.LogDebug("Running Clear Task", LoggerType.GameObjects);
        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        Logger.LogDebug("Sending ClearCachedForObjectMessage", LoggerType.GameObjects);
        Mediator.Publish(new ClearCacheForObjectMessage(this));
        _clearCts = null;
    }

    private void ZoneSwitchEnd()
    {
        if (!IsOwnedObject || _haltProcessing) return;

        _clearCts?.Cancel();
        _clearCts?.Dispose();
        _clearCts = null;
        try
        {
            _zoningCts?.CancelAfter(2500);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Zoning CTS cancel issue");
        }
    }

    private void ZoneSwitchStart()
    {
        if (!IsOwnedObject || _haltProcessing) return;

        _zoningCts = new();
        Logger.LogDebug("["+this+"] Starting Delay After Zoning", LoggerType.GameObjects);
        _delayedZoningTask = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(120), _zoningCts.Token).ConfigureAwait(false);
            }
            catch { /* consume */ }
            finally
            {
                Logger.LogDebug("["+this+"] Delay after zoning complete", LoggerType.GameObjects);
                _zoningCts.Dispose();
            }
        });
    }
}
