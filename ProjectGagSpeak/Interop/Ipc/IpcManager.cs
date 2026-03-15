using CkCommons;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

/// <summary>
///     The primary manager for all IPC calls.
/// </summary>
public sealed partial class IpcManager : DisposableMediatorSubscriberBase
{
    public IpcCallerCustomize   CPlus       { get; }
    public IpcCallerGlamourer   Glamourer   { get; }
    public IpcCallerIntiface    Intiface    { get; }
    public IpcCallerLifestream  Lifestream  { get; }
    public IpcCallerLoci        Loci        { get; }
    public IpcCallerMoodles     Moodles     { get; }
    public IpcCallerPenumbra    Penumbra    { get; }
    public IpcCallerSundouleia  Sundouleia  { get; }

    public IpcManager(ILogger<IpcManager> logger, GagspeakMediator mediator,
        IpcCallerCustomize customizePlus,
        IpcCallerGlamourer glamourer,
        IpcCallerIntiface intiface,
        IpcCallerLifestream lifestream,
        IpcCallerLoci loci,
        IpcCallerMoodles moodles,
        IpcCallerPenumbra penumbra,
        IpcCallerSundouleia sundouleia
        ) : base(logger, mediator)
    {
        CPlus = customizePlus;
        Glamourer = glamourer;
        Intiface = intiface;
        Lifestream = lifestream;
        Loci = loci;
        Moodles = moodles;
        Penumbra = penumbra;
        Sundouleia = sundouleia;

        // subscribe to the delayed framework update message, which will call upon the periodic API state check.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => PeriodicApiStateCheck());

        Generic.Safe(PeriodicApiStateCheck);
    }

    private void PeriodicApiStateCheck()
    {
        Sundouleia.CheckAPI();
        CPlus.CheckAPI();
        Glamourer.CheckAPI();
        Intiface.CheckAPI();
        Lifestream.CheckAPI();
        Loci.CheckAPI();
        Moodles.CheckAPI();
        Penumbra.CheckAPI();
        Penumbra.CheckModDirectory();
    }
}
