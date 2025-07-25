using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

/// <summary>
/// The primary manager for all IPC calls.
/// </summary>
public sealed partial class IpcManager : DisposableMediatorSubscriberBase
{
    public IpcCallerCustomize CustomizePlus { get; }
    public IpcCallerGlamourer Glamourer { get; }
    public IpcCallerPenumbra Penumbra { get; }
    public IpcCallerMoodles Moodles { get; }
    public IpcCallerMare Mare { get; }
    public IpcCallerLifestream Lifestream { get; }
    public IpcCallerIntiface Intiface { get; }

    public IpcManager(ILogger<IpcManager> logger, GagspeakMediator mediator,
        IpcCallerCustomize ipcCustomize, IpcCallerGlamourer ipcGlamourer,
        IpcCallerPenumbra ipcPenumbra, IpcCallerMoodles moodlesIpc,
        IpcCallerMare mareIpc, IpcCallerLifestream lifestreamIpc,
        IpcCallerIntiface intifaceIpc) : base(logger, mediator)
    {
        CustomizePlus = ipcCustomize;
        Glamourer = ipcGlamourer;
        Penumbra = ipcPenumbra;
        Moodles = moodlesIpc;
        Mare = mareIpc;
        Lifestream = lifestreamIpc;
        Intiface = intifaceIpc;

        // subscribe to the delayed framework update message, which will call upon the periodic API state check.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => PeriodicApiStateCheck());

        try // do an initial check
        {
            PeriodicApiStateCheck();
        }
        catch (Bagagwa ex)
        {
            logger.LogWarning(ex, "Failed to check for some IPC, plugin not installed?");
        }
    }

    private void PeriodicApiStateCheck()
    {
        Penumbra.CheckAPI();
        Glamourer.CheckAPI();
        CustomizePlus.CheckAPI();
        Moodles.CheckAPI();
        Mare.CheckAPI();
        Lifestream.CheckAPI();
        Intiface.CheckAPI();
    }
}
