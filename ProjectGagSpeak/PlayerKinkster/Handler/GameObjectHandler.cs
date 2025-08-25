using Dalamud.Game.ClientState.Objects.SubKinds;
using CkCommons;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Kinksters.Handlers;

/// <summary>
///     <b> SHOULD ONLY BE CREATED FOR KINKSTERS, NOT THE CLIENT</b><para />
///     Handles the state of the visible game objects. Can refer 
///     to player character or another visible pair. <para />
///     
///     Helps with detecting when they are valid in the object 
///     table or not, and what to do with them. <para />
///     
///     Could definitely use some cleanup!
/// </summary>
public sealed class KinksterGameObj : DisposableMediatorSubscriberBase
{
    private readonly OnFrameworkService _frameworkUtil; // for method helpers handled on the game's framework thread.
    private readonly Func<IntPtr> _getAddress;          // for getting the address of the object.
    private CancellationTokenSource? _clearCts = new(); // CTS for the cache creation service

    public KinksterGameObj(ILogger<KinksterGameObj> logger, GagspeakMediator mediator,
        OnFrameworkService frameworkUtil, Func<IntPtr> getAddress) 
        : base(logger, mediator)
    {
        _frameworkUtil = frameworkUtil;
        _getAddress = () =>
        {
            _frameworkUtil.EnsureIsOnFramework();
            return getAddress.Invoke();
        };

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
        // maybe replace with Svc.Framework.RunOnFrameworkThread?
        _frameworkUtil.RunOnFrameworkThread(CheckAndUpdateObject).GetAwaiter().GetResult();
        
        // Mark as created with the name @ world now present.
        Mediator.Publish(new KinksterGameObjCreatedMessage(this));

    }

    public string NameWithWorld { get; private set; } // the name of the character
    public IPlayerCharacter? PlayerCharacterObjRef { get; private set; }
    public IntPtr Address { get; private set; } // addr of character
    private IntPtr DrawObjectAddress { get; set; } // the address of the characters draw object.

    public override string ToString()
        => $"Name@World: {NameWithWorld}, Address: {Address.ToString("X")}";

    public void Invalidate()
    {
        Logger.LogDebug($"Object for [{NameWithWorld}] is now invalid, clearing Address & NameWithWorld", LoggerType.GameObjects);
        Address = IntPtr.Zero;
        NameWithWorld = string.Empty;
        DrawObjectAddress = IntPtr.Zero;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Mediator.Publish(new KinksterGameObjDestroyedMessage(this));
    }

    private void FrameworkUpdate()
    {
        Generic.Safe(CheckAndUpdateObject);
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

        // otherwise, check if the address or draw object address has changed.
        var drawObjDiff = DrawObjectAddress != prevDrawObj;
        var addrDiff = Address != prevAddr;

        // check if the updated Address and Draw object are both not 0
        if (Address != IntPtr.Zero && DrawObjectAddress != IntPtr.Zero)
        {
            // if they are both not 0, and the clear cts is not null (aka we want to cancel), cancel the clear cts.
            if (_clearCts != null)
            {
                Logger.LogDebug("Cancelling Clear Task", LoggerType.GameObjects);
                _clearCts.Cancel();
                _clearCts.Dispose();
                _clearCts = null;
            }

            if (addrDiff || drawObjDiff)
            {
                UpdatePlayerCharacterRef();
                Logger.LogDebug("Object Address Changed, updating with name & world "+NameWithWorld, LoggerType.GameObjects);
            }
        }
        // reaching this case means that one of the addresses because IntPtr.Zero, so we need to clear the cache.
        else if (addrDiff || drawObjDiff)
        {
            Logger.LogTrace("[{this}] Changed", this);
        }
    }

    public void UpdatePlayerCharacterRef()
    {
        if (Address == IntPtr.Zero)
            return;
        PlayerCharacterObjRef = _frameworkUtil.GetIPlayerCharacterFromObjectTableAsync(Address).GetAwaiter().GetResult();
        NameWithWorld = PlayerCharacterObjRef?.GetNameWithWorld() ?? string.Empty;
    }
}
