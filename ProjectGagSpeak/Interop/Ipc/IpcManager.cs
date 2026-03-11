using CkCommons;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

/// <summary>
///     The primary manager for all IPC calls.
/// </summary>
public sealed partial class IpcManager : DisposableMediatorSubscriberBase
{
    public IpcCallerSundouleia Sundouleia { get; }
    public IpcCallerCustomize CustomizePlus { get; }
    public IpcCallerGlamourer Glamourer { get; }
    public IpcCallerIntiface Intiface { get; }
    public IpcCallerLifestream Lifestream { get; }
    public IpcCallerLoci Loci { get; }
    public IpcCallerPenumbra Penumbra { get; }

    public IpcManager(ILogger<IpcManager> logger, GagspeakMediator mediator,
        IpcCallerSundouleia sundouleia,
        IpcCallerCustomize customizePlus,
        IpcCallerGlamourer glamourer,
        IpcCallerIntiface intiface,
        IpcCallerLifestream lifestream,
        IpcCallerLoci loci,
        IpcCallerPenumbra penumbra
        ) : base(logger, mediator)
    {
        Sundouleia = sundouleia;
        CustomizePlus = customizePlus;
        Glamourer = glamourer;
        Intiface = intiface;
        Lifestream = lifestream;
        Loci = loci;
        Penumbra = penumbra;

        // subscribe to the delayed framework update message, which will call upon the periodic API state check.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => PeriodicApiStateCheck());

        Generic.Safe(PeriodicApiStateCheck);
    }

    private void PeriodicApiStateCheck()
    {
        Sundouleia.CheckAPI();
        CustomizePlus.CheckAPI();
        Glamourer.CheckAPI();
        Intiface.CheckAPI();
        Lifestream.CheckAPI();
        Loci.CheckAPI();
        Penumbra.CheckAPI();
        Penumbra.CheckModDirectory();
    }
}
