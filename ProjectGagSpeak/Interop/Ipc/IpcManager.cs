using CkCommons;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

/// <summary>
/// The primary manager for all IPC calls.
/// </summary>
public sealed partial class IpcManager : DisposableMediatorSubscriberBase
{
    public IpcCallerCustomize   CustomizePlus { get; }
    public IpcCallerGlamourer   Glamourer { get; }
    public IpcCallerHeels       Heels { get; }
    public IpcCallerHonorific   Honorific { get; }
    public IpcCallerIntiface    Intiface { get; }
    public IpcCallerLifestream  Lifestream { get; }
    public IpcCallerMoodles     Moodles { get; }
    public IpcCallerPenumbra    Penumbra { get; }
    public IpcCallerPetNames    PetNames { get; }

    public IpcManager(ILogger<IpcManager> logger, GagspeakMediator mediator,
        IpcCallerCustomize customizePlus,
        IpcCallerGlamourer glamourer,
        IpcCallerHeels heels,
        IpcCallerHonorific honorific,
        IpcCallerIntiface intiface,
        IpcCallerLifestream lifestream,
        IpcCallerMoodles moodles,
        IpcCallerPenumbra penumbra,
        IpcCallerPetNames petNames
        ) : base(logger, mediator)
    {
        CustomizePlus = customizePlus;
        Glamourer = glamourer;
        Heels = heels;
        Honorific = honorific;
        Intiface = intiface;
        Lifestream = lifestream;
        Moodles = moodles;
        Penumbra = penumbra;
        PetNames = petNames;

        // subscribe to the delayed framework update message, which will call upon the periodic API state check.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => PeriodicApiStateCheck());

        Generic.Safe(PeriodicApiStateCheck);
    }

    private void PeriodicApiStateCheck()
    {
        CustomizePlus.CheckAPI();
        Glamourer.CheckAPI();
        Heels.CheckAPI();
        Honorific.CheckAPI();
        Intiface.CheckAPI();
        Lifestream.CheckAPI();
        Moodles.CheckAPI();
        Penumbra.CheckAPI();
        PetNames.CheckAPI();

        Penumbra.CheckModDirectory();
    }
}
